using UnityEngine;
using UnityEngine.UIElements;

namespace IdleGuild.UI
{
    /// <summary>
    /// Keeping the interface out from under the notch, the home indicator and the
    /// rounded corners.
    ///
    /// Worth doing on the day the first screen is built rather than on the day the first
    /// device build is made. The Editor's Game view reports a safe area equal to the
    /// whole screen, so this is invisible until Day 22 — at which point a tab bar sitting
    /// under the home indicator is a layout problem discovered during the bug bash, with
    /// three weeks of screens already built on top of it.
    /// </summary>
    public static class SafeArea
    {
        /// <summary>
        /// Pad <paramref name="root"/> so its contents clear the device's unsafe edges.
        ///
        /// The conversion matters: <see cref="Screen.safeArea"/> is in real pixels while
        /// the panel works in reference-resolution units, and the ratio between them is
        /// whatever Scale With Screen Size settled on. Rather than reproducing that
        /// calculation, the scale is recovered from the root's own resolved height
        /// against the screen's — which is why this is called from a geometry callback
        /// and not once at startup.
        ///
        /// Unity reports the safe area with its origin at the bottom left, so the top
        /// inset is what is left above the rectangle rather than its y.
        /// </summary>
        public static void Apply(VisualElement root)
        {
            if (root == null || Screen.height <= 0 || Screen.width <= 0)
            {
                return;
            }

            float panelHeight = root.resolvedStyle.height;
            if (float.IsNaN(panelHeight) || panelHeight <= 0f)
            {
                return;
            }

            float scale = panelHeight / Screen.height;
            Rect safe = Screen.safeArea;

            root.style.paddingTop = Mathf.Max(0f, Screen.height - safe.yMax) * scale;
            root.style.paddingBottom = Mathf.Max(0f, safe.yMin) * scale;
            root.style.paddingLeft = Mathf.Max(0f, safe.xMin) * scale;
            root.style.paddingRight = Mathf.Max(0f, Screen.width - safe.xMax) * scale;
        }
    }
}
