using IdleGuild.Core;
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

        [Header("The settlement at this tier")]
        // The revision's demand lever, and the reason the guild never relocates: the
        // settlement grows AROUND the hall, so advancing a tier multiplies how many
        // people want in without a single room changing. Everything the player owns
        // becomes insufficient at the moment they are rewarded, which is the rhythm
        // §3.1 of Vision_Revision.md exists to create.
        [SerializeField]
        [Tooltip("Multiplies every room's Service Demand. 1 at Village; the settlement growing around the hall.")]
        [Min(1f)]
        private float _marketSize = 1f;

        [SerializeField]
        [Tooltip("Multiplies what a contract pays. Static rewards become rounding error against geometric room income.")]
        [Min(1f)]
        private float _contractRewardScale = 1f;

        // The cold-start fix, in data rather than in a code branch — the same shape as
        // Day 4-5's opening deadlock, where Housing Capacity's zero base meant a guild
        // with no Inn could recruit nobody and so could never afford one. With service
        // coming from staff alone, an unstaffed room earns nothing, so a room upgrade
        // has no marginal value AND neither does the first staff member: each needs the
        // other to exist first. The model's run that found this hired no staff for a
        // hundred and fifty hours. This is the guildmaster working the bar themselves.
        [SerializeField]
        [Tooltip("Customers per hour the guildmaster serves unaided. Must be above zero, or an unstaffed guild can never start trading.")]
        [Min(0f)]
        private float _baseServicePerHour;

        [SerializeField]
        [Tooltip("Beds the settlement provides before any Barracks is built, so a Village guild can host an adventurer at all.")]
        [Min(0)]
        private int _baseHousingCapacity;

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
        public float MarketSize => _marketSize;
        public float ContractRewardScale => _contractRewardScale;
        public float BaseServicePerHour => _baseServicePerHour;
        public int BaseHousingCapacity => _baseHousingCapacity;

        /// <summary>Never null, so callers can iterate without a guard.</summary>
        public BuildingLevelRequirement[] RequirementsToAdvance =>
            _requirementsToAdvance ?? System.Array.Empty<BuildingLevelRequirement>();

        /// <summary>True when this is the final tier, which has nothing to advance to.</summary>
        public bool IsFinalTier => RequirementsToAdvance.Length == 0 && _reputationToAdvance <= 0d;

        private void OnValidate()
        {
            if (_reputationToAdvance < 0d)
            {
                _reputationToAdvance = 0d;
            }

            if (_marketSize < 1f)
            {
                _marketSize = 1f;
            }

            if (_contractRewardScale < 1f)
            {
                _contractRewardScale = 1f;
            }

            AssetValidation.WhenLoaded(this, WarnOnIncompleteAsset);
        }

        private void WarnOnIncompleteAsset()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning($"{name}: Id is empty. Saves reference tiers by Id, so this asset cannot persist yet.", this);
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
