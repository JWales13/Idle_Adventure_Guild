using UnityEngine;

namespace IdleGuild.World
{
    /// <summary>
    /// An empty floor with a grid on it, which is all step 1 of section 9 of
    /// Docs/World_View_Design.md asks the world to draw.
    ///
    /// The grid is not decoration. On a floor with nothing on it there is no way to tell
    /// a working drag-to-pan from a broken one -- a solid colour looks identical whether
    /// the camera moved or not, which is this project's own recurring failure shape: a
    /// wrong result whose only symptom is the absence of a change. The grid makes the
    /// camera's movement the visible thing, before there is a single room to judge it by.
    ///
    /// It rebuilds from scratch on every bounds change rather than resizing in place,
    /// because the hall grows a handful of times in a whole run and correctness is worth
    /// more here than the allocations.
    /// </summary>
    internal sealed class GreyBoxFloor
    {
        /// <summary>
        /// Grid line width in world units. Thin enough to read as a line at the zoom the
        /// hall is viewed at, thick enough not to shimmer while panning.
        /// </summary>
        private const float LineThickness = 0.04f;

        /// <summary>
        /// A guard rather than a design figure. Nothing should ever ask for this many
        /// lines; if something does, the bounds are wrong or the spacing is, and a
        /// silently enormous scene is a worse way to find that out than a warning.
        /// </summary>
        private const int MaxLinesPerAxis = 256;

        private readonly Transform _parent;

        internal GreyBoxFloor(Transform parent)
        {
            _parent = parent;
        }

        internal void Rebuild(Rect bounds, float gridSpacing)
        {
            Clear();

            if (bounds.width <= 0f || bounds.height <= 0f)
            {
                Debug.LogError(
                    $"[World] Floor bounds are empty ({bounds}), so there is nothing to " +
                    "draw and nothing to pan across. Set Floor Bounds on WorldView.",
                    _parent);
                return;
            }

            WorldShapes.AddRect(
                _parent, "Floor", bounds, GreyBoxPalette.Floor, WorldSorting.Floor);

            if (gridSpacing <= 0f)
            {
                return;
            }

            BuildGridLines(bounds, gridSpacing);
        }

        internal void Clear()
        {
            for (int index = _parent.childCount - 1; index >= 0; index--)
            {
                GameObject child = _parent.GetChild(index).gameObject;

                if (Application.isPlaying)
                {
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }
            }
        }

        private void BuildGridLines(Rect bounds, float spacing)
        {
            // Lines are placed at multiples of the spacing measured from world origin,
            // not from the bounds' corner, so that growing the hall slides new floor under
            // an unmoved grid instead of shifting every line the player was looking at.
            BuildAxis(bounds, spacing, vertical: true);
            BuildAxis(bounds, spacing, vertical: false);
        }

        private void BuildAxis(Rect bounds, float spacing, bool vertical)
        {
            float min = vertical ? bounds.xMin : bounds.yMin;
            float max = vertical ? bounds.xMax : bounds.yMax;

            int first = Mathf.CeilToInt(min / spacing);
            int last = Mathf.FloorToInt(max / spacing);
            int count = last - first + 1;

            if (count > MaxLinesPerAxis)
            {
                Debug.LogWarning(
                    $"[World] {count} grid lines requested on the " +
                    $"{(vertical ? "x" : "y")} axis at {spacing} spacing across {bounds}. " +
                    $"Drawing the first {MaxLinesPerAxis}; check the bounds or the spacing.",
                    _parent);
                last = first + MaxLinesPerAxis - 1;
            }

            for (int step = first; step <= last; step++)
            {
                float at = step * spacing;
                bool isOrigin = step == 0;

                Rect line = vertical
                    ? new Rect(at - (LineThickness * 0.5f), bounds.yMin, LineThickness, bounds.height)
                    : new Rect(bounds.xMin, at - (LineThickness * 0.5f), bounds.width, LineThickness);

                WorldShapes.AddRect(
                    _parent,
                    $"Grid{(vertical ? "X" : "Y")}{step}",
                    line,
                    isOrigin ? GreyBoxPalette.FloorOrigin : GreyBoxPalette.FloorGrid,
                    WorldSorting.FloorGrid);
            }
        }
    }
}
