namespace IdleGuild.UI
{
    /// <summary>
    /// The three top-level destinations behind the tab bar.
    ///
    /// The upgrade panel is deliberately absent: it is an overlay raised from a building
    /// on the Guild Hall, not a fourth place to be. Keeping it that way is what holds the
    /// core spend — gold into building levels — one tap from the treasury that funds it.
    /// </summary>
    public enum GuildScreen
    {
        Hall = 0,
        Quests = 1,
        Roster = 2
    }
}
