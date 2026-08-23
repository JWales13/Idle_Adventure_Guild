namespace IdleGuild.Core.Services
{
    /// <summary>
    /// Persistence boundary. The game hands this a string and a key and expects to get
    /// the same string back on the next launch; it never learns where the bytes went.
    ///
    /// The same shape as <see cref="IAdService"/> and <see cref="IPurchaseService"/>,
    /// and for the same reason: a cloud save, an iCloud key-value store or an in-memory
    /// double for tests all satisfy this contract, so choosing one later is an adapter
    /// rather than a change to anything that saves.
    ///
    /// Two promises an implementation must keep, because the save code relies on them
    /// instead of re-implementing them itself:
    ///
    /// <list type="bullet">
    /// <item><description><see cref="Write"/> is all-or-nothing. A process killed
    /// mid-write must leave either the previous payload or the new one intact, never a
    /// half-written one. Mobile makes this a routine event, not an edge case.</description></item>
    /// <item><description><see cref="Read"/> returns the best payload still available.
    /// A store that keeps an earlier copy should fall back to it when the current one
    /// has gone missing, which is what makes <see cref="Discard"/> a recovery step
    /// rather than a data loss.</description></item>
    /// </list>
    /// </summary>
    public interface ISaveStore
    {
        /// <summary>True when <see cref="Read"/> would return something for this key.</summary>
        bool Exists(string key);

        /// <summary>
        /// The stored payload, or null when there is nothing readable under this key.
        /// Never throws: an unreadable store is a "start a new game" decision for the
        /// caller, not an exception to unwind through the bootstrap.
        /// </summary>
        string Read(string key);

        /// <summary>
        /// Store <paramref name="contents"/> under <paramref name="key"/>, atomically.
        /// Returns false rather than throwing when the write could not be completed —
        /// a full disk should cost the player their last thirty seconds, not the session.
        /// </summary>
        bool Write(string key, string contents);

        /// <summary>
        /// Drop whichever payload <see cref="Read"/> would currently return, leaving any
        /// older copy behind it available to the next read. This is how a corrupt save is
        /// stepped over — and calling it repeatedly walks back through whatever copies the
        /// store keeps until there are none, rather than getting stuck on the same bad one.
        /// Returns true when something was dropped.
        /// </summary>
        bool Discard(string key);

        /// <summary>
        /// Remove the payload and every recoverable copy of it. Nothing survives, so this
        /// is the player deliberately wiping their progress — not error recovery.
        /// </summary>
        bool Delete(string key);

        /// <summary>
        /// Where this key lives, in whatever terms the store understands. For the debug
        /// console and for bug reports; never parsed.
        /// </summary>
        string DescribeLocation(string key);
    }
}
