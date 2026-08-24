using IdleGuild.Core;
using UnityEngine;

namespace IdleGuild.Quests
{
    /// <summary>
    /// The quest maths, as pure functions of a definition, the party's power, and the
    /// guild's stats.
    ///
    /// Kept static and side-effect free on purpose. Balancing is the part of this
    /// project most likely to be revisited at two in the morning in Week 2, and every
    /// number a quest produces being derivable from three inputs — with no hidden
    /// state and no order of operations to remember — is what makes that survivable.
    ///
    /// Party power is the *sum* of the members' Power, not the average, so quests
    /// should be tuned with their required party size in mind. Sum rewards sending a
    /// bigger party, which is the behaviour the Inn is meant to pay off.
    /// </summary>
    public static class QuestResolution
    {
        /// <summary>An underpowered party never takes more than twice the base duration.</summary>
        public const float MinimumSpeedMultiplier = 0.5f;

        /// <summary>An overpowered party never finishes faster than half the base duration.</summary>
        public const float MaximumSpeedMultiplier = 2f;

        /// <summary>Even a hopeless party keeps a sliver of a chance, and a perfect one keeps a sliver of risk.</summary>
        public const float MaximumFailureChance = 0.9f;

        /// <summary>
        /// Party power over what the quest was balanced for. 1 means exactly matched.
        /// Guards a zero recommendation so a half-filled asset reads as "trivially
        /// overpowered" rather than dividing by zero.
        /// </summary>
        public static float PowerRatio(QuestDefinition quest, float partyPower)
        {
            if (quest == null)
            {
                return 1f;
            }

            float recommended = Mathf.Max(0.0001f, quest.RecommendedPower);
            return Mathf.Max(0f, partyPower) / recommended;
        }

        /// <summary>
        /// How long this run takes. Speed scales with the square root of the power ratio
        /// — quadrupling power halves the timer rather than quartering it — so the
        /// Training Room stays worth investing in without collapsing quest durations to
        /// nothing by the Capital tier.
        /// </summary>
        public static double DurationSeconds(QuestDefinition quest, float partyPower)
        {
            if (quest == null)
            {
                return 0d;
            }

            float speed = Mathf.Clamp(
                Mathf.Sqrt(PowerRatio(quest, partyPower)),
                MinimumSpeedMultiplier,
                MaximumSpeedMultiplier);

            return quest.BaseDurationSeconds / speed;
        }

        /// <summary>
        /// Chance this run fails. The base rate applies at exactly the recommended power,
        /// rises linearly as the party falls short of it — 1.5x the base at half power,
        /// 2x at no power at all — and reaches zero at twice it. The
        /// Armory's Failure Rate Reduction is subtracted afterwards; it reads zero until
        /// that building ships, which is what leaves a flat base rate for the MVP.
        /// </summary>
        public static float FailureChance(QuestDefinition quest, float partyPower, IGuildStats guildStats)
        {
            if (quest == null)
            {
                return 0f;
            }

            float ratio = PowerRatio(quest, partyPower);
            float difficultyScale = Mathf.Clamp(2f - ratio, 0f, 2f);
            float mitigation = guildStats?.Get(GuildStat.FailureRateReduction) ?? 0f;

            return Mathf.Clamp((quest.BaseFailureChance * difficultyScale) - mitigation, 0f, MaximumFailureChance);
        }

        /// <summary>Gold paid on success, scaled by the Tavern's Reward Yield.</summary>
        public static double GoldReward(QuestDefinition quest, IGuildStats guildStats)
        {
            return quest == null ? 0d : quest.GoldReward * RewardYield(guildStats);
        }

        /// <summary>Reputation paid on success, scaled by the Tavern's Reward Yield.</summary>
        public static double ReputationReward(QuestDefinition quest, IGuildStats guildStats)
        {
            return quest == null ? 0d : quest.ReputationReward * RewardYield(guildStats);
        }

        /// <summary>
        /// True when this quest is offered right now: the guild has reached the tier it
        /// appears at, and its difficulty band is within the guild's maximum. That maximum
        /// comes from the tier today and gains the Quest Board's contribution post-launch
        /// without this method changing.
        /// </summary>
        public static bool IsAvailable(QuestDefinition quest, int guildTierOrder, IGuildStats guildStats)
        {
            if (quest == null)
            {
                return false;
            }

            if (quest.MinimumTierOrder > guildTierOrder)
            {
                return false;
            }

            int maxQuestTier = Mathf.FloorToInt(guildStats?.Get(GuildStat.MaxQuestTier) ?? 0f);
            return quest.QuestTier <= maxQuestTier;
        }

        private static float RewardYield(IGuildStats guildStats)
        {
            float yield = guildStats?.Get(GuildStat.RewardYield) ?? 1f;
            return Mathf.Max(0f, yield);
        }
    }
}
