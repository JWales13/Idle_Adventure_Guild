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

    /// <summary>Why an adventurer did or did not leave the roster.</summary>
    public enum DismissOutcome
    {
        Dismissed,

        /// <summary>Nobody with that instance id is on the roster.</summary>
        UnknownAdventurer,

        /// <summary>They are out in the field. A party is not disbanded mid-dungeon.</summary>
        OnQuest,

        /// <summary>They belong to a standing order, which has to release them first.</summary>
        OnStandingOrder
    }

    /// <summary>
    /// Hiring, the three separate gates that stand in front of it, and — since Day 12 —
    /// the way back out.
    ///
    /// Tier decides whether an archetype exists yet, the Tavern decides how good a
    /// recruit the guild can attract, and the Inn decides whether there is anywhere to
    /// put them. Keeping the three distinct is what stops one building becoming the
    /// only one worth levelling.
    ///
    /// Retiring lives here rather than on the roster because it is the inverse of the
    /// same transaction and answers to the same resource. The Inn tops out at sixteen
    /// beds and a Capital guild fields twelve, so before this existed a bed spent on the
    /// wrong archetype was spent for the rest of the run — a player who filled their
    /// spare beds with Epics during City could never hire the Legendary that Capital
    /// unlocks, no matter how much gold they ended up with. The content is authored so
    /// that both outcomes are playable, but an irreversible decision made on incomplete
    /// information is a trap wearing a decision's clothes.
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

        /// <summary>
        /// What <see cref="TryDismiss"/> would return, without changing anything.
        ///
        /// The two refusals are checked in the order the player can clear them. Being out
        /// on a quest is the nearer obstacle and resolves itself with time; belonging to a
        /// standing order is the one that needs a decision. Somebody who is both is told
        /// about the quest, because until that run lands nothing they do to the order
        /// helps. Pinned by a test, so a later reorder is a failure rather than a subtly
        /// unhelpful sentence.
        /// </summary>
        public DismissOutcome PreviewDismissal(Adventurer member)
        {
            if (member == null || _world.Roster.Find(member.InstanceId) == null)
            {
                return DismissOutcome.UnknownAdventurer;
            }

            if (member.Activity == AdventurerActivity.OnQuest)
            {
                return DismissOutcome.OnQuest;
            }

            if (_world.IsAssigned(member.InstanceId))
            {
                return DismissOutcome.OnStandingOrder;
            }

            return DismissOutcome.Dismissed;
        }

        /// <summary>
        /// Retire one adventurer, freeing their bed.
        ///
        /// Nothing is refunded. A rebate would make hiring and firing a free churn loop,
        /// and what the roster was missing was reversibility rather than a refund — the
        /// player who guessed wrong needs a way back, not a way to guess for nothing.
        ///
        /// Refusing while they are committed is what keeps this from being the destructive
        /// action that undoes itself. Removing a member of a standing order would leave
        /// <c>QuestDispatchService.TryStartRun</c> failing silently for the rest of the
        /// run, with an order on screen that simply never goes out again;
        /// <c>TryReformParty</c> is the way to release them, and the refusal says so.
        /// </summary>
        public DismissOutcome TryDismiss(Adventurer member)
        {
            DismissOutcome preview = PreviewDismissal(member);
            if (preview != DismissOutcome.Dismissed)
            {
                return preview;
            }

            if (!_world.Roster.Remove(member.InstanceId))
            {
                return DismissOutcome.UnknownAdventurer;
            }

            EventBus.Publish(new AdventurerDismissed(member.Definition.Id, member.InstanceId));
            return DismissOutcome.Dismissed;
        }
    }
}
