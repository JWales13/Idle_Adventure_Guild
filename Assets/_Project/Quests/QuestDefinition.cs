using UnityEngine;

namespace IdleGuild.Quests
{
    /// <summary>
    /// A dispatchable job, as data.
    ///
    /// Like adventurers, quests declare their own unlock tier rather than being listed
    /// on a tier asset. <see cref="QuestTier"/> is the difficulty band, checked against
    /// the guild's current maximum — which the Quest Board will raise post-launch
    /// without this asset or its consumers changing.
    /// </summary>
    [CreateAssetMenu(menuName = "Idle Guild/Quest Definition", fileName = "Quest_", order = 3)]
    public sealed class QuestDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable key written into save files. Never change this once a build has shipped.")]
        private string _id;

        [SerializeField] private string _displayName;
        [SerializeField, TextArea(2, 4)] private string _description;

        [Header("Availability")]
        [SerializeField]
        [Tooltip("Difficulty band. Offered only while the guild's max quest tier reaches this.")]
        [Min(1)]
        private int _questTier = 1;

        [SerializeField]
        [Tooltip("Guild tier order at which this quest can appear. 0 = available from the start.")]
        [Min(0)]
        private int _minimumTierOrder;

        [Header("Requirements")]
        [SerializeField, Min(1)] private int _requiredAdventurers = 1;

        [SerializeField]
        [Tooltip("Party power the quest is balanced around. Falling short raises failure chance rather than blocking dispatch.")]
        [Min(0f)]
        private float _recommendedPower = 1f;

        [Header("Resolution")]
        [SerializeField, Min(1f)] private float _baseDurationSeconds = 60f;

        [SerializeField]
        [Tooltip("Failure chance at exactly the recommended power, before Armory mitigation exists. 0.15 = 15%.")]
        [Range(0f, 1f)]
        private float _baseFailureChance;

        [Header("Rewards")]
        [SerializeField, Min(0)] private double _goldReward;
        [SerializeField, Min(0)] private double _reputationReward;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int QuestTier => _questTier;
        public int MinimumTierOrder => _minimumTierOrder;
        public int RequiredAdventurers => _requiredAdventurers;
        public float RecommendedPower => _recommendedPower;
        public float BaseDurationSeconds => _baseDurationSeconds;
        public float BaseFailureChance => _baseFailureChance;
        public double GoldReward => _goldReward;
        public double ReputationReward => _reputationReward;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning($"{name}: Id is empty. Saves reference quests by Id, so this asset cannot persist yet.", this);
            }
        }
    }
}
