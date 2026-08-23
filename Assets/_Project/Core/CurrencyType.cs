namespace IdleGuild.Core
{
    /// <summary>
    /// Values are explicit and must never be renumbered — they are persisted in saves.
    /// </summary>
    public enum CurrencyType
    {
        /// <summary>Primary soft currency. Earned from quests, spent on upgrades.</summary>
        Gold = 0,

        /// <summary>Earned alongside gold; gates guild tier advancement.</summary>
        Reputation = 1,

        /// <summary>Premium currency, sold via IAP and granted by rewarded ads.</summary>
        Gems = 2,

        /// <summary>
        /// Prestige currency for the post-launch Branch Expansion loop. Stubbed for v1:
        /// the field persists and reads zero, and nothing grants or spends it yet.
        /// </summary>
        Renown = 3
    }
}
