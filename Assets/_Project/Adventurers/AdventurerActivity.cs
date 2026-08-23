namespace IdleGuild.Adventurers
{
    /// <summary>
    /// What a roster member is doing right now, and therefore whether they can be
    /// dispatched.
    ///
    /// Values are explicit and must never be renumbered — they are persisted in saves.
    /// </summary>
    public enum AdventurerActivity
    {
        /// <summary>Available for dispatch.</summary>
        Idle = 0,

        /// <summary>Out on a quest. Comes back to <see cref="Resting"/> when it resolves.</summary>
        OnQuest = 1,

        /// <summary>Recovering at the Inn. Becomes <see cref="Idle"/> when the timer runs out.</summary>
        Resting = 2
    }
}
