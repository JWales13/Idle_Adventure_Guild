namespace IdleGuild.Core.Events
{
    /// <summary>
    /// Things one presentation layer needs to tell another, and nothing the simulation
    /// knows or cares about.
    ///
    /// **Separate from <c>GameEvents</c> deliberately.** That file is the vocabulary the
    /// simulation speaks -- gold changed, a quest finished, a tier advanced -- and every
    /// struct in it describes something that happened to the guild. Nothing here does. A
    /// room being tapped is not a fact about the world; it is one view asking another to
    /// open a panel, and mixing the two would make <c>GameEvents</c> a place where you
    /// can no longer tell which entries a save, a test or an offline catch-up should care
    /// about.
    ///
    /// **Why these live in Core at all**, given that section 06 of the Ledger says the bar
    /// for adding to Core should stay high. <c>IdleGuild.World</c> and <c>IdleGuild.UI</c>
    /// are siblings above App: the hall draws the rooms, the interface owns the overlays,
    /// and neither may reference the other -- so a tap in one has to reach the other
    /// through something they both already depend on. Core and the event bus are that
    /// something, and routing it through events rather than a reference is Principle 01's
    /// own answer ("systems communicate through events/interfaces rather than direct
    /// references"). The alternatives were an eleventh assembly above both, which is a
    /// whole assembly for one wire, and letting UI reference World, which breaks the rule
    /// that nothing references World on the first day that rule exists.
    ///
    /// The bar this file has to keep clearing: an event belongs here only if BOTH sides of
    /// it are presentation. The moment one is a service, it is a <c>GameEvents</c> entry
    /// or a direct call through App instead.
    /// </summary>
    public readonly struct RoomSelected
    {
        /// <summary>
        /// The <c>BuildingDefinition</c> Id of the room the player touched. An Id rather
        /// than the definition itself, because the definition lives in
        /// <c>IdleGuild.Guild</c> and Core may not reference a feature assembly -- and
        /// because the save format's rules already forbid renaming an Id once shipped,
        /// which makes it the one identifier safe to pass between layers.
        /// </summary>
        public readonly string BuildingId;

        public RoomSelected(string buildingId)
        {
            BuildingId = buildingId;
        }
    }
}
