namespace IdleGuild.Core
{
    /// <summary>
    /// Which stats mean something when added up across every room, and which do not.
    ///
    /// Most stats aggregate: beds from two buildings are more beds, and that is the
    /// property the whole Quest Board / Armory bet rests on — a new asset contributes
    /// additively to a stat consumers already read, and no call site changes.
    ///
    /// Three do not. A room's revenue is its own seats multiplied by its own spend
    /// against its own demand, and summing seats across five rooms produces a number
    /// that is arithmetically fine and means nothing at all. That is the dangerous
    /// shape: not an absence, which this project has learned four times it cannot
    /// detect, but something worse — a plausible wrong answer. Sixty-eight seats reads
    /// exactly like a real figure.
    ///
    /// So <c>GuildState.Aggregate</c> refuses to produce them, <c>IGuildStats.Get</c>
    /// hands back their neutral zero, and the only sanctioned way to read one is
    /// against a named building. A room that quietly earns nothing is a bug you notice
    /// in ten seconds; a room that quietly earns five rooms' worth of seats is one you
    /// ship.
    ///
    /// In Core rather than in Guild because both the producer (GuildState) and the
    /// consumers (the trade layer in App) need the same answer, and two copies of this
    /// rule is one copy too many.
    /// </summary>
    public static class GuildStatScope
    {
        /// <summary>
        /// True when this stat belongs to one room and must be read against that room.
        /// Summing it across the guild is meaningless.
        /// </summary>
        public static bool IsPerBuilding(GuildStat stat)
        {
            return stat == GuildStat.ServiceSeats
                   || stat == GuildStat.CustomerSpend
                   || stat == GuildStat.ServiceDemand;
        }

        /// <summary>True when adding this stat up across every building is the right thing to do.</summary>
        public static bool IsGuildWide(GuildStat stat)
        {
            return !IsPerBuilding(stat);
        }
    }
}
