using UnityEngine;

namespace IdleGuild.World
{
    /// <summary>
    /// Keeps the camera looking at the hall.
    ///
    /// This is the only real logic in step 1, so it is plain C# with no MonoBehaviour and
    /// no scene around it, and it is tested. Same arrangement as the rest of the project:
    /// <c>GameBootstrap</c> and <c>GuildScreenController</c> are seams onto Unity's
    /// lifecycle and everything below them is ordinary code a test can drive.
    ///
    /// Two things it gets right that the obvious version does not.
    ///
    /// It clamps the **visible rectangle**, not the camera's position. Clamping the centre
    /// to the floor lets half a screen of nothing show at every edge, which on a phone is
    /// most of the screen.
    ///
    /// And it handles the floor being **smaller than the view**, which is not an edge case
    /// here but the opening of the game: section 5 of Docs/World_View_Design.md has the
    /// hall physically growing as wings unlock, so a Village hall on a tall phone is
    /// narrower than the screen from the first frame. On that axis the min bound is
    /// greater than the max, and <c>Mathf.Clamp</c> given a reversed range returns the
    /// max -- silently pinning the hall to one edge and letting the player drag it there
    /// permanently. Centring instead is the only answer that reads as deliberate.
    ///
    /// Public rather than internal for the same reason <c>Format</c> and <c>Outcomes</c>
    /// are: this project carries no <c>InternalsVisibleTo</c>, so the test assembly can
    /// only see what is public, and a rule with an edge case in it is worth more tested
    /// than hidden.
    /// </summary>
    public static class WorldCameraBounds
    {
        /// <summary>
        /// The nearest position to <paramref name="desiredCentre"/> that keeps a view of
        /// <paramref name="viewSize"/> world units inside <paramref name="bounds"/>,
        /// centring on any axis where the bounds are too small to contain the view.
        /// </summary>
        public static Vector2 Clamp(Vector2 desiredCentre, Vector2 viewSize, Rect bounds)
        {
            return new Vector2(
                ClampAxis(desiredCentre.x, viewSize.x, bounds.xMin, bounds.xMax),
                ClampAxis(desiredCentre.y, viewSize.y, bounds.yMin, bounds.yMax));
        }

        private static float ClampAxis(float centre, float viewExtent, float min, float max)
        {
            float half = viewExtent * 0.5f;
            float lowest = min + half;
            float highest = max - half;

            // The view is wider than the floor on this axis: there is no position that
            // fills the screen with floor, so sit in the middle of what there is.
            if (lowest > highest)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(centre, lowest, highest);
        }
    }
}
