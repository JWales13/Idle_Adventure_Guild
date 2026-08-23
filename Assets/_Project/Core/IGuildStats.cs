namespace IdleGuild.Core
{
    /// <summary>
    /// Read-only view of the guild's aggregated stats.
    ///
    /// This is the seam that lets Quests and Adventurers consume building effects
    /// without referencing the Guild assembly. Guild produces the numbers; everyone
    /// else depends on this interface, so no feature ever reaches into another.
    /// </summary>
    public interface IGuildStats
    {
        /// <summary>
        /// Current value of <paramref name="stat"/> with every building effect applied.
        /// Always returns a usable number: stats no building touches yet — the post-MVP
        /// ones especially — return their documented neutral value rather than throwing.
        /// </summary>
        float Get(GuildStat stat);
    }
}
