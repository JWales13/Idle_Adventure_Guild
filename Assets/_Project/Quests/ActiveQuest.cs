using System;
using System.Collections.Generic;
using IdleGuild.Core;

namespace IdleGuild.Quests
{
    /// <summary>
    /// One quest currently being run by a specific party.
    ///
    /// Duration, failure chance and rewards are all snapshotted when the run starts
    /// rather than recomputed on completion. That is a deliberate design choice with
    /// two payoffs: the player sees a timer that does not move under them when they
    /// upgrade a building mid-run, and offline catch-up can resolve hours of runs
    /// without reconstructing what the guild's stats were at each point in the past.
    /// The trade is that an upgrade only benefits the *next* dispatch, which is the
    /// more legible rule anyway.
    ///
    /// Party members are held as instance ids rather than Adventurer references, so
    /// this assembly still depends on Core alone.
    /// </summary>
    public sealed class ActiveQuest
    {
        private readonly string[] _partyInstanceIds;

        public ActiveQuest(
            string instanceId,
            QuestDefinition definition,
            IReadOnlyList<string> partyInstanceIds,
            double durationSeconds,
            float failureChance,
            double goldOnSuccess,
            double reputationOnSuccess)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("Quest runs need a stable instance id.", nameof(instanceId));
            }

            InstanceId = instanceId;
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));

            _partyInstanceIds = new string[partyInstanceIds?.Count ?? 0];
            for (int index = 0; index < _partyInstanceIds.Length; index++)
            {
                _partyInstanceIds[index] = partyInstanceIds[index];
            }

            TotalSeconds = Math.Max(0d, durationSeconds);
            RemainingSeconds = TotalSeconds;
            FailureChance = Math.Clamp(failureChance, 0f, 1f);
            GoldOnSuccess = Math.Max(0d, goldOnSuccess);
            ReputationOnSuccess = Math.Max(0d, reputationOnSuccess);
        }

        /// <summary>Identifies this run. Saves and the adventurers on it reference this.</summary>
        public string InstanceId { get; }

        public QuestDefinition Definition { get; }

        /// <summary>Instance ids of the adventurers out on this run.</summary>
        public IReadOnlyList<string> PartyInstanceIds => _partyInstanceIds;

        public double TotalSeconds { get; }

        public double RemainingSeconds { get; private set; }

        public float FailureChance { get; }

        public double GoldOnSuccess { get; }

        public double ReputationOnSuccess { get; }

        public bool IsComplete => RemainingSeconds <= 0d;

        /// <summary>Progress in [0, 1], for a progress bar. A zero-length run reads as finished.</summary>
        public float Progress01 =>
            TotalSeconds <= 0d ? 1f : (float)Math.Clamp(1d - (RemainingSeconds / TotalSeconds), 0d, 1d);

        /// <summary>Burn down the timer. Never goes below zero, so an overlong step cannot bank negative time.</summary>
        public void Advance(double seconds)
        {
            if (seconds <= 0d || IsComplete)
            {
                return;
            }

            RemainingSeconds = Math.Max(0d, RemainingSeconds - seconds);
        }

        /// <summary>
        /// Roll the outcome for this run. Call once, when the timer has finished — this
        /// method does not check <see cref="IsComplete"/>, because the caller resolving a
        /// batch of finished quests already knows.
        /// </summary>
        public QuestOutcome Resolve(IRandomSource random)
        {
            float roll = random?.NextUnitFloat() ?? 0f;
            bool succeeded = roll >= FailureChance;

            return succeeded
                ? new QuestOutcome(true, GoldOnSuccess, ReputationOnSuccess)
                : new QuestOutcome(false, 0d, 0d);
        }

        /// <summary>
        /// Set the timer directly. For save restoration only, which rebuilds a run that
        /// was already partway through when the app closed.
        /// </summary>
        public void RestoreRemainingSeconds(double remainingSeconds)
        {
            RemainingSeconds = Math.Clamp(remainingSeconds, 0d, TotalSeconds);
        }
    }
}
