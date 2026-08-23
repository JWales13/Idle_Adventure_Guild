using System;
using UnityEngine.UIElements;

namespace IdleGuild.UI
{
    /// <summary>
    /// Small constructors for the elements every screen builds.
    ///
    /// The hierarchy is built in C# rather than in UXML, which is a deliberate reading of
    /// the styling principle: the principle is that appearance lives in text-based USS
    /// instead of the Inspector, and it is equally satisfied by code that adds class
    /// names. Building in code also keeps a screen's structure and its behaviour in one
    /// file, so wiring a button cannot drift from the button existing — a UXML name typo
    /// fails at runtime, a C# one fails at compile time.
    ///
    /// These helpers exist purely so that intent survives the boilerplate: three lines to
    /// make a label, set its text and add two classes buries the one line that matters.
    /// </summary>
    internal static class Ui
    {
        internal static VisualElement Box(params string[] classes)
        {
            return Classed(new VisualElement(), classes);
        }

        internal static Label Text(string text, params string[] classes)
        {
            return Classed(new Label(text), classes);
        }

        internal static Button Action(string text, Action onClick, params string[] classes)
        {
            Button button = new Button(onClick) { text = text };
            button.AddToClassList("button");
            return Classed(button, classes);
        }

        internal static ScrollView Scroll(params string[] classes)
        {
            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
            return Classed(scroll, classes);
        }

        /// <summary>A label/value pair on one line, as used by the stat rows.</summary>
        internal static VisualElement Stat(string label, string value)
        {
            return Stat(label, value, out Label _);
        }

        /// <summary>
        /// The same, handing back the value label. Views that colour a value by whether
        /// a requirement is met take this overload rather than querying the tree for it —
        /// a UQuery lookup would silently return null the day a class name changes.
        /// </summary>
        internal static VisualElement Stat(string label, string value, out Label valueLabel)
        {
            VisualElement stat = Box("stat");
            stat.Add(Text(label, "stat__label"));
            valueLabel = Text(value, "stat__value");
            stat.Add(valueLabel);
            return stat;
        }

        /// <summary>
        /// A track and a fill. Built by hand rather than from ProgressBar because that
        /// control carries a title element and a nest of theme styles that would have to
        /// be undone before it matched anything else on screen.
        /// </summary>
        internal static VisualElement Progress(out VisualElement fill)
        {
            VisualElement track = Box("progress");
            fill = Box("progress__fill");
            track.Add(fill);
            return track;
        }

        /// <summary>Set a progress fill from a value in [0, 1].</summary>
        internal static void SetProgress(VisualElement fill, float unitValue)
        {
            if (fill != null)
            {
                fill.style.width = Length.Percent(UnityEngine.Mathf.Clamp01(unitValue) * 100f);
            }
        }

        private static T Classed<T>(T element, string[] classes) where T : VisualElement
        {
            foreach (string className in classes)
            {
                if (!string.IsNullOrEmpty(className))
                {
                    element.AddToClassList(className);
                }
            }

            return element;
        }
    }
}
