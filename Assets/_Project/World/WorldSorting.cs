namespace IdleGuild.World
{
    /// <summary>
    /// Draw order for everything in the hall, in one place.
    ///
    /// The world sorts on two axes and they do different jobs. Sorting *order* separates
    /// the layers that can never interleave — the floor is always under the rooms, which
    /// are always under the people. Within a single order, the camera's custom
    /// transparency sort axis sorts by world Y, so a townsperson standing lower on the
    /// floor is nearer the viewer and draws in front. That is why everything that walks
    /// on the floor shares one order rather than getting an order each: the moment two
    /// agents are given different orders, the one behind can be drawn in front and no
    /// amount of Y-sorting will save it.
    ///
    /// See <see cref="WorldView"/> for where the sort axis is configured, and section 5
    /// of Docs/World_View_Design.md for why the view is Y-sorted at all — the hall is a
    /// high three-quarter floor plan, so depth into the screen *is* Y.
    /// </summary>
    internal static class WorldSorting
    {
        /// <summary>The ground itself. Nothing is ever behind it.</summary>
        internal const int Floor = -200;

        /// <summary>Grey-box grid lines, painted onto the floor. Dies with the grey box.</summary>
        internal const int FloorGrid = -190;

        /// <summary>
        /// Rooms, their seats and their dressing — the fabric of the building. Step 2.
        /// </summary>
        internal const int Rooms = -100;

        /// <summary>
        /// Everything that walks: townsfolk, staff, adventurers. One shared order, sorted
        /// against each other by world Y. Steps 4, 7 and 8.
        /// </summary>
        internal const int Agents = 0;

        /// <summary>
        /// Coin popups, the 90-second dwell rings, and anything else that annotates an
        /// agent and must never be occluded by one. Step 4.
        /// </summary>
        internal const int Annotations = 100;
    }
}
