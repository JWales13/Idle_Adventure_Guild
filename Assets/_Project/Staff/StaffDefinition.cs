using IdleGuild.Core;
using UnityEngine;

namespace IdleGuild.Staff
{
    /// <summary>
    /// One kind of employee, as data.
    ///
    /// Staff are the guild's throughput — the third of §3.1's three levers, and the
    /// only one that is a person rather than a number on a building. Demand comes from
    /// the tier, capacity from the room's level, and speed from these.
    ///
    /// Availability is declared here rather than listed on a tier asset, exactly as
    /// adventurers and contracts declare theirs, so adding a fifth kind of employee is
    /// one new asset and no code.
    ///
    /// <b>There is deliberately no wage field.</b> §3.1 of Vision_Revision.md describes
    /// the bill as the sum of each employee's wage, and the model that produced every
    /// tuned number in this project does not do that — it prices wages against what a
    /// customer is worth, because a flat wage against geometric room revenue is
    /// decoration, and the model measured it at three hundredths of one percent of
    /// gross by the endgame. Carrying a per-employee wage here would create a second
    /// source of truth for a number the trade layer already derives, and this project
    /// has watched a ratio authored in one place and paid for in another go unchecked
    /// for four days. One home for it, in the trade layer. See §5 of
    /// Docs/Day16_Staff_And_Revenue.md.
    /// </summary>
    [CreateAssetMenu(menuName = "Idle Guild/Staff Definition", fileName = "Staff_", order = 3)]
    public sealed class StaffDefinition : ScriptableObject
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
        [Tooltip("Guild tier order at which this kind of employee can be taken on. 0 = available from the start.")]
        [Min(0)]
        private int _minimumTierOrder;

        // Deliberately not [Min]: Unity's Min drawer edits through a float field and
        // would truncate this double on every Inspector draw. Clamped in OnValidate.
        // The same trap Day 4-5 found on four fields before any asset existed.
        [SerializeField]
        [Tooltip("One-time gold cost to take this employee on. Wages are ongoing and are not set here.")]
        private double _hireCostGold;

        [Header("Work")]
        [SerializeField]
        [Tooltip("Customers per hour this employee gets through. This is the whole of what they do.")]
        [Min(0f)]
        private float _servicePerHour = 1f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int MinimumTierOrder => _minimumTierOrder;
        public double HireCostGold => _hireCostGold;
        public float ServicePerHour => _servicePerHour;

        /// <summary>
        /// Gold per customer-per-hour this employee delivers. Lower is better value.
        ///
        /// Exists so that a test can divide one authored number by the other, which is
        /// the check Day 13 found nobody had ever performed on the rarity ladder: power
        /// lived on one curve, price on another, and in four days of hunting that exact
        /// symptom nothing had divided them. A ladder that gets worse per gold as it
        /// climbs is a ladder nobody climbs.
        /// </summary>
        public double GoldPerServicePoint =>
            _servicePerHour <= 0f ? double.PositiveInfinity : _hireCostGold / _servicePerHour;

        private void OnValidate()
        {
            if (_hireCostGold < 0d)
            {
                _hireCostGold = 0d;
            }

            AssetValidation.WhenLoaded(this, WarnOnIncompleteAsset);
        }

        private void WarnOnIncompleteAsset()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning($"{name}: Id is empty. Saves reference staff by Id, so this asset cannot persist yet.", this);
            }

            if (_servicePerHour <= 0f)
            {
                Debug.LogWarning(
                    $"{name}: serves nobody per hour, so hiring one does nothing at all. " +
                    "Service is the entirety of what an employee contributes.", this);
            }
        }
    }
}
