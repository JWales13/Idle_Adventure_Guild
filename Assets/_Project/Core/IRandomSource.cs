namespace IdleGuild.Core
{
    /// <summary>
    /// Source of randomness for anything the simulation rolls, currently quest
    /// success.
    ///
    /// Injected rather than calling UnityEngine.Random directly for two reasons that
    /// both bite later: offline catch-up replays hours of quests through the same
    /// code path as live play and must be reproducible when debugging a balance
    /// complaint, and a seeded source lets a test assert an exact outcome instead of
    /// running a quest a thousand times and hoping.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Uniform value in [0, 1).</summary>
        float NextUnitFloat();
    }
}
