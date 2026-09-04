using System.Collections.Generic;
using IdleGuild.App;
using IdleGuild.Core;
using IdleGuild.Guild;
using IdleGuild.Quests;
using NUnit.Framework;

namespace IdleGuild.Tests
{
    /// <summary>
    /// Step 7 of the pass: content appearing on schedule, and not before.
    ///
    /// This is the architectural bet as an assertion. Neither new quest is listed on a
    /// tier asset — each declares its own <c>MinimumTierOrder</c> and difficulty band,
    /// and <see cref="QuestResolution.IsAvailable"/> reads the guild's ceiling off
    /// <see cref="IGuildStats"/> rather than off the tier. That indirection is the same
    /// path Quest Board will raise post-launch, which is why it is worth pinning down
    /// now rather than after it has two producers.
    /// </summary>
    public sealed class TierUnlockTests
    {
        private static readonly Dictionary<string, string[]> ExpectedAtEachTier = new Dictionary<string, string[]>
        {
            ["village"] = new[] { "rat_infested_cellar", "bandit_patrol" },
            ["town"] = new[] { "rat_infested_cellar", "bandit_patrol", "ruined_watchtower" },
            ["city"] = new[] { "rat_infested_cellar", "bandit_patrol", "ruined_watchtower", "sunken_crypt" },
            ["capital"] = new[] { "rat_infested_cellar", "bandit_patrol", "ruined_watchtower", "sunken_crypt", "dragons_roost" }
        };

        [Test]
        public void EachTierOffersExactlyTheQuestsItShould()
        {
            foreach (KeyValuePair<string, string[]> expected in ExpectedAtEachTier)
            {
                GameWorld world = Shipped.NewGuild();
                Shipped.MoveTo(world, expected.Key);

                HashSet<string> available = new HashSet<string>();
                foreach (QuestDefinition quest in world.Content.Quests)
                {
                    if (QuestResolution.IsAvailable(quest, world.GuildState.CurrentTier.Order, world.Stats))
                    {
                        available.Add(quest.Id);
                    }
                }

                Assert.That(available, Is.EquivalentTo(expected.Value),
                    $"The quests on offer at {expected.Key} are not the ones expected.");
            }
        }

        [Test]
        public void QuestSlotsFollowTheTier()
        {
            AssertSlots("village", 1);
            AssertSlots("town", 2);
            AssertSlots("city", 3);
            AssertSlots("capital", 4);
        }

        /// <summary>
        /// Quest slots and the hardest tier are seeded from the guild tier and read
        /// through the same stat everything else uses, so Quest Board can add to them
        /// later without a single call site changing. The seeding is the part worth a
        /// test: get it wrong and the post-launch building has nothing to add to.
        /// </summary>
        [Test]
        public void SlotsAndDifficultyAreSeededFromTheTierRatherThanHardcoded()
        {
            GameWorld world = Shipped.NewGuild();

            foreach (GuildTierDefinition tier in Shipped.TiersInOrder())
            {
                world.GuildState.AdvanceTo(tier);

                Assert.That(world.Stats.Get(GuildStat.QuestSlots), Is.EqualTo((float)tier.QuestSlots).Within(0.001f));
                Assert.That(world.Stats.Get(GuildStat.MaxQuestTier), Is.EqualTo((float)tier.MaxQuestTier).Within(0.001f));
            }
        }

        /// <summary>
        /// Nothing produces these two stats yet — Quest Board and Armory ship post-launch.
        /// Failure mitigation reading anything but zero today would mean something started
        /// contributing to it by accident.
        /// </summary>
        [Test]
        public void ThePostLaunchStatsStillHaveNoProducer()
        {
            GameWorld world = Shipped.NewGuild();
            Shipped.SetLevels(world, tavern: 57, frontDesk: 52, barracks: 41, inn: 53, provisioner: 48);
            Shipped.MoveTo(world, "capital");

            Assert.That(world.Stats.Get(GuildStat.FailureRateReduction), Is.EqualTo(0f).Within(0.0001f),
                "Armory has not shipped, so the hardest quests keep a flat base failure rate.");
        }

        /// <summary>
        /// The rooms arrive on a schedule, and it is a data decision rather than a code one.
        ///
        /// This test used to assert the opposite — that every building was available from
        /// the start — and said in its own message that "a tier-gated building would be a
        /// design change, not a data one". It was right, the design change happened on
        /// Day 18, and the change was still one field on three assets. §6.2 of
        /// Vision_Revision.md put two shapes up, by-cost and by-tier, and leaned by-tier
        /// for legibility; the tuned model is by-tier and this is its answer.
        ///
        /// What is asserted is the SHAPE — every tier opens at least one new room, and the
        /// two the guild starts with are the two the Village gate requires — rather than
        /// the schedule itself, which a balance pass may move. The one thing that is not
        /// negotiable is that the starting tier can be played: §01's rule, one layer down.
        /// </summary>
        [Test]
        public void EachTierOpensAtLeastOneNewRoomAndVillageOpensWhatItsGateAsksFor()
        {
            GameWorld world = Shipped.NewGuild();
            List<GuildTierDefinition> tiers = Shipped.TiersInOrder();

            var openedAt = new Dictionary<int, List<string>>();
            foreach (BuildingDefinition building in world.Content.Buildings)
            {
                if (!openedAt.TryGetValue(building.MinimumTierOrder, out List<string> rooms))
                {
                    rooms = new List<string>();
                    openedAt[building.MinimumTierOrder] = rooms;
                }

                rooms.Add(building.Id);
            }

            for (int order = 0; order < tiers.Count - 1; order++)
            {
                Assert.That(openedAt.ContainsKey(order), Is.True,
                    $"{tiers[order].Id} opens no room the tier below it did not already have, so advancing " +
                    "to it is a reward with nothing in it.");
            }

            GuildTierDefinition village = tiers[0];
            foreach (BuildingLevelRequirement requirement in village.RequirementsToAdvance)
            {
                Assert.That(requirement.Building.MinimumTierOrder, Is.EqualTo(0),
                    $"Village's gate asks for {requirement.Building.Id}, which cannot be built at Village. " +
                    "That is a tier the player can never leave.");
            }
        }

        /// <summary>
        /// The Barracks is a Town room, so the settlement has to sleep the roster before
        /// there is anywhere to put it — otherwise a Village guild recruits nobody, earns
        /// no reputation and never reaches the tier that would sell it a bed.
        ///
        /// This is Day 4-5's opening deadlock for the third time, and it is solved the same
        /// way it was the first two: in data. What is different is that §01 now has the rule
        /// written down, and that the tier's own housing is the field which makes it true.
        /// </summary>
        [Test]
        public void EveryTierSleepsAtLeastOneAdventurerBeforeAnythingIsBuilt()
        {
            GameWorld world = Shipped.NewGuild();

            foreach (GuildTierDefinition tier in Shipped.TiersInOrder())
            {
                world.GuildState.AdvanceTo(tier);

                Assert.That(world.Roster.CapacityWith(world.Stats), Is.GreaterThanOrEqualTo(1),
                    $"{tier.Id} grants no beds of its own, and a guild that has built no Barracks there " +
                    "can recruit nobody — which is a tier with no way out of it.");
            }
        }

        private static void AssertSlots(string tierId, int expected)
        {
            GameWorld world = Shipped.NewGuild();
            Shipped.MoveTo(world, tierId);

            Assert.That(world.QuestLog.SlotsWith(world.Stats), Is.EqualTo(expected),
                $"{tierId} should run {expected} quest(s) at once.");
        }
    }
}
