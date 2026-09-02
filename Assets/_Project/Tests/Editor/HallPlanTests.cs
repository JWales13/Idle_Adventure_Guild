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
        public void TheFloorLeavesAStreetAtTheEntranceAndAMarginElsewhere()
        {
            // This replaced two earlier tests -- "every room is inside the floor" and "the
            // plan leaves the street clear" -- which BOTH became unfalsifiable the moment
            // the floor stopped being authored and started being derived from the rooms.
            // A guard that cannot fail is this project's oldest recurring fault wearing
            // test clothes, and it is worth catching in our own suite rather than only in
            // the game's. What is still worth asserting is that the derivation puts the
            // margins where section 5 wants them.
            HallRoom[] plan = HallPlan.Default();
            Rect floor = HallPlan.FloorFor(plan);

            float xMin = float.MaxValue, xMax = float.MinValue;
            float yMin = float.MaxValue, yMax = float.MinValue;

            foreach (HallRoom room in plan)
            {
                xMin = Mathf.Min(xMin, room.Footprint.xMin);
                xMax = Mathf.Max(xMax, room.Footprint.xMax);
                yMin = Mathf.Min(yMin, room.Footprint.yMin);
                yMax = Mathf.Max(yMax, room.Footprint.yMax);
            }

            Assert.That(floor.yMin, Is.EqualTo(yMin - HallPlan.StreetDepth).Within(0.0001f),
                "The entrance needs its street: it is where the queue outside the door stands.");
            Assert.That(floor.xMin, Is.EqualTo(xMin - HallPlan.EdgeMargin).Within(0.0001f));
            Assert.That(floor.xMax, Is.EqualTo(xMax + HallPlan.EdgeMargin).Within(0.0001f));
            Assert.That(floor.yMax, Is.EqualTo(yMax + HallPlan.EdgeMargin).Within(0.0001f));
        }

        [Test]
        public void AFloorTooSmallForItsRoomsIsReported()
        {
            // The derived floor can never be too small, so the checker would go untested
            // along with it. Handed a floor that IS too small, it has to name the rooms
            // hanging off the edge -- otherwise it is a guard nobody has ever seen work,
            // waiting for the day something else sets the bounds.
            Rect tooSmall = new Rect(-2f, -2f, 4f, 4f);

            Assert.That(
                HallPlan.FootprintsOutside(HallPlan.Default(), tooSmall),
                Is.EquivalentTo(new[] { "tavern", "inn", "training_room" }));
        }

        [Test]
        public void ATapInsideARoomFindsThatRoom()
        {
            HallRoom[] plan = HallPlan.Default();

            Assert.That(HallPlan.FindAt(plan, new Vector2(-5f, -8f))?.BuildingId, Is.EqualTo("tavern"));
            Assert.That(HallPlan.FindAt(plan, new Vector2(5f, -10f))?.BuildingId, Is.EqualTo("inn"));
            Assert.That(HallPlan.FindAt(plan, new Vector2(5f, 2f))?.BuildingId, Is.EqualTo("training_room"));
        }

        [Test]
        public void ATapOnTheCorridorFindsNothing()
        {
            // The gap up the middle of the plan is circulation, not a room. It has to
            // answer null rather than the nearest thing, or every miss opens a panel.
            Assert.That(HallPlan.FindAt(HallPlan.Default(), new Vector2(0f, -8f)), Is.Null);
        }

        [Test]
        public void ATapOnTheStreetFindsNothing()
        {
            // And the street stays tappable-but-empty on purpose: step 6 re-homes the tap
            // onto a waiting customer standing exactly here, and it must not be competing
            // with a room panel when it arrives.
            float street = HallPlan.DefaultFloor.yMin + (HallPlan.StreetDepth * 0.5f);

            // Deliberately off the corridor's axis, so this fails for being street rather
            // than for being the gap between the two columns.
            Assert.That(HallPlan.FindAt(HallPlan.Default(), new Vector2(-5f, street)), Is.Null);
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
