using System;
using IdleGuild.Adventurers;
using IdleGuild.Core;
using IdleGuild.Core.Events;
using UnityEngine;

namespace IdleGuild.App
{
    /// <summary>Why a recruitment did or did not happen.</summary>
    public enum RecruitOutcome
    {
        Recruited,

        /// <summary>No such adventurer in the catalogue.</summary>
        UnknownAdventurer,

        /// <summary>The guild has not reached the tier this archetype appears at.</summary>
        TierLocked,

        /// <summary>The Tavern is not yet good enough to attract this rarity.</summary>
        RarityLocked,

        /// <summary>The Inn has no free bed.</summary>
        HousingFull,

        /// <summary>Not enough gold.</summary>
        Unaffordable
    }

    /// <summary>
    /// Hiring, and the three separate gates that stand in front of it.
    ///
    /// Tier decides whether an archetype exists yet, the Tavern decides how good a
    /// recruit the guild can attract, and the Inn decides whether there is anywhere to
    /// put them. Keeping the three distinct is what stops one building becoming the
    /// only one worth levelling.
    /// </summary>
    public sealed class RecruitmentService
    {
        private readonly GameWorld _world;

        public RecruitmentService(GameWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        /// <summary>Highest rarity the Tavern currently attracts.</summary>
        public Rarity MaximumRecruitableRarity()
        {
            int raw = Mathf.FloorToInt(_world.Stats.Get(GuildStat.RecruitableRarity));
            int highest = (int)Rarity.Legendary;
            return (Rarity)Mathf.Clamp(raw, (int)Rarity.Common, highest);
        }

        /// <summary>Beds occupied out of beds available.</summary>
        public int UsedHousing => _world.Roster.Count;

        /// <summary>Beds available, from the Inn.</summary>
        public int TotalHousing => _world.Roster.CapacityWith(_world.Stats);

        /// <summary>What <see cref="TryRecruit"/> would return, without changing anything.</summary>
        public RecruitOutcome Preview(AdventurerDefinition definition)
        {
            if (definition == null || _world.Content.FindAdventurer(definition.Id) == null)
            {
                return RecruitOutcome.UnknownAdventurer;
            }

            if (definition.MinimumTierOrder > _world.GuildState.CurrentTier.Order)
            {
                return RecruitOutcome.TierLocked;
            }

            if (definition.Rarity > MaximumRecruitableRarity())
            {
                return RecruitOutcome.RarityLocked;
            }

            if (!_world.Roster.HasRoomWith(_world.Stats))
            {
                return RecruitOutcome.HousingFull;
            }

            if (!_world.Economy.CanAfford(CurrencyType.Gold, definition.RecruitCostGold))
            {
                return RecruitOutcome.Unaffordable;
            }

            return RecruitOutcome.Recruited;
        }

        /// <summary>
        /// Hire one adventurer of this archetype. The new roster member gets a fresh
        /// instance id, because the archetype is shared and the individual is not — two
        /// Rangers are two different people with their own levels and rest timers.
        /// </summary>
        public RecruitOutcome TryRecruit(AdventurerDefinition definition, out Adventurer recruited)
        {
            recruited = null;

            RecruitOutcome preview = Preview(definition);
            if (preview != RecruitOutcome.Recruited)
            {
                return preview;
            }

            if (!_world.Economy.TrySpend(CurrencyType.Gold, definition.RecruitCostGold))
            {
                return RecruitOutcome.Unaffordable;
            }

            string instanceId = Guid.NewGuid().ToString("N");
            Adventurer adventurer = new Adventurer(instanceId, definition);
            _world.Roster.Add(adventurer);
            recruited = adventurer;

            EventBus.Publish(new AdventurerRecruited(definition.Id, instanceId));
            return RecruitOutcome.Recruited;
        }
    }
}
