using UnityEngine;

namespace IdleGuild.Guild
{
    /// <summary>
    /// One rung of the Village to Capital arc, as data.
    ///
    /// Advancement is gated on minimum levels across *several* buildings, not on total
    /// gold spent. That is deliberate: it stops a player tunnelling into one building
    /// and skipping the others, which is what keeps the three MVP buildings all worth
    /// upgrading instead of one dominating.
    /// </summary>
    [CreateAssetMenu(menuName = "Idle Guild/Guild Tier Definition", fileName = "Tier_", order = 1)]
    public sealed class GuildTierDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable key written into save files. Never change this once a build has shipped.")]
        private string _id;

        [SerializeField] private string _displayName;

        [SerializeField]
        [Tooltip("Position in the arc, starting at 0 for Village. Content declares the tier order it unlocks at.")]
        [Min(0)]
        private int _order;

        [Header("Quest availability at this tier")]
        [SerializeField]
        [Tooltip("Simultaneous quest slots. Static per tier until Quest Board ships, which then adds to this.")]
        [Min(1)]
        private int _questSlots = 1;

        [SerializeField]
        [Tooltip("Hardest quest tier offered. Static per tier until Quest Board ships, which then adds to this.")]
        [Min(1)]
        private int _maxQuestTier = 1;

        [Header("Advancement to the next tier")]
        [SerializeField]
        [Tooltip("Building levels required to advance. The design rule is that this spans multiple buildings.")]
        private BuildingLevelRequirement[] _requirementsToAdvance;

        // Deliberately not [Min]: Unity's Min drawer edits through a float field and
        // would truncate this double on every Inspector draw. Clamped in OnValidate.
        [SerializeField] private double _reputationToAdvance;

        public string Id => _id;
        public string DisplayName => _displayName;
        public int Order => _order;
        public int QuestSlots => _questSlots;
        public int MaxQuestTier => _maxQuestTier;
        public double ReputationToAdvance => _reputationToAdvance;

        /// <summary>Never null, so callers can iterate without a guard.</summary>
        public BuildingLevelRequirement[] RequirementsToAdvance =>
            _requirementsToAdvance ?? System.Array.Empty<BuildingLevelRequirement>();

        /// <summary>True when this is the final tier, which has nothing to advance to.</summary>
        public bool IsFinalTier => RequirementsToAdvance.Length == 0 && _reputationToAdvance <= 0d;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning($"{name}: Id is empty. Saves reference tiers by Id, so this asset cannot persist yet.", this);
            }

            if (_reputationToAdvance < 0d)
            {
                _reputationToAdvance = 0d;
            }

            if (_requirementsToAdvance is { Length: 1 })
            {
                Debug.LogWarning(
                    $"{name}: only one building gates advancement. The tier-gate rule exists so players cannot " +
                    "tunnel into a single building — require at least two, or leave the list empty for a final tier.",
                    this);
            }
        }
    }
}
