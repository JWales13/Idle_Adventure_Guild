using IdleGuild.Core;
using UnityEngine;

namespace IdleGuild.Adventurers
{
    /// <summary>
    /// A recruitable adventurer archetype, as data.
    ///
    /// Availability is declared here rather than listed on the tier asset, so adding
    /// an adventurer means creating one asset and nothing else. The Tavern gates the
    /// pool by raising the maximum <see cref="Rarity"/> on offer, which is why rarity
    /// is ordered.
    /// </summary>
    [CreateAssetMenu(menuName = "Idle Guild/Adventurer Definition", fileName = "Adventurer_", order = 2)]
    public sealed class AdventurerDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable key written into save files. Never change this once a build has shipped.")]
        private string _id;

        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _portrait;

        [Header("Availability")]
        [SerializeField] private Rarity _rarity = Rarity.Common;

        [SerializeField]
        [Tooltip("Guild tier order at which this adventurer can appear. 0 = available from the start.")]
        [Min(0)]
        private int _minimumTierOrder;

        // Deliberately not [Min]: Unity's Min drawer edits through a float field and
        // would truncate this double on every Inspector draw. Clamped in OnValidate.
        [SerializeField] private double _recruitCostGold;

        [Header("Combat")]
        [SerializeField]
        [Tooltip("Power at level 1, before the Training Room bonus is applied.")]
        private ScalingCurve _powerByLevel;

        [SerializeField, Min(1)] private int _maxLevel = 10;

        [SerializeField]
        [Tooltip("Gold to train up to a given level. Evaluated at the target level, so level 2 is the first training cost.")]
        private ScalingCurve _trainingCostToReachLevel;

        [SerializeField]
        [Tooltip("Seconds of rest after a quest, before the Inn's recovery speed is applied.")]
        [Min(0f)]
        private float _baseRecoverySeconds = 60f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public Sprite Portrait => _portrait;
        public Rarity Rarity => _rarity;
        public int MinimumTierOrder => _minimumTierOrder;
        public double RecruitCostGold => _recruitCostGold;
        public int MaxLevel => _maxLevel;
        public float BaseRecoverySeconds => _baseRecoverySeconds;

        /// <summary>Power from this archetype alone at the given level, excluding guild bonuses.</summary>
        public float BasePowerAt(int level) => _powerByLevel.Evaluate(level);

        /// <summary>True when <paramref name="level"/> is a real level on this archetype.</summary>
        public bool HasLevel(int level) => level >= 1 && level <= _maxLevel;

        /// <summary>
        /// Gold to train from the level below <paramref name="targetLevel"/> up to it.
        /// Returns zero for levels off the end of the track, which callers should read as
        /// "not trainable" rather than "free" — check <see cref="HasLevel"/> first. This
        /// mirrors BuildingDefinition.CostToReach deliberately: two progression tracks
        /// that behave the same way are two fewer rules to remember.
        /// </summary>
        public double TrainingCostToReach(int targetLevel)
        {
            return HasLevel(targetLevel) && targetLevel > 1 ? _trainingCostToReachLevel.Evaluate(targetLevel) : 0d;
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning($"{name}: Id is empty. Saves reference adventurers by Id, so this asset cannot persist yet.", this);
            }

            if (_recruitCostGold < 0d)
            {
                _recruitCostGold = 0d;
            }
        }
    }
}
