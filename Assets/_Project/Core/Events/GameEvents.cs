namespace IdleGuild.Core.Events
{
    /// <summary>Raised whenever a balance changes, not on every idle tick.</summary>
    /// <remarks>
    /// Idle income accrues continuously; publishing per frame would flood the bus for
    /// no benefit. Continuously-ticking displays should read the balance directly and
    /// treat this event as a correction signal.
    /// </remarks>
    public readonly struct CurrencyChanged
    {
        public CurrencyChanged(CurrencyType currency, double newBalance, double delta)
        {
            Currency = currency;
            NewBalance = newBalance;
            Delta = delta;
        }

        public CurrencyType Currency { get; }
        public double NewBalance { get; }

        /// <summary>Signed change. Negative for spends.</summary>
        public double Delta { get; }
    }

    /// <summary>Raised after a building's level increases and stats have been recalculated.</summary>
    public readonly struct BuildingUpgraded
    {
        public BuildingUpgraded(string buildingId, int newLevel)
        {
            BuildingId = buildingId;
            NewLevel = newLevel;
        }

        public string BuildingId { get; }
        public int NewLevel { get; }
    }

    /// <summary>
    /// Raised when aggregated guild stats change for any reason. Consumers should
    /// re-read <see cref="IGuildStats"/> rather than tracking causes themselves.
    /// </summary>
    public readonly struct GuildStatsRecalculated
    {
    }

    /// <summary>Raised when the guild advances a tier, e.g. Village to Town.</summary>
    public readonly struct GuildTierAdvanced
    {
        public GuildTierAdvanced(string tierId, int order)
        {
            TierId = tierId;
            Order = order;
        }

        public string TierId { get; }

        /// <summary>Zero-based position in the tier arc. Content unlocks compare against this.</summary>
        public int Order { get; }
    }

    /// <summary>Raised when a new adventurer joins the roster.</summary>
    public readonly struct AdventurerRecruited
    {
        public AdventurerRecruited(string definitionId, string instanceId)
        {
            DefinitionId = definitionId;
            InstanceId = instanceId;
        }

        /// <summary>Identifies which AdventurerDefinition asset this came from.</summary>
        public string DefinitionId { get; }

        /// <summary>Identifies this specific roster member, which is what saves reference.</summary>
        public string InstanceId { get; }
    }

    /// <summary>Raised when adventurers are dispatched on a quest.</summary>
    public readonly struct QuestStarted
    {
        public QuestStarted(string definitionId, string instanceId)
        {
            DefinitionId = definitionId;
            InstanceId = instanceId;
        }

        public string DefinitionId { get; }
        public string InstanceId { get; }
    }

    /// <summary>Raised when a quest resolves, whether it succeeded or not.</summary>
    public readonly struct QuestCompleted
    {
        public QuestCompleted(
            string definitionId,
            string instanceId,
            bool succeeded,
            double goldAwarded,
            double reputationAwarded)
        {
            DefinitionId = definitionId;
            InstanceId = instanceId;
            Succeeded = succeeded;
            GoldAwarded = goldAwarded;
            ReputationAwarded = reputationAwarded;
        }

        public string DefinitionId { get; }
        public string InstanceId { get; }
        public bool Succeeded { get; }

        /// <summary>Already scaled by Reward Yield. Zero on failure.</summary>
        public double GoldAwarded { get; }

        /// <summary>Already scaled by Reward Yield. Zero on failure.</summary>
        public double ReputationAwarded { get; }
    }
}
