using IdleGuild.Adventurers;
using IdleGuild.Core;
using IdleGuild.Guild;
using IdleGuild.Quests;
using UnityEngine;

namespace IdleGuild.App
{
    /// <summary>
    /// The catalogue of everything the game is made of, plus the handful of new-game
    /// numbers that belong to no single definition.
    ///
    /// This is the one asset that knows about all four content types at once, which is
    /// why it lives in the App layer rather than in a feature: the features still
    /// depend on Core alone. Adding content means adding an entry here and nothing
    /// else — no code changes, which is the whole point of the arrangement.
    /// </summary>
    [CreateAssetMenu(menuName = "Idle Guild/Game Content", fileName = "GameContent", order = 10)]
    public sealed class GameContent : ScriptableObject
    {
        [Header("Catalogue")]
        [SerializeField] private BuildingDefinition[] _buildings;
        [SerializeField] private GuildTierDefinition[] _tiers;
        [SerializeField] private AdventurerDefinition[] _adventurers;
        [SerializeField] private QuestDefinition[] _quests;

        [Header("New game")]
        [Tooltip("Gold the player starts with. Must cover the first building, since a guild with no Inn has no beds and cannot recruit.")]
        [SerializeField] private double _startingGold = 100d;

        [SerializeField] private double _startingReputation;

        [Header("Offline")]
        [Tooltip("Longest stretch of absence that still pays out. Time beyond this is forfeited.")]
        [SerializeField, Min(60f)] private float _maximumOfflineSeconds = 8f * 3600f;

        /// <summary>Never null, so callers can iterate without a guard.</summary>
        public BuildingDefinition[] Buildings => _buildings ?? System.Array.Empty<BuildingDefinition>();

        /// <summary>Never null. Not assumed to be sorted — order is read from each tier's Order field.</summary>
        public GuildTierDefinition[] Tiers => _tiers ?? System.Array.Empty<GuildTierDefinition>();

        /// <summary>Never null.</summary>
        public AdventurerDefinition[] Adventurers => _adventurers ?? System.Array.Empty<AdventurerDefinition>();

        /// <summary>Never null.</summary>
        public QuestDefinition[] Quests => _quests ?? System.Array.Empty<QuestDefinition>();

        public double StartingGold => _startingGold;
        public double StartingReputation => _startingReputation;
        public float MaximumOfflineSeconds => _maximumOfflineSeconds;

        /// <summary>The lowest-Order tier, which a new guild begins at. Null if no tiers are listed.</summary>
        public GuildTierDefinition StartingTier
        {
            get
            {
                GuildTierDefinition lowest = null;
                foreach (GuildTierDefinition tier in Tiers)
                {
                    if (tier == null)
                    {
                        continue;
                    }

                    if (lowest == null || tier.Order < lowest.Order)
                    {
                        lowest = tier;
                    }
                }

                return lowest;
            }
        }

        /// <summary>
        /// The next tier up from <paramref name="order"/>, or null at the end of the arc.
        /// Found by comparing Order rather than by array position, so reordering the list
        /// in the Inspector cannot silently reshuffle the progression.
        /// </summary>
        public GuildTierDefinition TierAfter(int order)
        {
            GuildTierDefinition next = null;
            foreach (GuildTierDefinition tier in Tiers)
            {
                if (tier == null || tier.Order <= order)
                {
                    continue;
                }

                if (next == null || tier.Order < next.Order)
                {
                    next = tier;
                }
            }

            return next;
        }

        /// <summary>
        /// The tier with this id, or null. Saves store the tier the guild reached by id,
        /// and this is how it is resolved back to the asset on load.
        /// </summary>
        public GuildTierDefinition FindTier(string id)
        {
            foreach (GuildTierDefinition tier in Tiers)
            {
                if (tier != null && tier.Id == id)
                {
                    return tier;
                }
            }

            return null;
        }

        public BuildingDefinition FindBuilding(string id)
        {
            foreach (BuildingDefinition building in Buildings)
            {
                if (building != null && building.Id == id)
                {
                    return building;
                }
            }

            return null;
        }

        public AdventurerDefinition FindAdventurer(string id)
        {
            foreach (AdventurerDefinition adventurer in Adventurers)
            {
                if (adventurer != null && adventurer.Id == id)
                {
                    return adventurer;
                }
            }

            return null;
        }

        public QuestDefinition FindQuest(string id)
        {
            foreach (QuestDefinition quest in Quests)
            {
                if (quest != null && quest.Id == id)
                {
                    return quest;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            if (_startingGold < 0d)
            {
                _startingGold = 0d;
            }

            if (_startingReputation < 0d)
            {
                _startingReputation = 0d;
            }

            AssetValidation.WhenLoaded(this, WarnOnIncompleteAsset);
        }

        // Day 4–5 moved these checks off StartingTier and onto array lengths, because
        // OnValidate fires while Unity has the referenced tier assets reloading and the
        // references read null. That was half the story: the arrays themselves read
        // empty in the same window, so a fully populated catalogue went on warning that
        // it had no tiers. Both checks are correct — they just cannot run until the
        // asset has finished loading, which is what AssetValidation is for. Length is
        // still the right thing to count: a genuinely empty slot is caught at startup,
        // where GameWorld throws with the same message.
        private void WarnOnIncompleteAsset()
        {
            if (Tiers.Length == 0)
            {
                Debug.LogWarning($"{name}: no guild tiers listed, so there is nothing for a new guild to start at.", this);
            }

            if (Buildings.Length == 0)
            {
                Debug.LogWarning($"{name}: no buildings listed. Nothing can be upgraded and no stats will accumulate.", this);
            }
        }
    }
}
