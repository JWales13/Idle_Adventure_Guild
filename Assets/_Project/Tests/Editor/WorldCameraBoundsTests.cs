using IdleGuild.World;
using NUnit.Framework;
using UnityEngine;

namespace IdleGuild.Tests
{
    /// <summary>
    /// How far the player can drag the hall, and what happens when the hall is smaller
    /// than the screen.
    ///
    /// Step 1 of section 9 of Docs/World_View_Design.md is mostly scene plumbing — a
    /// camera, an empty floor, a finger — and this is the one part of it with a rule in
    /// it, so this is the part that gets tested. The pan itself needs no test: it pins the
    /// world point under the finger, so it is correct by construction and wrong only if
    /// the camera is not orthographic, which <see cref="WorldView"/> refuses at startup.
    ///
    /// The case worth the file is the hall being narrower than the view. It is not
    /// hypothetical and it is not the endgame: section 5 has the hall physically growing
    /// as wings unlock, so the FIRST thing the player ever sees is a Village hall on a
    /// 1080x1920 phone, and the naive clamp puts its minimum above its maximum.
    /// <c>Mathf.Clamp</c> does not complain about that — it returns the max — so the hall
    /// would sit against one edge of the screen and stay there, and the symptom is a view
    /// that looks slightly badly composed rather than anything that reads as a bug. This
    /// project has met that shape five times now under the name *a failure whose only
    /// symptom is the absence of something*, and this is the cheapest one to have caught
    /// in advance.
    /// </summary>
    public sealed class WorldCameraBoundsTests
    {
        /// <summary>A hall 32 x 24 world units, centred on the origin.</summary>
        private static readonly Rect Hall = new Rect(-16f, -12f, 32f, 24f);

        /// <summary>A portrait view, narrower and taller than most of the hall.</summary>
        private static readonly Vector2 PhoneView = new Vector2(10f, 20f);

        [Test]
        public void ADragWellInsideTheHallGoesExactlyWhereItWasAsked()
        {
            Vector2 asked = new Vector2(3f, 1f);

            Assert.That(WorldCameraBounds.Clamp(asked, PhoneView, Hall), Is.EqualTo(asked),
                "Nothing should be clamped in the middle of the floor.");
        }

        [Test]
        public void TheHallStopsWhenItsEdgeReachesTheEdgeOfTheScreen()
        {
            Vector2 clamped = WorldCameraBounds.Clamp(new Vector2(1000f, 0f), PhoneView, Hall);

            Assert.That(clamped.x, Is.EqualTo(11f).Within(0.0001f));

            // The point of clamping the VIEW rather than the camera: the right edge of
            // what the player can see lands exactly on the right edge of the hall, with no
            // empty space beyond it. Clamping the camera position to the hall would show
            // half a screen of nothing here.
            float visibleRightEdge = clamped.x + (PhoneView.x * 0.5f);
            Assert.That(visibleRightEdge, Is.EqualTo(Hall.xMax).Within(0.0001f),
                "The screen edge should sit on the hall edge, not past it.");
        }

        [Test]
        public void AHallNarrowerThanTheScreenIsCentredRatherThanPinnedToAnEdge()
        {
            Rect village = new Rect(-4f, -12f, 8f, 24f);

            Vector2 clamped = WorldCameraBounds.Clamp(new Vector2(3f, 0f), PhoneView, village);

            Assert.That(clamped.x, Is.EqualTo(village.center.x).Within(0.0001f),
                "A hall too small to fill the screen should sit in the middle of it.");
        }

        [Test]
        public void AHallNarrowerThanTheScreenCannotBeDraggedAtAll()
        {
            Rect village = new Rect(-4f, -12f, 8f, 24f);

            float draggedHardLeft = WorldCameraBounds.Clamp(new Vector2(-500f, 0f), PhoneView, village).x;
            float draggedHardRight = WorldCameraBounds.Clamp(new Vector2(500f, 0f), PhoneView, village).x;

            // The regression this file exists for. With a reversed range Mathf.Clamp
            // returns the max for both of these, so the two agree — and the hall sits off
            // to one side for the whole of the early game while looking merely untidy.
            Assert.That(draggedHardLeft, Is.EqualTo(draggedHardRight).Within(0.0001f),
                "Dragging either way should do nothing when the hall does not fill the screen.");
            Assert.That(draggedHardLeft, Is.EqualTo(village.center.x).Within(0.0001f),
                "And the nothing it does should leave the hall centred.");
        }

        [Test]
        public void TheTwoAxesAreDecidedIndependently()
        {
            // Wide and shallow: the view can pan across it but is taller than it is.
            Rect corridor = new Rect(-16f, -2f, 32f, 4f);

            Vector2 clamped = WorldCameraBounds.Clamp(new Vector2(1000f, 1000f), PhoneView, corridor);

            Assert.That(clamped.x, Is.EqualTo(11f).Within(0.0001f), "x has room and should clamp.");
            Assert.That(clamped.y, Is.EqualTo(corridor.center.y).Within(0.0001f), "y has none and should centre.");
        }

        [Test]
        public void AViewExactlyTheSizeOfTheHallHasOnlyOnePosition()
        {
            Vector2 exact = new Vector2(Hall.width, Hall.height);

            Assert.That(WorldCameraBounds.Clamp(new Vector2(7f, -5f), exact, Hall), Is.EqualTo(Hall.center),
                "A perfect fit is not the too-small case and must not be centred by accident — " +
                "it is the clamp with its two bounds meeting.");
        }

        [Test]
        public void TheHallDoesNotHaveToBeCentredOnTheOrigin()
        {
            // Section 5: wings attach as rooms unlock, so the hall grows off to one side
            // and its centre walks away from the origin. Nothing here may assume otherwise.
            Rect grown = new Rect(0f, 0f, 32f, 24f);

            Vector2 clamped = WorldCameraBounds.Clamp(new Vector2(1000f, 1000f), PhoneView, grown);

            Assert.That(clamped.x, Is.EqualTo(27f).Within(0.0001f));
            Assert.That(clamped.y, Is.EqualTo(14f).Within(0.0001f));
        }
    }
}
