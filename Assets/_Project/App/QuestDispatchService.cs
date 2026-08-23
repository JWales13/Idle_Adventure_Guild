using System;
using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.Core.Events;
using IdleGuild.Quests;

namespace IdleGuild.App
{
    /// <summary>Why a dispatch did or did not happen.</summary>
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
        NoFreeSlot
    }

    /// <summary>
    /// Sending parties out, and starting each subsequent run of a repeating assignment.
    ///
    /// The distinction that matters here: <see cref="TryDispatch"/> is the player's
    /// decision and validates everything, while <see cref="TryStartRun"/> is the
    /// simulation restarting an order the player already approved. Both funnel into the
    /// same run-creation code, so a quest started while the player watches and one
    /// started three hours into an offline stretch are computed identically.
    /// </summary>
    public sealed class QuestDispatchService
    {
        private readonly GameWorld _world;
        private readonly List<Adventurer> _availableBuffer = new List<Adventurer>();
        private readonly List<string> _idBuffer = new List<string>();

        public QuestDispatchService(GameWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
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

            int partySize = memberInstanceIds?.Count ?? 0;
            if (partySize < quest.RequiredAdventurers)
            {
                return DispatchOutcome.PartyTooSmall;
            }

            foreach (string memberId in memberInstanceIds)
            {
                Adventurer member = _world.Roster.Find(memberId);
                if (member == null || !member.IsAvailable || _world.IsAssigned(memberId))
                {
                    return DispatchOutcome.MemberUnavailable;
                }
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
        /// Convenience for the debug console and, later, a one-tap "send whoever is free"
        /// button: builds a party from idle, unassigned members and dispatches it.
        /// </summary>
        public DispatchOutcome TryDispatchAvailableParty(QuestDefinition quest, bool repeat, out QuestAssignment assignment)
        {
            assignment = null;

            if (quest == null)
            {
                return DispatchOutcome.UnknownQuest;
            }

            _world.Roster.CollectAvailable(_availableBuffer, _world.Roster.Count);

            _idBuffer.Clear();
            foreach (Adventurer member in _availableBuffer)
            {
                if (_world.IsAssigned(member.InstanceId))
                {
                    continue;
                }

                _idBuffer.Add(member.InstanceId);
                if (_idBuffer.Count >= quest.RequiredAdventurers)
                {
                    break;
                }
            }

            return TryDispatch(quest, _idBuffer, repeat, out assignment);
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
    }
}
