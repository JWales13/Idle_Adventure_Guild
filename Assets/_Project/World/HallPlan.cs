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
    /// working answer for the three rooms that exist rather than a settled one. It is a
    /// spine: the street runs along the bottom, an entrance corridor goes up the middle,
    /// and rooms attach either side of it — chosen because section 5 needs new wings to
    /// arrive without the hall ceasing to read as one building, and a corridor with space
    /// left at the top is the cheapest shape that survives being extended.
    /// </summary>
    public static class HallPlan
    {
        /// <summary>
        /// The hall's extent before any wing is added. One source of truth: the default on
        /// <see cref="WorldView"/> reads it and so do the tests, so a plan that outgrows
        /// its floor is a red test rather than a room drawn off the edge of the world.
        /// </summary>
        public static readonly Rect DefaultFloor = new Rect(-16f, -12f, 32f, 24f);

        /// <summary>
        /// How much of the floor's bottom edge is street rather than building. Section 5:
        /// a bit of outside is visible at the entrance, and it is where unserved demand
        /// physically lives — "the most informative square metre on the screen".
        /// </summary>
        public const float StreetDepth = 3f;

        /// <summary>The provisional layout for the three rooms that currently exist.</summary>
        public static HallRoom[] Default()
        {
            return new[]
            {
                // The main room, and the biggest: it is the only building that compounds,
                // so it is the one the player keeps feeding for ninety levels.
                new HallRoom("tavern", new Rect(-14f, -9f, 12f, 10f)),

                new HallRoom("inn", new Rect(2f, -9f, 12f, 8f)),
                new HallRoom("training_room", new Rect(2f, 1f, 12f, 8f)),
            };
        }

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
