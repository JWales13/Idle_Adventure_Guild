using System;
using System.Globalization;
using IdleGuild.Core;

namespace IdleGuild.UI
{
    /// <summary>
    /// Turning simulation numbers into something a player can read at a glance.
    ///
    /// Display only — nothing here influences what the guild does, and the debug
    /// console's blunt "N0" formatting stays valid alongside it. The reason this exists
    /// as its own file rather than as helpers on the views is that an idle game shows
    /// the same handful of quantities on every screen, and two screens disagreeing
    /// about whether 1500 gold reads as "1.5K" or "1,500" is the sort of thing nobody
    /// notices in review and everybody notices in a store screenshot.
    /// </summary>
    public static class Format
    {
        /// <summary>
        /// Suffixes for each power of a thousand. Four guild tiers do not get anywhere
        /// near the end of this list; the entries past billions are there so that a
        /// balancing mistake shows up as an absurd number rather than as an exception.
        /// </summary>
        private static readonly string[] MagnitudeSuffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };

        /// <summary>
        /// A currency amount. Below a thousand it is written out; above it, three
        /// significant figures and a suffix — 1.25K, 34.2K, 902K, 1.10M.
        ///
        /// Three figures rather than two because idle players read these as progress
        /// bars: 12.4K ticking to 12.5K is visible motion, 12K sitting still for a
        /// minute reads as a stalled game.
        /// </summary>
        public static string Amount(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "—";
            }

            bool negative = value < 0d;
            double magnitude = Math.Abs(value);
            string sign = negative ? "-" : string.Empty;

            if (magnitude < 1000d)
            {
                return sign + Math.Floor(magnitude).ToString("0", CultureInfo.InvariantCulture);
            }

            int tier = 0;
            while (magnitude >= 1000d && tier < MagnitudeSuffixes.Length - 1)
            {
                magnitude /= 1000d;
                tier++;
            }

            string format = magnitude < 10d ? "0.00" : magnitude < 100d ? "0.0" : "0";
            return sign + magnitude.ToString(format, CultureInfo.InvariantCulture) + MagnitudeSuffixes[tier];
        }

        /// <summary>
        /// A countdown. Two units at most, largest first, because "1h 04m" tells a player
        /// what they need and "1h 04m 09s" makes them read three numbers to learn the
        /// same thing.
        /// </summary>
        public static string Duration(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0d)
            {
                seconds = 0d;
            }

            if (double.IsInfinity(seconds))
            {
                return "—";
            }

            int total = (int)Math.Ceiling(seconds);
            int hours = total / 3600;
            int minutes = total % 3600 / 60;
            int remainder = total % 60;

            if (hours > 0)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}h {1:00}m", hours, minutes);
            }

            if (minutes > 0)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}m {1:00}s", minutes, remainder);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}s", remainder);
        }

        /// <summary>A multiplier, as the player thinks of it: x1.15.</summary>
        public static string Multiplier(float value)
        {
            return "x" + value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        /// <summary>A bonus that adds rather than scales, sign always shown: +4.5.</summary>
        public static string Bonus(float value)
        {
            return value.ToString("+0.#;-0.#;0", CultureInfo.InvariantCulture);
        }

        /// <summary>A chance in [0, 1] as whole percent.</summary>
        public static string Percent(float unitValue)
        {
            return Math.Round(unitValue * 100f).ToString("0", CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>
        /// The USS modifier class for a rarity, so a colour never has to be chosen in
        /// C#. Falls through to Common for a value outside the enum, which a save from a
        /// newer build could in principle produce.
        /// </summary>
        public static string RarityClass(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Uncommon => "rarity--uncommon",
                Rarity.Rare => "rarity--rare",
                Rarity.Epic => "rarity--epic",
                Rarity.Legendary => "rarity--legendary",
                _ => "rarity--common"
            };
        }

        /// <summary>
        /// One building effect's own contribution, which reads differently from the
        /// aggregated stat it feeds. A multiplicative effect stores a bonus fraction —
        /// 0.15 meaning +15% — so writing it as "0.15" would be accurate and useless.
        /// </summary>
        public static string EffectValue(ModifierKind kind, float value)
        {
            return kind == ModifierKind.Multiplicative
                ? (value >= 0f ? "+" : string.Empty) + Percent(value)
                : Bonus(value);
        }

        /// <summary>Title-cases a stat name for display: AdventurerPower becomes "Adventurer Power".</summary>
        public static string StatName(GuildStat stat)
        {
            return stat switch
            {
                GuildStat.RewardYield => "Reward yield",
                GuildStat.RecruitableRarity => "Recruit quality",
                GuildStat.AdventurerPower => "Adventurer power",
                GuildStat.HousingCapacity => "Beds",
                GuildStat.RecoverySpeed => "Recovery speed",
                GuildStat.QuestSlots => "Quest slots",
                GuildStat.MaxQuestTier => "Max quest tier",
                GuildStat.FailureRateReduction => "Failure reduction",
                _ => stat.ToString()
            };
        }

        /// <summary>
        /// How a stat's raw number should be written. Multiplicative stats read as a
        /// multiplier, counts as whole numbers, everything else as a bonus — which keeps
        /// the decision in one place rather than at every call site that displays a stat.
        /// </summary>
        public static string StatValue(GuildStat stat, float value)
        {
            return stat switch
            {
                GuildStat.RewardYield => Multiplier(value),
                GuildStat.RecoverySpeed => Multiplier(value),
                GuildStat.FailureRateReduction => Percent(value),
                GuildStat.HousingCapacity => Math.Floor(value).ToString("0", CultureInfo.InvariantCulture),
                GuildStat.QuestSlots => Math.Floor(value).ToString("0", CultureInfo.InvariantCulture),
                GuildStat.MaxQuestTier => Math.Floor(value).ToString("0", CultureInfo.InvariantCulture),
                GuildStat.RecruitableRarity => ((Rarity)Math.Clamp((int)value, 0, (int)Rarity.Legendary)).ToString(),
                _ => Bonus(value)
            };
        }
    }
}
