using System.Collections.Generic;
using IdleGuild.Guild;
using IdleGuild.World;
using NUnit.Framework;
using UnityEngine;

namespace IdleGuild.Tests
{
    /// <summary>
    /// The hall's floor plan, checked against the catalogue it has to hold.
    ///
    /// Step 2 of section 9 of Docs/World_View_Design.md draws each room as a rectangle at
    /// a position the World assembly owns, joined to the simulation by building Id. The
    /// join is the fragile part, and it fails silently in the direction this project keeps
    /// getting caught by: **a building with no footprint is simply not drawn.** No error,
    /// no gap, nothing to notice — the hall just quietly has fewer rooms in it than the
    /// guild does. That is the sixth appearance of *a failure whose only symptom is the
    /// absence of something*, and the first test here is the guard against it.
    ///
    /// It matters most on the day it has not happened yet. The revision authors five rooms
    /// where three stand today, and the plan is a separate file that nothing forces anybody
    /// to update — so the moment a Barracks asset lands, this goes red with its name in it
    /// rather than the Barracks going missing from the picture.
    ///
    /// These assert the plan's SHAPE and not its taste. Where the rooms should actually sit
    /// is listed as open in section 11 and the current layout is provisional; moving a room
    /// must not break a test, but overlapping two of them or pushing one off the floor
    /// should.
    /// </summary>
    public sealed class HallPlanTests
    {
        [Test]
        public void EveryShippedBuildingHasSomewhereToStand()
        {
            List<string> missing = HallPlan.BuildingsWithNoFootprint(
                HallPlan.Default(), Shipped.Content.Buildings);

            Assert.That(missing, Is.Empty,
                "These buildings are in the catalogue with no ground on the hall plan, so " +
                "they would not be drawn at all: " + string.Join(", ", missing) +
                ". Add a footprint to HallPlan.Default().");
        }

        [Test]
        public void NoTwoRoomsStandOnTheSameGround()
        {
            List<string> clashes = HallPlan.OverlappingPairs(HallPlan.Default());

            Assert.That(clashes, Is.Empty,
                "Overlapping footprints: " + string.Join(", ", clashes) +
                ". Two rooms sharing ground draw over each other in an order nothing decides.");
        }

        [Test]
        public void EveryRoomIsInsideTheFloor()
        {
            List<string> outside = HallPlan.FootprintsOutside(
                HallPlan.Default(), HallPlan.DefaultFloor);

            Assert.That(outside, Is.Empty,
                "These rooms sit outside the floor the camera is allowed to reach, so they " +
                "cannot be panned to: " + string.Join(", ", outside) +
                ". Either move them or grow the floor.");
        }

        [Test]
        public void ThePlanLeavesTheStreetClear()
        {
            // Section 5: a bit of outside is visible at the entrance, and section 3 calls it
            // "the most informative square metre on the screen" because unserved demand is
            // what physically stands there. A room built over it would take the queue with
            // it — so the street is a property of the plan rather than a drawing decision.
            float streetTop = HallPlan.DefaultFloor.yMin + HallPlan.StreetDepth;

            foreach (HallRoom room in HallPlan.Default())
            {
                Assert.That(room.Footprint.yMin, Is.GreaterThanOrEqualTo(streetTop),
                    $"{room.BuildingId} is built out over the street, which is where the " +
                    "queue outside the door has to stand.");
            }
        }

        [Test]
        public void AMissingFootprintIsReportedRatherThanGuessedAt()
        {
            HallRoom[] plan = HallPlan.Default();

            Assert.That(HallPlan.Find(plan, "barracks"), Is.Null,
                "An unauthored room has no ground, and Find must say so rather than " +
                "handing back somebody else's.");

            var catalogue = new List<BuildingDefinition> { null };
            Assert.That(HallPlan.BuildingsWithNoFootprint(plan, catalogue), Is.Empty,
                "A null entry in the catalogue is not a room with a missing footprint.");
        }
    }
}
