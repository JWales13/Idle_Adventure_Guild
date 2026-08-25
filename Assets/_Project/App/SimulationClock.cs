using System;
using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.Core;
using IdleGuild.Core.Events;
using IdleGuild.Quests;
using UnityEngine;

namespace IdleGuild.App
{
    /// <summary>
    /// Time passing: quest timers running down, rewards paying out, parties resting,
    /// and repeating orders starting again.
    ///
    /// The important design decision is that this steps from *event to event* rather
    /// than in fixed slices. Each iteration jumps straight to whichever happens first —
    /// the next quest completing or the next adventurer finishing their rest — applies
    /// it, and continues. One frame of live play and eight hours of offline catch-up
    /// therefore run through exactly the same code, differing only in how many
    /// iterations they take. There is no separate offline formula that can drift away
    /// from what the game actually pays while the player is watching, which is the usual
    /// way idle games end up with two economies that disagree.
    /// </summary>
    public sealed class SimulationClock
    {
        /// <summary>
        /// Escape hatch for a data mistake that would otherwise hang the game: a quest
        /// with a zero duration and a party with no rest completes instantly and forever.
        /// Eight hours of one-minute quests is a few hundred iterations, so this ceiling
        /// is far above any legitimate run.
        /// </summary>
        private const int MaximumStepsPerAdvance = 20000;

        private readonly GameWorld _world;
        private readonly QuestDispatchService _dispatch;
        private readonly List<ActiveQuest> _finishedBuffer = new List<ActiveQuest>();

        public SimulationClock(GameWorld world, QuestDispatchService dispatch)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));

            Trade = new TradeService(_world);
            Takings = new TakingsService(_world, Trade);
            Stipend = new StipendService(_world);
        }

        /// <summary>
        /// What the rooms are earning. Built here rather than passed in, for two
        /// reasons that both matter more than they look.
        ///
        /// The takings queue is <i>state</i> — it fills with time and a save carries it —
        /// so two instances of it would be two different answers to "how many are
        /// waiting at the bar", and whichever the interface happened to hold would
        /// disagree with whichever the clock was filling. One owner, and the owner is
        /// the thing that drives it.
        ///
        /// And room income is time passing, which is what this class is for. The clock
        /// has been the single path for online and offline since Day 4-5 — the decision
        /// that means there is no second offline formula able to drift from what the
        /// game pays while the player watches. Four rooms earning gold per hour is the
        /// fourth time that decision has paid out, and it paid without being asked:
        /// putting the accrual in here makes eight hours away correct for free.
        /// </summary>
        public TradeService Trade { get; }

        /// <summary>The queue of customers the player can serve by hand, and what they have earned by doing it.</summary>
        public TakingsService Takings { get; }

        /// <summary>
        /// The crown's stipend. Owned here for the same two reasons the takings queue is:
        /// it is state that a save carries, so two instances would be two answers to "is
        /// there anything in the mailbox"; and it fills with time, which is this class.
        /// </summary>
        public StipendService Stipend { get; }

        public long QuestsCompleted { get; private set; }

        public long QuestsSucceeded { get; private set; }

        public long QuestsFailed { get; private set; }

        /// <summary>Total simulated seconds, live and offline together. Useful when reading a bug report.</summary>
        public double TotalSecondsSimulated { get; private set; }

        /// <summary>
        /// Lifetime gold the rooms have taken, before wages. Kept because the ratio it
        /// forms with contract commission is the design requirement the whole revision
        /// exists to hit — §6C tunes for rooms carrying about 70% of lifetime income —
        /// and a ratio measured over one session is noise.
        /// </summary>
        public double GrossEarned { get; private set; }

        /// <summary>Lifetime wages paid. Never more than what was earned in the same moment, because of the floor.</summary>
        public double WagesPaid { get; private set; }

        /// <summary>
        /// Lifetime gold collected from the crown. Its own counter, deliberately outside
        /// <see cref="GrossEarned"/>: the stipend is not room trade, and folding it into
        /// the room total would move the 70/30 split the revision is tuned against
        /// without anybody choosing to.
        /// </summary>
        public double StipendEarned => Stipend.LifetimeStipend;

        /// <summary>
        /// Run the guild forward by <paramref name="seconds"/>. Called with a frame's
        /// delta during play and with the whole absence when catching up offline.
        /// </summary>
        public void Advance(double seconds)
        {
            if (seconds <= 0d || double.IsNaN(seconds))
            {
                return;
            }

            double remaining = seconds;
            int steps = 0;

            while (remaining > 0d)
            {
                if (++steps > MaximumStepsPerAdvance)
                {
                    Debug.LogWarning(
                        "SimulationClock hit its step ceiling in a single Advance. The usual cause is a quest " +
                        "asset with a zero or near-zero duration completing instantly on repeat. Remaining " +
                        $"unsimulated time: {remaining:F1}s.");
                    break;
                }

                double step = NextEventSeconds();
                if (double.IsInfinity(step) || step > remaining)
                {
                    step = remaining;
                }

                if (step < 0d)
                {
                    step = 0d;
                }

                _world.QuestLog.Advance(step);
                _world.Roster.AdvanceRest(step);
                AccrueTrade(step);
                remaining -= step;

                ResolveFinishedQuests();
                StartWaitingAssignments();
            }

            TotalSecondsSimulated += seconds;
        }

        /// <summary>
        /// Pay the rooms for <paramref name="seconds"/> of trading.
        ///
        /// Gross and wages are both constant across one step of this loop, because the
        /// clock only ever stops at an event and nothing inside an event changes a room
        /// level, a tier or the payroll — those are all player actions, which happen
        /// between calls to <see cref="Advance"/>. So this is exact rather than an
        /// approximation, and it is exact over eight offline hours for the same reason.
        ///
        /// <b>The net is floored at zero and the gross is not.</b> Wages come out of the
        /// till and never out of the vault: an over-hired guild earns nothing for an
        /// hour, it does not go backwards. Lifetime wages are recorded as what was
        /// actually taken out of the till, which is why they are capped at the gross —
        /// counting the unpayable remainder would report a bill the player never paid.
        ///
        /// Income arrives through <c>PlayerEconomy.Accrue</c> rather than
        /// <c>Grant</c>, so it announces nothing. That is what CurrencyChanged's own
        /// remark asks for: idle income accrues continuously, publishing per frame would
        /// flood the bus, and a ticking display reads the balance directly.
        /// </summary>
        private void AccrueTrade(double seconds)
        {
            if (seconds <= 0d)
            {
                return;
            }

            double hours = seconds / 3600d;
            double gross = Trade.GrossPerHour() * hours;
            double wages = Trade.WagesPerHour() * hours;
            double net = Math.Max(0d, gross - wages);

            if (gross > 0d)
            {
                GrossEarned += gross;
                WagesPaid += Math.Min(wages, gross);
            }

            _world.Economy.Accrue(CurrencyType.Gold, net);

            // The queue fills at whatever custom is going unserved, which is why a
            // well-staffed guild has nothing to tap and a familiar bought late is a
            // familiar wasted.
            Takings.Accrue(seconds);

            // The mailbox fills whatever else is happening, which is the whole point of
            // it: it is the one income in this game that no sequence of player choices
            // can switch off.
            Stipend.Accrue(seconds);
        }

        /// <summary>
        /// Put the lifetime counters back to a saved reading. For save restoration only —
        /// these numbers are a record of what has happened, and nothing in the simulation
        /// may set them except by actually resolving a quest.
        ///
        /// Values are floored at zero rather than validated against each other. A
        /// succeeded-plus-failed total that disagrees with the completed count means a
        /// hand-edited file, and refusing to load over it would cost the player their
        /// guild to protect a statistic.
        /// </summary>
        public void RestoreCounters(long completed, long succeeded, long failed, double totalSecondsSimulated)
        {
            QuestsCompleted = Math.Max(0L, completed);
            QuestsSucceeded = Math.Max(0L, succeeded);
            QuestsFailed = Math.Max(0L, failed);
            TotalSecondsSimulated = double.IsNaN(totalSecondsSimulated) ? 0d : Math.Max(0d, totalSecondsSimulated);
        }

        /// <summary>
        /// Put the lifetime trade totals back to a saved reading. Restoration only, for
        /// the same reason the quest counters are: these are a record of what has
        /// happened, and nothing in the simulation may set them except by actually
        /// trading. A pre-revision save carries neither and correctly restores zero.
        /// </summary>
        public void RestoreTradeTotals(double grossEarned, double wagesPaid)
        {
            GrossEarned = double.IsNaN(grossEarned) ? 0d : Math.Max(0d, grossEarned);
            WagesPaid = double.IsNaN(wagesPaid) ? 0d : Math.Max(0d, wagesPaid);
        }

        /// <summary>
        /// Seconds until the next thing that needs handling, or
        /// <see cref="double.PositiveInfinity"/> when the guild is idle and nothing will
        /// happen without the player.
        /// </summary>
        public double NextEventSeconds()
        {
            double nextQuest = _world.QuestLog.NextCompletionSeconds();
            double nextRest = _world.Roster.NextRestCompletionSeconds();
            return Math.Min(nextQuest, nextRest);
        }

        private void ResolveFinishedQuests()
        {
            _world.QuestLog.CollectCompleted(_finishedBuffer);
            if (_finishedBuffer.Count == 0)
            {
                return;
            }

            foreach (ActiveQuest run in _finishedBuffer)
            {
                QuestOutcome outcome = run.Resolve(_world.Random);

                _world.Economy.Grant(CurrencyType.Gold, outcome.GoldAwarded);
                _world.Economy.Grant(CurrencyType.Reputation, outcome.ReputationAwarded);

                SendPartyToRest(run);
                _world.QuestLog.Remove(run.InstanceId);

                QuestAssignment assignment = _world.FindAssignmentByRun(run.InstanceId);
                if (assignment != null)
                {
                    assignment.MarkFinished();
                    if (!assignment.Repeat)
                    {
                        _world.RemoveAssignment(assignment.Id);
                    }
                }

                QuestsCompleted++;
                if (outcome.Succeeded)
                {
                    QuestsSucceeded++;
                }
                else
                {
                    QuestsFailed++;
                }

                EventBus.Publish(new QuestCompleted(
                    run.Definition.Id,
                    run.InstanceId,
                    outcome.Succeeded,
                    outcome.GoldAwarded,
                    outcome.ReputationAwarded));
            }
        }

        private void SendPartyToRest(ActiveQuest run)
        {
            foreach (string memberId in run.PartyInstanceIds)
            {
                Adventurer member = _world.Roster.Find(memberId);
                member?.BeginRest(member.RecoverySecondsWith(_world.Stats));
            }
        }

        /// <summary>
        /// Restart every standing order whose party is home and rested, as far as quest
        /// slots allow. Orders are tried in the sequence the player created them, so a
        /// slot shortage is resolved by seniority rather than at random.
        /// </summary>
        private void StartWaitingAssignments()
        {
            for (int index = 0; index < _world.Assignments.Count; index++)
            {
                if (!_world.QuestLog.HasFreeSlotWith(_world.Stats))
                {
                    return;
                }

                QuestAssignment assignment = _world.Assignments[index];
                if (assignment.IsRunning)
                {
                    continue;
                }

                _dispatch.TryStartRun(assignment);
            }
        }
    }
}
