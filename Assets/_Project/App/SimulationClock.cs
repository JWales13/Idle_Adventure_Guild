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
        }

        public long QuestsCompleted { get; private set; }

        public long QuestsSucceeded { get; private set; }

        public long QuestsFailed { get; private set; }

        /// <summary>Total simulated seconds, live and offline together. Useful when reading a bug report.</summary>
        public double TotalSecondsSimulated { get; private set; }

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
                remaining -= step;

                ResolveFinishedQuests();
                StartWaitingAssignments();
            }

            TotalSecondsSimulated += seconds;
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
