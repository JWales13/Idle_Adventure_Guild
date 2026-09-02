using UnityEngine;
using UnityEngine.UIElements;

namespace IdleGuild.World
{
    /// <summary>
    /// Asks the interface whether a press landed on it.
    ///
    /// The hall reads the pointer device directly and UI Toolkit does not intercept that,
    /// so without this a drag on the treasury bar slides the floor underneath it and a tap
    /// on a button also opens a room.
    ///
    /// It takes a <c>UIDocument</c> and not a type from <c>IdleGuild.UI</c>, which is the
    /// point. <c>UIDocument</c> lives in Unity's UIElements module, so World can ask the
    /// panel what sits under a pixel without either presentation assembly referencing the
    /// other -- they stay siblings above App, exactly as the graph in section 2 of
    /// Docs/World_View_Design.md draws them.
    ///
    /// **This was written two steps ago and removed rather than shipped dark**, because
    /// while every screen was a full-bleed ScrollView the panel honestly reported the
    /// interface under every pixel of the content area, and turning it on would have left
    /// the hall unable to pan at all. What changed is that the interface is now actually
    /// chrome -- a bar at the top, a bar at the bottom, overlays when raised -- so the
    /// honest answer and the useful answer are finally the same one.
    ///
    /// It still depends on the interface marking its full-screen pass-through containers
    /// <c>PickingMode.Ignore</c>. Without that the root picks every pixel and the hall
    /// stops panning, which looks exactly like the pan breaking rather than like a picking
    /// mode. <c>GuildScreenController.BuildShell</c> sets it and says why.
    /// </summary>
    internal sealed class ChromeHitTest
    {
        private readonly UIDocument _document;

        internal ChromeHitTest(UIDocument document)
        {
            _document = document;
        }

        /// <summary>
        /// True when the interface owns this screen position. Screen coordinates are the
        /// Input System's -- origin bottom-left -- which is what ScreenToPanel expects.
        /// </summary>
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
