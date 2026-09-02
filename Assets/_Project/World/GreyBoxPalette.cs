using UnityEngine;

namespace IdleGuild.World
{
    /// <summary>
    /// The grey box's colours, and nothing else's.
    ///
    /// **This file is scheduled for deletion.** Step 9 of section 9 of
    /// Docs/World_View_Design.md replaces every rectangle here with art, and this goes
    /// with them. It is deliberately NOT wired to Tokens.uss: those tokens are the
    /// interface's design system and the hall is not the interface. Borrowing them would
    /// make a stylesheet that ships look like it was load-bearing for a scaffold that
    /// does not.
    ///
    /// Named rather than inlined so that a future reader can tell "the floor is dark
    /// grey" from "somebody typed 0.15 three times", which is Principle 01's no-magic-
    /// numbers rule applied to a throwaway.
    /// </summary>
    internal static class GreyBoxPalette
    {
        /// <summary>The floor of the hall. Dark, so anything drawn on it reads.</summary>
        internal static readonly Color Floor = new Color(0.14f, 0.15f, 0.18f, 1f);

        /// <summary>
        /// Grid lines. Faint on purpose — they exist to make panning legible on an empty
        /// floor, not to be looked at.
        /// </summary>
        internal static readonly Color FloorGrid = new Color(1f, 1f, 1f, 0.06f);

        /// <summary>
        /// The line through world origin, on both axes. Slightly stronger than the rest,
        /// because "where is nothing" is the first question an empty floor raises.
        /// </summary>
        internal static readonly Color FloorOrigin = new Color(1f, 1f, 1f, 0.16f);
    }
}
