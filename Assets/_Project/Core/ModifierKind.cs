namespace IdleGuild.Core
{
    /// <summary>How a building effect combines with others targeting the same stat.</summary>
    public enum ModifierKind
    {
        /// <summary>Summed into the stat's running total. Used for counts and flat bonuses.</summary>
        Additive = 0,

        /// <summary>Multiplied into the stat after all additive terms. Used for yield and speed scaling.</summary>
        Multiplicative = 1
    }
}
