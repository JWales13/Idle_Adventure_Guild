using System;
using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.Core.Events;
using IdleGuild.Quests;

namespace IdleGuild.App
{
    /// <summary>Why a dispatch did or did not happen.</summary>
    /// <remarks>
    /// Appended to on Day 12 rather than reordered. Nothing persists this enum — it is a
    /// return value, never a saved field — but the project's habit of only ever adding to
    /// an enum is worth keeping uniform, since the one place it matters is not obvious
    /// from the declaration.
    /// </remarks>
    public enum DispatchOutcome
    {
        Dispatched,

        /// <summary>No such quest in the catalogue.</summary>
        UnknownQuest,

        /// <summary>Not offered at the guild's current tier, or above its hardest quest tier.</summary>
        QuestLocked,

        /// <summary>Fewer adventurers than the quest requires.</summary>
        PartyTooSmall,

        /// <summary>Someone in the party is missing, out on a quest, resting, or already on another standing order.</summary>
        MemberUnavailable,

        /// <summary>Every quest slot is occupied.</summary>
        NoFreeSlot,

        /// <summary>More adventurers than the quest has room for.</summary>
        PartyTooLarge,

        /// <summary>The same adventurer appears in the party twice.</summary>
        DuplicateMember,

        /// <summary>No such standing order. Re-forming a party that has since finished.</summary>
        UnknownOrder
    }

    /// <summary>
    /// Sending parties out, changing who is in one, and starting each subsequent run of a
    /// repeating assignment.
    ///
    /// The distinction that matters here: <see cref="TryDispatch"/> is the player's
    /// decision and validates everything, while <see cref="TryStartRun"/> is the
    /// simulation restarting an order the player already approved. Both funnel into the
    /// same run-creation code, so a quest started while the player watches and one
    /// started three hours into an offline stretch are computed identically.
    ///
    /// <see cref="TryReformParty"/> is the third case and behaves like neither: it
    /// changes who goes out *next*, leaving the run in flight entirely alone.
    /// </summary>
    public sealed class QuestDispatchService
    {
        private readonly GameWorld _world;
        private readonly List<Adventurer> _candidateBuffer = new List<Adventurer>();
        private readonly List<string> _idBuffer = new List<string>();
        private readonly Comparison<Adventurer> _strongestFirst;

        public QuestDispatchService(GameWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _strongestFirst = CompareStrongestFirst;
        }

        /// <summary>Combined Power of a prospective party. Missing members count as zero.</summary>
        public float PartyPower(IReadOnlyList<string> memberInstanceIds)
        {
            if (memberInstanceIds == null)
            {
                return 0f;
            }

            float total = 0f;
            foreach (string memberId in memberInstanceIds)
            {
                Adventurer member = _world.Roster.Find(memberId);
                if (member != null)
                {
                    total += member.PowerWith(_world.Stats);
                }
            }

            return total;
        }

        /// <summary>How long this party would take on this quest, for showing before dispatch.</summary>
        public double PreviewDurationSeconds(QuestDefinition quest, IReadOnlyList<string> memberInstanceIds)
        {
            return QuestResolution.DurationSeconds(quest, PartyPower(memberInstanceIds));
        }

        /// <summary>True when the guild's tier and quest tier both allow this quest right now.</summary>
        public bool IsAvailable(QuestDefinition quest)
        {
            return QuestResolution.IsAvailable(quest, _world.GuildState.CurrentTier.Order, _world.Stats);
        }

        /// <summary>
        /// Whether this adventurer could be put into a party being assembled right now.
        ///
        /// <paramref name="existingOrder"/> is the order being re-formed, when there is
        /// one. Its own members pass whatever they are doing, because they are already out
        /// on its behalf and a re-form takes effect from the next run rather than this
        /// one — asking a player to recall an order before they may edit it would put the
        /// guild's most useful hire back on the bench for the length of a run.
        /// </summary>
        public bool IsFreeForParty(string memberInstanceId, QuestAssignment existingOrder = null)
        {
            Adventurer member = _world.Roster.Find(memberInstanceId);
            if (member == null)
            {
                return false;
            }

            if (existingOrder != null && Contains(existingOrder.MemberInstanceIds, memberInstanceId))
            {
                return true;
            }

            return member.IsAvailable && !_world.IsAssigned(memberInstanceId);
        }

        /// <summary>
        /// The party this service would choose for a quest: the strongest free
        /// adventurers, up to exactly what the quest asks for.
        ///
        /// Strongest rather than first-found, which is what roster order used to give. A
        /// convenience button that quietly picks worse people than the player would have
        /// is worth less than no button, and it is also the choice `guild_model.py` makes
        /// on the player's behalf — the two disagreeing is how a modelled arc stops
        /// describing the real one.
        /// </summary>
        public void SuggestParty(QuestDefinition quest, List<string> buffer, QuestAssignment existingOrder = null)
        {
            if (buffer == null)
            {
                return;
            }

            buffer.Clear();
            if (quest == null)
            {
                return;
            }

            _candidateBuffer.Clear();
            foreach (Adventurer member in _world.Roster.Members)
            {
                if (IsFreeForParty(member.InstanceId, existingOrder))
                {
                    _candidateBuffer.Add(member);
                }
            }

            _candidateBuffer.Sort(_strongestFirst);

            int wanted = Math.Min(quest.RequiredAdventurers, _candidateBuffer.Count);
            for (int index = 0; index < wanted; index++)
            {
                buffer.Add(_candidateBuffer[index].InstanceId);
            }
        }

        /// <summary>What <see cref="TryDispatch"/> would return, without changing anything.</summary>
        public DispatchOutcome Preview(QuestDefinition quest, IReadOnlyList<string> memberInstanceIds)
        {
            if (quest == null || _world.Content.FindQuest(quest.Id) == null)
            {
                return DispatchOutcome.UnknownQuest;
            }

            if (!IsAvailable(quest))
            {
                return DispatchOutcome.QuestLocked;
            }

            DispatchOutcome party = CheckParty(quest, memberInstanceIds, null);
            if (party != DispatchOutcome.Dispatched)
            {
                return party;
            }

            if (!_world.QuestLog.HasFreeSlotWith(_world.Stats))
            {
                return DispatchOutcome.NoFreeSlot;
            }

            return DispatchOutcome.Dispatched;
        }

        /// <summary>
        /// Create a standing order for this party and start its first run. A repeating
        /// order survives each run and restarts once the party has rested, which is what
        /// keeps the guild earning while the app is closed.
        /// </summary>
        public DispatchOutcome TryDispatch(
            QuestDefinition quest,
            IReadOnlyList<string> memberInstanceIds,
            bool repeat,
            out QuestAssignment assignment)
        {
            assignment = null;

            DispatchOutcome preview = Preview(quest, memberInstanceIds);
            if (preview != DispatchOutcome.Dispatched)
            {
                return preview;
            }

            QuestAssignment created = new QuestAssignment(
                Guid.NewGuid().ToString("N"),
                quest,
                memberInstanceIds,
                repeat);

            _world.AddAssignment(created);

            if (!TryStartRun(created))
            {
                // Preview passed a moment ago, so this should not happen. Roll the order
                // back rather than leaving a standing order that never runs.
                _world.RemoveAssignment(created.Id);
                return DispatchOutcome.MemberUnavailable;
            }

            assignment = created;
            return DispatchOutcome.Dispatched;
        }

        /// <summary>
        /// Convenience for the debug console and for the party picker's auto-fill: builds
        /// a party from the strongest free adventurers and dispatches it.
        /// </summary>
        public DispatchOutcome TryDispatchAvailableParty(QuestDefinition quest, bool repeat, out QuestAssignment assignment)
        {
            assignment = null;

            if (quest == null)
            {
                return DispatchOutcome.UnknownQuest;
            }

            SuggestParty(quest, _idBuffer);
            return TryDispatch(quest, _idBuffer, repeat, out assignment);
        }

        /// <summary>
        /// What <see cref="TryReformParty"/> would return, without changing anything.
        /// </summary>
        public DispatchOutcome PreviewReform(string assignmentId, IReadOnlyList<string> memberInstanceIds)
        {
            QuestAssignment assignment = _world.FindAssignment(assignmentId);
            if (assignment == null)
            {
                return DispatchOutcome.UnknownOrder;
            }

            return CheckParty(assignment.Quest, memberInstanceIds, assignment);
        }

        /// <summary>
        /// Replace a standing order's party, from its next run onwards.
        ///
        /// The run already in flight is left completely alone. <see cref="ActiveQuest"/>
        /// holds its own snapshot of who went out and what they were promised, and the
        /// clock sends *that* snapshot home when the timer lands — so nobody is recalled
        /// mid-dungeon, no reward that was already computed is lost, and the new party
        /// goes out next time. It is the same reasoning that makes a quest's numbers
        /// immune to an upgrade bought halfway through it.
        ///
        /// Deliberately not gated on the order being idle. The window between runs of a
        /// repeating order is a few seconds of rest, and an edit the player can only make
        /// by catching that window is an edit they will never make.
        ///
        /// The quest slot is not re-checked, because the order already holds one.
        /// </summary>
        public DispatchOutcome TryReformParty(string assignmentId, IReadOnlyList<string> memberInstanceIds)
        {
            DispatchOutcome preview = PreviewReform(assignmentId, memberInstanceIds);
            if (preview != DispatchOutcome.Dispatched)
            {
                return preview;
            }

            QuestAssignment assignment = _world.FindAssignment(assignmentId);
            assignment.SetParty(memberInstanceIds);

            EventBus.Publish(new QuestPartyReformed(assignment.Id, assignment.Quest.Id));
            return DispatchOutcome.Dispatched;
        }

        /// <summary>
        /// Start the next run of an existing order. Returns false when the party is not
        /// all home yet or no slot is free, which is not an error — the simulation simply
        /// tries again at the next step.
        /// </summary>
        public bool TryStartRun(QuestAssignment assignment)
        {
            if (assignment == null || assignment.IsRunning)
            {
                return false;
            }

            if (!_world.QuestLog.HasFreeSlotWith(_world.Stats))
            {
                return false;
            }

            foreach (string memberId in assignment.MemberInstanceIds)
            {
                Adventurer member = _world.Roster.Find(memberId);
                if (member == null || !member.IsAvailable)
                {
                    return false;
                }
            }

            float partyPower = PartyPower(assignment.MemberInstanceIds);
            QuestDefinition quest = assignment.Quest;

            ActiveQuest run = new ActiveQuest(
                Guid.NewGuid().ToString("N"),
                quest,
                assignment.MemberInstanceIds,
                QuestResolution.DurationSeconds(quest, partyPower),
                QuestResolution.FailureChance(quest, partyPower, _world.Stats),
                QuestResolution.GoldReward(quest, _world.Stats),
                QuestResolution.ReputationReward(quest, _world.Stats));

            _world.QuestLog.Add(run);
            assignment.MarkStarted(run.InstanceId);

            foreach (string memberId in assignment.MemberInstanceIds)
            {
                _world.Roster.Find(memberId)?.SendOnQuest(run.InstanceId);
            }

            EventBus.Publish(new QuestStarted(quest.Id, run.InstanceId));
            return true;
        }

        /// <summary>Turn repeating on or off for an existing order.</summary>
        public bool SetRepeat(string assignmentId, bool repeat)
        {
            QuestAssignment assignment = _world.FindAssignment(assignmentId);
            if (assignment == null)
            {
                return false;
            }

            assignment.Repeat = repeat;
            return true;
        }

        /// <summary>
        /// Call a party home. A run already in flight is allowed to finish — abandoning
        /// it would either lose the player earned time or hand out an unearned reward,
        /// and neither reads as fair. The order is dropped once that last run resolves.
        /// </summary>
        public bool Cancel(string assignmentId)
        {
            QuestAssignment assignment = _world.FindAssignment(assignmentId);
            if (assignment == null)
            {
                return false;
            }

            assignment.Repeat = false;
            if (!assignment.IsRunning)
            {
                _world.RemoveAssignment(assignment.Id);
            }

            return true;
        }

        /// <summary>
        /// The checks a prospective party has to pass, shared by a first dispatch and a
        /// re-form so the two can never drift into disagreeing about who may go.
        ///
        /// The size is exact rather than a minimum. A quest asks for a specific number of
        /// adventurers and every balance figure in the game was derived against that
        /// number, so letting a player send four on a three-person job would hand them a
        /// speed multiplier nothing has been tuned for. It was unreachable before Day 12
        /// only because no caller could build an over-size party by hand; the picker can,
        /// so the rule is written down here rather than left to the screen.
        /// </summary>
        private DispatchOutcome CheckParty(
            QuestDefinition quest,
            IReadOnlyList<string> memberInstanceIds,
            QuestAssignment existingOrder)
        {
            int partySize = memberInstanceIds?.Count ?? 0;

            if (partySize < quest.RequiredAdventurers)
            {
                return DispatchOutcome.PartyTooSmall;
            }

            if (partySize > quest.RequiredAdventurers)
            {
                return DispatchOutcome.PartyTooLarge;
            }

            if (NamesAnyoneTwice(memberInstanceIds))
            {
                return DispatchOutcome.DuplicateMember;
            }

            for (int index = 0; index < partySize; index++)
            {
                if (!IsFreeForParty(memberInstanceIds[index], existingOrder))
                {
                    return DispatchOutcome.MemberUnavailable;
                }
            }

            return DispatchOutcome.Dispatched;
        }

        /// <summary>
        /// True when a list names the same adventurer twice. Worth checking now that a
        /// party can be assembled by hand: a duplicated id would count one person's power
        /// twice and shorten a quest for a party that does not exist. Quadratic over at
        /// most a handful of ids, which is cheaper than the set it would otherwise
        /// allocate on every preview.
        /// </summary>
        private static bool NamesAnyoneTwice(IReadOnlyList<string> memberInstanceIds)
        {
            if (memberInstanceIds == null)
            {
                return false;
            }

            for (int index = 0; index < memberInstanceIds.Count; index++)
            {
                for (int other = index + 1; other < memberInstanceIds.Count; other++)
                {
                    if (memberInstanceIds[index] == memberInstanceIds[other])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Contains(IReadOnlyList<string> ids, string candidate)
        {
            foreach (string id in ids)
            {
                if (id == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private int CompareStrongestFirst(Adventurer left, Adventurer right)
        {
            return right.PowerWith(_world.Stats).CompareTo(left.PowerWith(_world.Stats));
        }
    }
}
