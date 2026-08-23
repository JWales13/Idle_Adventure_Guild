namespace IdleGuild.Core
{
    /// <summary>
    /// Adventurer quality band. Ordered, because the Tavern gates recruitment by
    /// raising the maximum rarity available rather than by listing specific
    /// adventurers — so a new adventurer asset joins the pool without editing
    /// anything that already ships.
    ///
    /// Values are explicit and must never be renumbered — they are persisted in saves.
    /// </summary>
    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }
}
