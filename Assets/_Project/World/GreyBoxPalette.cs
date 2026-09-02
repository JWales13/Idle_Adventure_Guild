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
        /// <summary>
        /// Beyond the hall's edge. Darker and warmer than the floor, so the boundary
        /// between inside and outside reads without a wall being drawn -- and so that the
        /// early game, when the hall is smaller than the screen, looks like a small guild
        /// standing in a street rather than like a rendering fault.
        /// </summary>
        internal static readonly Color Outside = new Color(0.09f, 0.085f, 0.08f, 1f);

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

        /// <summary>
        /// A room the guild tier has not unlocked yet. Barely there: section 5 wants
        /// unbuilt rooms "dark and shuttered where the wing will be", which is the tier
        /// gate's missing requirements shown diegetically instead of as a list.
        /// </summary>
        internal static readonly Color RoomLocked = new Color(0.20f, 0.20f, 0.24f, 0.35f);

        /// <summary>Unlocked, affordable, not yet built. Visibly a room-shaped absence.</summary>
        internal static readonly Color RoomUnbuilt = new Color(0.24f, 0.23f, 0.30f, 0.85f);

        /// <summary>A room that exists.</summary>
        internal static readonly Color RoomBuilt = new Color(0.42f, 0.38f, 0.50f, 1f);

        /// <summary>
        /// The band drawn around every room's edge. Without it adjacent rooms of the same
        /// state merge into one field of colour and the plan stops reading as rooms at all
        /// -- which is not a cosmetic problem in a grey box whose entire job is to be
        /// judged by eye.
        /// </summary>
        internal static readonly Color RoomWall = new Color(0.10f, 0.09f, 0.13f, 1f);

        /// <summary>
        /// The unfilled part of a room's level bar. Drawn even at level 1, so that a
        /// ninety-level Tavern at level 1 reads as "barely started" rather than as nothing
        /// -- the bar's job is the ratio, and a ratio needs both halves on screen.
        /// </summary>
        internal static readonly Color RoomLevelTrack = new Color(0f, 0f, 0f, 0.45f);

        /// <summary>How far up its own tree a room has been taken.</summary>
        internal static readonly Color RoomLevelFill = new Color(0.85f, 0.72f, 0.42f, 1f);
    }
}
