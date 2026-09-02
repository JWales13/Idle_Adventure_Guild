using System.Collections.Generic;
using IdleGuild.Guild;
using UnityEngine;

namespace IdleGuild.World
{
    /// <summary>
    /// The rooms, as coloured rectangles that know what they are.
    ///
    /// Step 2 of section 9 of Docs/World_View_Design.md, and the first thing in the world
    /// view that reads the simulation. It reads two things and computes neither: the
    /// building's level, and whether the tier has unlocked it. Both come off
    /// <see cref="GuildState"/> already decided.
    ///
    /// That restraint is the depict-not-cause rule (section 4) at its very first
    /// opportunity to be broken, and it is worth naming what breaking it would look like,
    /// because it would not look like cheating. It would look like this class working out
    /// whether a room is affordable so it can tint it, which needs a cost and a balance,
    /// and now the picture owns a rule that <c>BuildingUpgradeService</c> also owns and
    /// the two can disagree. <c>GuildContext</c>'s rule -- *views read state and call
    /// services, they never compute one* -- applies here unchanged, and section 7 says it
    /// matters more here than in the interface.
    ///
    /// Rebuilt whole on every structural change rather than diffed. A guild changes shape
    /// a few dozen times in a run.
    /// </summary>
    internal sealed class RoomRectangles
    {
        /// <summary>
        /// Inset from the room's edge to its level bar, in world units. Keeps the bar from
        /// reading as part of the wall.
        /// </summary>
        private const float BarInset = 0.5f;

        /// <summary>Height of the level bar in world units.</summary>
        private const float BarHeight = 0.5f;

        private readonly Transform _parent;

        internal RoomRectangles(Transform parent)
        {
            _parent = parent;
        }

        internal void Rebuild(IReadOnlyList<HallRoom> plan, GuildState guild)
        {
            Clear();

            if (plan == null || guild == null)
            {
                return;
            }

            foreach (BuildingDefinition building in guild.Buildings)
            {
                if (building == null)
                {
                    continue;
                }

                HallRoom room = HallPlan.Find(plan, building.Id);

                if (room == null)
                {
                    // Loud, because the alternative is a room that is simply not on the
                    // screen and a session spent wondering where the Barracks went.
                    Debug.LogWarning(
                        $"[World] '{building.Id}' has no footprint on the hall plan, so it " +
                        "cannot be drawn. Add one to the Plan on WorldView.",
                        _parent);
                    continue;
                }

                Draw(building, room, guild);
            }
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

        private void Draw(BuildingDefinition building, HallRoom room, GuildState guild)
        {
            int level = guild.GetLevel(building.Id);
            bool built = level > 0;
            bool available = guild.IsAvailable(building);

            Color body = built
                ? GreyBoxPalette.RoomBuilt
                : available
                    ? GreyBoxPalette.RoomUnbuilt
                    : GreyBoxPalette.RoomLocked;

            WorldShapes.AddRect(
                _parent, $"Room_{building.Id}", room.Footprint, body, WorldSorting.Rooms);

            if (!built)
            {
                return;
            }

            DrawLevelBar(building, room, level);
        }

        private void DrawLevelBar(BuildingDefinition building, HallRoom room, int level)
        {
            Rect footprint = room.Footprint;
            float width = footprint.width - (BarInset * 2f);

            if (width <= 0f)
            {
                return;
            }

            var track = new Rect(
                footprint.xMin + BarInset, footprint.yMin + BarInset, width, BarHeight);

            WorldShapes.AddRect(
                _parent,
                $"Room_{building.Id}_LevelTrack",
                track,
                GreyBoxPalette.RoomLevelTrack,
                WorldSorting.Rooms + 1);

            // The tree's own length, not a shared scale. The three trees are deliberately
            // different lengths -- Tavern 90, Training Room 40, Inn 30, because only the
            // Tavern compounds -- so a bar drawn against a common maximum would report the
            // Inn as permanently behind when it is finished. See "Why the building trees
            // are different lengths" in the Ledger.
            float fraction = building.MaxLevel > 0
                ? Mathf.Clamp01((float)level / building.MaxLevel)
                : 0f;

            if (fraction <= 0f)
            {
                return;
            }

            var fill = new Rect(track.xMin, track.yMin, track.width * fraction, BarHeight);

            WorldShapes.AddRect(
                _parent,
                $"Room_{building.Id}_LevelFill",
                fill,
                GreyBoxPalette.RoomLevelFill,
                WorldSorting.Rooms + 2);
        }
    }
}
