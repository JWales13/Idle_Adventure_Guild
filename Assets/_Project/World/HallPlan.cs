using System;
using System.Collections.Generic;
using IdleGuild.Guild;
using UnityEngine;

namespace IdleGuild.World
{
    /// <summary>One room's ground on the hall's floor plan.</summary>
    [Serializable]
    public sealed class HallRoom
    {
        [SerializeField]
        [Tooltip("The BuildingDefinition Id this ground belongs to. Never renamed once shipped.")]
        private string _buildingId;

        [SerializeField]
        [Tooltip("Where the room sits, in world units.")]
        private Rect _footprint;

        public HallRoom(string buildingId, Rect footprint)
        {
            _buildingId = buildingId;
            _footprint = footprint;
        }

        public string BuildingId => _buildingId;

        public Rect Footprint => _footprint;
    }

    /// <summary>
    /// Where the rooms physically are, and the checks that keep the plan honest.
    ///
    /// **This lives in World rather than on <c>BuildingDefinition</c>, deliberately.** A
    /// room's position is presentation: the simulation has never known where anything is
    /// and adding a coordinate to a shipped definition asset would put a fact about the
    /// picture inside the data the economy is authored from. It would also be the first
    /// new field on <c>BuildingDefinition</c> since Day 2, and section 06 of the Ledger
    /// names exactly that as the signal that the Quest Board / Armory bet has been lost —
    /// a signal worth keeping sensitive even when this particular field would be harmless.
    ///
    /// The join is the building **Id**, which the save format's own rules already forbid
    /// renaming once a build has shipped, so it is the one identifier safe to key on.
    ///
    /// **The layout below is provisional.** Section 11 of Docs/World_View_Design.md lists
    /// "where the five rooms physically sit on the plan" as still open, and this is a
    /// working answer rather than a settled one. It is a spine: the street runs along the
    /// bottom, an entrance corridor goes up the middle, and rooms attach either side of it
    /// — chosen because section 5 needs new wings to arrive without the hall ceasing to
    /// read as one building, and a corridor growing upward is the cheapest shape that
    /// survives being extended. Day 18 authored the other two rooms into it.
    /// </summary>
    public static class HallPlan
    {
        /// <summary>
        /// How much of the floor's bottom edge is street rather than building. Section 5:
        /// a bit of outside is visible at the entrance, and it is where unserved demand
        /// physically lives -- "the most informative square metre on the screen".
        /// </summary>
        public const float StreetDepth = 3f;

        /// <summary>Floor left beyond the outermost wall on the other three sides.</summary>
        public const float EdgeMargin = 1f;

        /// <summary>
        /// The layout, drawn for a portrait phone and now holding all five rooms.
        ///
        /// **The first version of this was a landscape shape on a portrait device**, and the
        /// grey box is what caught it: rooms twelve units wide on a plan thirty-two across,
        /// which at any zoom where a room was legible meant one room filled 89% of the
        /// screen width and two could never be seen at once. That is fatal for a view whose
        /// whole job is showing the guild as a place, and no amount of art would have fixed
        /// it. Exactly the sort of thing section 9 says to find out as rectangles.
        ///
        /// So: rooms eight units wide, stacked either side of a two-unit corridor running up
        /// from the entrance, and the hall grows **upward**, which suits both the device and
        /// section 5's expanding hall. Against the zoom policy of *the screen's short edge
        /// shows fourteen world units*, one room is 8/14 — about 57% of a portrait phone's
        /// width, which reads. A facing pair plus the corridor is eighteen and never fits at
        /// once; that is the accepted trade rather than an oversight, because the alternative
        /// is rooms too small to hold a legible seat.
        ///
        /// **The rooms are placed in the order the guild unlocks them**, bottom to top, so
        /// the hall grows away from the street as the settlement grows around it and every
        /// growth step still reads as one building. The two Village rooms face each other
        /// across the entrance — the Tavern where the townsfolk sit and the Front Desk where
        /// the contracts are posted — Town adds the Inn above the Tavern and the Barracks
        /// above the desk, which is the door the adventurers living in it walk out of, and
        /// City adds the Provisioner at the top.
        ///
        /// The Tavern is the tallest at twelve units against nine. It is the only room whose
        /// spend compounds, the one the player feeds for fifty-seven levels, and the one with
        /// sixty seats in it at the end where the Inn has thirty-four — so it needs the most
        /// ground and is the room the camera opens on.
        ///
        /// Still provisional: section 11 lists where the rooms sit as open, and this is a
        /// working answer with a constraint attached rather than a settled one.
        /// </summary>
        public static HallRoom[] Default()
        {
            return new[]
            {
                // Village — either side of the entrance, hard against the street.
                new HallRoom("tavern", new Rect(-9f, -14f, 8f, 12f)),
                new HallRoom("front_desk", new Rect(1f, -14f, 8f, 9f)),

                // Town.
                new HallRoom("inn", new Rect(-9f, 0f, 8f, 9f)),
                new HallRoom("barracks", new Rect(1f, -3f, 8f, 9f)),

                // City.
                new HallRoom("provisioner", new Rect(1f, 8f, 8f, 9f)),
            };
        }

        /// <summary>
        /// The ground the hall stands on: everything the rooms occupy, plus a margin, plus
        /// the street along the entrance.
        ///
        /// **Derived rather than authored, and that is the point.** A hand-set floor is a
        /// number that has to agree with the plan and has nothing keeping it honest -- and
        /// it had already gone stale in the scene, where a serialized rectangle from the
        /// first layout would have survived every edit to this file. Deriving it means
        /// section 5's requirement that "camera bounds grow with the hall" is satisfied by
        /// construction: author a room, and the ground it stands on and the reach of the
        /// camera both follow.
        /// </summary>
        public static Rect FloorFor(IReadOnlyList<HallRoom> plan)
        {
            if (plan == null || plan.Count == 0)
            {
                return Rect.zero;
            }

            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;
            bool any = false;

            foreach (HallRoom room in plan)
            {
                if (room == null)
                {
                    continue;
                }

                Rect f = room.Footprint;
                xMin = Mathf.Min(xMin, f.xMin);
                xMax = Mathf.Max(xMax, f.xMax);
                yMin = Mathf.Min(yMin, f.yMin);
                yMax = Mathf.Max(yMax, f.yMax);
                any = true;
            }

            if (!any)
            {
                return Rect.zero;
            }

            return Rect.MinMaxRect(
                xMin - EdgeMargin,
                yMin - StreetDepth,
                xMax + EdgeMargin,
                yMax + EdgeMargin);
        }

        /// <summary>The ground under the default layout.</summary>
        public static Rect DefaultFloor => FloorFor(Default());

        /// <summary>The ground assigned to a building, or null if it has none.</summary>
        public static HallRoom Find(IReadOnlyList<HallRoom> plan, string buildingId)
        {
            if (plan == null || string.IsNullOrEmpty(buildingId))
            {
                return null;
            }

            foreach (HallRoom room in plan)
            {
                if (room != null && room.BuildingId == buildingId)
                {
                    return room;
                }
            }

            return null;
        }

        /// <summary>
        /// The room standing on a world point, or null for floor, corridor or street.
        ///
        /// Unambiguous only because footprints may not overlap, which
        /// <c>NoTwoRoomsStandOnTheSameGround</c> asserts -- otherwise "which room did I
        /// tap" would be answered by array order, which is not an answer.
        /// </summary>
        public static HallRoom FindAt(IReadOnlyList<HallRoom> plan, Vector2 worldPoint)
        {
            if (plan == null)
            {
                return null;
            }

            foreach (HallRoom room in plan)
            {
                if (room != null && room.Footprint.Contains(worldPoint))
                {
                    return room;
                }
            }

            return null;
        }

        /// <summary>
        /// Buildings in the catalogue with nowhere to stand. A room with no footprint is
        /// simply not drawn, and this project has been caught five times by a failure whose
        /// only symptom is the absence of something — so it is named rather than left to be
        /// noticed.
        /// </summary>
        public static List<string> BuildingsWithNoFootprint(
            IReadOnlyList<HallRoom> plan, IReadOnlyList<BuildingDefinition> buildings)
        {
            var missing = new List<string>();

            if (buildings == null)
            {
                return missing;
            }

            foreach (BuildingDefinition building in buildings)
            {
                if (building != null && Find(plan, building.Id) == null)
                {
                    missing.Add(building.Id);
                }
            }

            return missing;
        }

        /// <summary>Rooms whose ground is not wholly inside the floor.</summary>
        public static List<string> FootprintsOutside(IReadOnlyList<HallRoom> plan, Rect floor)
        {
            var outside = new List<string>();

            if (plan == null)
            {
                return outside;
            }

            foreach (HallRoom room in plan)
            {
                if (room == null)
                {
                    continue;
                }

                Rect f = room.Footprint;
                bool contained =
                    f.xMin >= floor.xMin && f.xMax <= floor.xMax &&
                    f.yMin >= floor.yMin && f.yMax <= floor.yMax;

                if (!contained)
                {
                    outside.Add(room.BuildingId);
                }
            }

            return outside;
        }

        /// <summary>Pairs of rooms standing on the same ground, as "a/b" strings.</summary>
        public static List<string> OverlappingPairs(IReadOnlyList<HallRoom> plan)
        {
            var clashes = new List<string>();

            if (plan == null)
            {
                return clashes;
            }

            for (int a = 0; a < plan.Count; a++)
            {
                for (int b = a + 1; b < plan.Count; b++)
                {
                    if (plan[a] == null || plan[b] == null)
                    {
                        continue;
                    }

                    if (plan[a].Footprint.Overlaps(plan[b].Footprint))
                    {
                        clashes.Add($"{plan[a].BuildingId}/{plan[b].BuildingId}");
                    }
                }
            }

            return clashes;
        }
    }
}
