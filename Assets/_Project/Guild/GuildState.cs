using System;
using System.Collections.Generic;
using IdleGuild.Core;
using IdleGuild.Core.Events;

namespace IdleGuild.Guild
{
    /// <summary>
    /// Runtime state of the guild hall: which tier it has reached and what level each
    /// building sits at, plus the aggregated stats those buildings produce.
    ///
    /// Plain C# with no Unity lifecycle and no knowledge of UI. It publishes what
    /// changed and lets interested systems react, which is what allows the UI to be
    /// rebuilt in Week 3 without this class noticing.
    /// </summary>
    public sealed class GuildState : IGuildStats
    {
        private readonly IReadOnlyList<BuildingDefinition> _buildings;
        private readonly Dictionary<string, int> _levelsByBuildingId;
        private readonly Dictionary<GuildStat, float> _statCache = new Dictionary<GuildStat, float>();

        private GuildTierDefinition _currentTier;

        public GuildState(IReadOnlyList<BuildingDefinition> buildings, GuildTierDefinition startingTier)
        {
            _buildings = buildings ?? throw new ArgumentNullException(nameof(buildings));
            _currentTier = startingTier ?? throw new ArgumentNullException(nameof(startingTier));

            _levelsByBuildingId = new Dictionary<string, int>(buildings.Count);
            foreach (BuildingDefinition building in buildings)
            {
                _levelsByBuildingId[building.Id] = 0;
            }

            Recalculate();
        }

        public GuildTierDefinition CurrentTier => _currentTier;

        public IReadOnlyList<BuildingDefinition> Buildings => _buildings;

        /// <summary>Building levels keyed by building Id. Save/load reads this directly.</summary>
        public IReadOnlyDictionary<string, int> BuildingLevels => _levelsByBuildingId;

        /// <summary>Current level, where 0 means not yet built. Unknown ids read as 0.</summary>
        public int GetLevel(string buildingId)
        {
            return _levelsByBuildingId.TryGetValue(buildingId, out int level) ? level : 0;
        }

        /// <summary>True once the guild has reached the tier at which this building appears.</summary>
        public bool IsAvailable(BuildingDefinition building)
        {
            if (building == null)
            {
                return false;
            }

            return building.MinimumTierOrder <= _currentTier.Order;
        }

        /// <summary>
        /// Set a building's level, clamped to its valid range. Recalculates stats and
        /// announces the change. Silently ignores unknown ids and no-op writes, so
        /// save restoration can replay levels without special-casing.
        /// </summary>
        public void SetLevel(string buildingId, int level)
        {
            if (!_levelsByBuildingId.TryGetValue(buildingId, out int currentLevel))
            {
                return;
            }

            BuildingDefinition definition = FindBuilding(buildingId);
            int maxLevel = definition != null ? definition.MaxLevel : 0;
            int clamped = Math.Clamp(level, 0, maxLevel);

            if (clamped == currentLevel)
            {
                return;
            }

            _levelsByBuildingId[buildingId] = clamped;
            Recalculate();
            EventBus.Publish(new BuildingUpgraded(buildingId, clamped));
        }

        /// <summary>
        /// True when every building requirement for leaving the current tier is met and
        /// the player holds enough reputation.
        ///
        /// Reputation arrives as an argument rather than through a reference to the
        /// Economy assembly: the tier gate needs a number, not a dependency.
        /// </summary>
        public bool CanAdvance(double currentReputation)
        {
            if (_currentTier.IsFinalTier)
            {
                return false;
            }

            if (currentReputation < _currentTier.ReputationToAdvance)
            {
                return false;
            }

            foreach (BuildingLevelRequirement requirement in _currentTier.RequirementsToAdvance)
            {
                if (requirement.Building == null)
                {
                    continue;
                }

                if (GetLevel(requirement.Building.Id) < requirement.MinimumLevel)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Move to <paramref name="tier"/>. Callers are expected to have checked
        /// <see cref="CanAdvance"/> first; this method performs the transition itself.
        /// </summary>
        public void AdvanceTo(GuildTierDefinition tier)
        {
            if (tier == null || tier == _currentTier)
            {
                return;
            }

            _currentTier = tier;
            Recalculate();
            EventBus.Publish(new GuildTierAdvanced(tier.Id, tier.Order));
        }

        /// <summary>
        /// Put the guild back into a previously saved shape: a tier and a full set of
        /// building levels, applied together.
        ///
        /// Deliberately quiet. It publishes <see cref="GuildStatsRecalculated"/>, because
        /// the numbers genuinely did change and anything displaying them must re-read, but
        /// it publishes neither <see cref="BuildingUpgraded"/> nor
        /// <see cref="GuildTierAdvanced"/> — loading a save is not the player upgrading
        /// four times and reaching City again, and a UI that celebrates those events
        /// should not be made to celebrate a load.
        ///
        /// Taking the whole picture in one call rather than level by level is also what
        /// keeps the recalculation to one pass, and what stops the stats being briefly
        /// wrong partway through a restore.
        ///
        /// Buildings absent from <paramref name="buildingLevels"/> are reset to level 0
        /// rather than left alone, so restoring onto a session already in progress cannot
        /// leave a building standing that the save never built.
        /// </summary>
        public void RestoreState(GuildTierDefinition tier, IReadOnlyDictionary<string, int> buildingLevels)
        {
            if (tier != null)
            {
                _currentTier = tier;
            }

            foreach (BuildingDefinition building in _buildings)
            {
                if (building == null)
                {
                    continue;
                }

                int level = 0;
                if (buildingLevels != null && buildingLevels.TryGetValue(building.Id, out int savedLevel))
                {
                    level = savedLevel;
                }

                _levelsByBuildingId[building.Id] = Math.Clamp(level, 0, building.MaxLevel);
            }

            Recalculate();
        }

        /// <inheritdoc />
        public float Get(GuildStat stat)
        {
            return _statCache.TryGetValue(stat, out float value) ? value : NeutralBaseFor(stat);
        }

        private BuildingDefinition FindBuilding(string buildingId)
        {
            foreach (BuildingDefinition building in _buildings)
            {
                if (building != null && building.Id == buildingId)
                {
                    return building;
                }
            }

            return null;
        }

        /// <summary>
        /// Recompute every stat from scratch. Cheap enough to do wholesale — there are
        /// a handful of buildings and a handful of stats — and far easier to reason
        /// about than incremental updates, which is what matters during balancing.
        /// </summary>
        private void Recalculate()
        {
            foreach (GuildStat stat in Enum.GetValues(typeof(GuildStat)))
            {
                _statCache[stat] = Aggregate(stat);
            }

            EventBus.Publish(new GuildStatsRecalculated());
        }

        private float Aggregate(GuildStat stat)
        {
            float additiveTotal = NeutralBaseFor(stat);
            float multiplicativeBonus = 0f;

            foreach (BuildingDefinition building in _buildings)
            {
                if (building == null)
                {
                    continue;
                }

                int level = GetLevel(building.Id);
                if (level < 1)
                {
                    continue;
                }

                foreach (BuildingEffect effect in building.Effects)
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
            }

            return additiveTotal * (1f + multiplicativeBonus);
        }

        /// <summary>
        /// Value a stat holds before any building contributes.
        ///
        /// Quest slots and max quest tier seed from the current guild tier, which is
        /// what makes Quest Board a pure data addition later: it contributes additively
        /// to stats consumers already read, so no call site changes when it ships.
        /// </summary>
        private float NeutralBaseFor(GuildStat stat)
        {
            return stat switch
            {
                GuildStat.RewardYield => 1f,
                GuildStat.RecoverySpeed => 1f,
                GuildStat.QuestSlots => _currentTier.QuestSlots,
                GuildStat.MaxQuestTier => _currentTier.MaxQuestTier,
                _ => 0f
            };
        }
    }
}
