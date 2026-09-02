using UnityEngine;
using UnityEngine.UIElements;

namespace IdleGuild.World
{
    /// <summary>
    /// TEMPORARY — restored only so Unity can compile and then remove this file through the
    /// AssetDatabase. Delete it in the Project window; nothing references it.
    ///
    /// It was written to stop a drag that begins on the interface from also panning the
    /// hall, and it cannot be turned on yet: every screen is a full-bleed ScrollView, so
    /// the panel reports the interface under every pixel of the content area and wiring
    /// this would leave the hall unable to pan at all. The seam it would have filled is
    /// WorldView.IsPointerOverChrome, which carries the reasoning and stays unset until
    /// section 7 of Docs/World_View_Design.md is settled.
    /// </summary>
    internal sealed class ChromeHitTest
    {
        private readonly UIDocument _document;

        internal ChromeHitTest(UIDocument document)
        {
            _document = document;
        }

        internal bool Covers(Vector2 screenPosition)
        {
            if (_document == null)
            {
                return false;
            }

            VisualElement root = _document.rootVisualElement;
            IPanel panel = root?.panel;

            if (panel == null)
            {
                return false;
            }

            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
            return panel.Pick(panelPosition) != null;
        }
    }
}
