using System;
using IdleGuild.Core;
using UnityEngine;

namespace IdleGuild.Guild
{
    /// <summary>
    /// One building's contribution to one stat, scaled by its level.
    ///
    /// Effects are data rather than code, which is what lets Quest Board and Armory
    /// arrive post-launch as new assets: they target <see cref="GuildStat"/> values
    /// that already exist and already aggregate, so nothing that ships today changes.
    /// </summary>
    [Serializable]
    public struct BuildingEffect
    {
        [Tooltip("Which guild stat this contributes to.")]
        public GuildStat Stat;

        [Tooltip("Additive contributions are summed. Multiplicative ones are treated as a bonus fraction: 0.15 means +15%.")]
        public ModifierKind Kind;

        [Tooltip("Contribution by building level.")]
        public ScalingCurve ValuePerLevel;
    }
}
