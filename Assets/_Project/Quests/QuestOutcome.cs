namespace IdleGuild.Quests
{
    /// <summary>What a finished quest run paid out.</summary>
    public readonly struct QuestOutcome
    {
        public QuestOutcome(bool succeeded, double goldAwarded, double reputationAwarded)
        {
            Succeeded = succeeded;
            GoldAwarded = goldAwarded;
            ReputationAwarded = reputationAwarded;
        }

        public bool Succeeded { get; }

        /// <summary>Already scaled by Reward Yield. Zero on failure.</summary>
        public double GoldAwarded { get; }

        /// <summary>Already scaled by Reward Yield. Zero on failure.</summary>
        public double ReputationAwarded { get; }
    }
}
