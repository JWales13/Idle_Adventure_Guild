using IdleGuild.Core;
using UnityEngine;

namespace IdleGuild.Guild
{
    /// <summary>
    /// A guild hall building as pure data.
    ///
    /// Level 0 means not yet built, and a building at level 0 contributes nothing.
    /// Level 1 is the constructed state, so <see cref="CostToReach"/>(1) is the build
    /// cost and every level above that is an upgrade.
    /// </summary>
    [CreateAssetMenu(menuName = "Idle Guild/Building Definition", fileName = "Building_", order = 0)]
    public sealed class BuildingDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable key written into save files. Never change this once a build has shipped.")]
        private string _id;

        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 4)] private string _description;
        [SerializeField] private Sprite _icon;

        [Header("Availability")]
        [SerializeField]
        [Tooltip("Guild tier order at which this building becomes available. 0 = available from the start.")]
        [Min(0)]
        private int _minimumTierOrder;

        [Header("Progression")]
        [SerializeField, Min(1)] private int _maxLevel = 10;

        [SerializeField]
        [Tooltip("Gold cost to reach a given level. Evaluated at the target level, so level 1 is the build cost.")]
        private ScalingCurve _costToReachLevel;

        [SerializeField]
        [Tooltip("What this building does. Each entry targets one stat.")]
        private BuildingEffect[] _effects;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int MinimumTierOrder => _minimumTierOrder;
        public int MaxLevel => _maxLevel;

        /// <summary>Never null, so callers can iterate without a guard.</summary>
        public BuildingEffect[] Effects => _effects ?? System.Array.Empty<BuildingEffect>();

        /// <summary>
        /// Gold required to reach <paramref name="targetLevel"/> from the level below it.
        /// Returns zero for levels beyond <see cref="MaxLevel"/>, which callers should
        /// treat as "not purchasable" rather than "free" — check <see cref="CanReach"/> first.
        /// </summary>
        public double CostToReach(int targetLevel)
        {
            return CanReach(targetLevel) ? _costToReachLevel.Evaluate(targetLevel) : 0d;
        }

        /// <summary>True when <paramref name="targetLevel"/> is a real level on this building.</summary>
        public bool CanReach(int targetLevel)
        {
            return targetLevel >= 1 && targetLevel <= _maxLevel;
        }

        /// <summary>
        /// This building's own contribution to <paramref name="stat"/> at
        /// <paramref name="level"/>, with no other building involved.
        ///
        /// The seam the revenue engine needs and the reason no new field was added to
        /// this asset to get it. A room's takings are its own seats times its own spend
        /// against its own demand, and none of those three survive being summed across
        /// the guild — see <see cref="GuildStatScope"/>. Reading them here, off the
        /// definition that owns them, is what lets Revenue be an effect like every other
        /// effect rather than a special case bolted onto BuildingDefinition. Adding a
        /// sixth department stays one new .asset file and zero code, which is the bet.
        ///
        /// Additive entries are summed and multiplicative ones accumulate as a bonus
        /// fraction onto 1.0, exactly as <c>GuildState.Aggregate</c> does it — the two
        /// must agree, because a stat that means one thing guild-wide and another
        /// per-room is a stat nobody can balance. Returns zero at level 0: an unbuilt
        /// room contributes nothing, here as everywhere.
        /// </summary>
        public float EffectAt(GuildStat stat, int level)
        {
            if (level < 1)
            {
                return 0f;
            }

            float additiveTotal = 0f;
            float multiplicativeBonus = 0f;

            foreach (BuildingEffect effect in Effects)
            {
                if (effect.Stat != stat)
                {
                    continue;
                }

                float value = effect.ValuePerLevel.Evaluate(level);
                if (effect.Kind == ModifierKind.Additive)
                {
                    additiveTotal += value;
                }
                else
                {
                    multiplicativeBonus += value;
                }
            }

            return additiveTotal * (1f + multiplicativeBonus);
        }

        /// <summary>True when this building carries at least one effect targeting <paramref name="stat"/>.</summary>
        public bool Produces(GuildStat stat)
        {
            foreach (BuildingEffect effect in Effects)
            {
                if (effect.Stat == stat)
                {
                    return true;
                }
            }

            return false;
        }

        // Nothing to clamp here, so OnValidate only queues the self-check. See
        // AssetValidation for why it cannot run inline.
        private void OnValidate()
        {
            AssetValidation.WhenLoaded(this, WarnOnIncompleteAsset);
        }

        private void WarnOnIncompleteAsset()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning($"{name}: Id is empty. Saves reference buildings by Id, so this asset cannot persist yet.", this);
            }

            if (_effects == null || _effects.Length == 0)
            {
                Debug.LogWarning($"{name}: no effects defined, so this building does nothing when upgraded.", this);
            }
        }
    }
}
