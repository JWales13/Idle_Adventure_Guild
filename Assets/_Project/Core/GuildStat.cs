namespace IdleGuild.Core
{
    /// <summary>
    /// Every quantity a building can influence. Buildings own non-overlapping stats
    /// by design, so upgrading each one matters instead of a single building
    /// dominating the curve.
    ///
    /// The post-MVP entries are declared now on purpose: Quest Board and Armory ship
    /// later as new BuildingDefinition assets targeting stats that already exist,
    /// which is what keeps that expansion a data change rather than a code change.
    ///
    /// Values are explicit and must never be renumbered — they are persisted in saves.
    /// </summary>
    public enum GuildStat
    {
        /// <summary>Tavern. Morale multiplier on gold and loot paid per completed quest.</summary>
        RewardYield = 0,

        /// <summary>Tavern. Highest <see cref="Rarity"/> offered in the recruitment pool.</summary>
        RecruitableRarity = 1,

        /// <summary>Training Room. Added to every adventurer's Power, which shortens quests and cuts failure chance.</summary>
        AdventurerPower = 2,

        /// <summary>Inn. Hard cap on how many adventurers can be housed.</summary>
        HousingCapacity = 3,

        /// <summary>Inn. Multiplier on rest and recovery time between quests. Higher is faster.</summary>
        RecoverySpeed = 4,

        /// <summary>Quest Board (post-MVP). Simultaneous quest slots. Static per guild tier until it ships.</summary>
        QuestSlots = 5,

        /// <summary>Quest Board (post-MVP). Hardest quest tier offered. Static per guild tier until it ships.</summary>
        MaxQuestTier = 6,

        /// <summary>Armory (post-MVP). Flat reduction to failure chance. Zero until it ships, leaving a flat base rate.</summary>
        FailureRateReduction = 7
    }
}
