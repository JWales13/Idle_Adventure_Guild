using System;
using UnityEngine;

namespace IdleGuild.Core
{
    /// <summary>
    /// Level-indexed growth shared by upgrade costs and building effects.
    ///
    /// Deliberately expressed as growth *percent* per level rather than a raw
    /// multiplier, so that a freshly created asset — every field zero — evaluates to
    /// a flat zero rather than collapsing to zero through a pow(0, n) term. Designer
    /// mistakes should read as "I haven't filled this in", never as silent breakage.
    /// </summary>
    [Serializable]
    public struct ScalingCurve
    {
        [Tooltip("Value at level 1.")]
        public float BaseValue;

        [Tooltip("Added per level beyond the first, before growth is applied.")]
        public float LinearPerLevel;

        [Tooltip("Compounding growth per level, as a fraction. 0 = linear, 0.15 = +15% per level.")]
        public float GrowthPerLevel;

        /// <summary>
        /// Value at <paramref name="level"/>, which is 1-based. Levels below 1 clamp to
        /// level 1 rather than extrapolating backwards into negative territory.
        /// </summary>
        public readonly float Evaluate(int level)
        {
            int stepsAboveFirst = Mathf.Max(0, level - 1);
            float linearPart = BaseValue + (LinearPerLevel * stepsAboveFirst);
            if (Mathf.Approximately(GrowthPerLevel, 0f))
            {
                return linearPart;
            }

            return linearPart * Mathf.Pow(1f + GrowthPerLevel, stepsAboveFirst);
        }
    }
}
