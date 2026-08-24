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
            Shipped.SetLevels(world, tavern: 90, trainingRoom: 40, inn: 30);
            Shipped.MoveTo(world, "capital");

            Assert.That(world.Stats.Get(GuildStat.FailureRateReduction), Is.EqualTo(0f).Within(0.0001f),
                "Armory has not shipped, so the hardest quests keep a flat base failure rate.");
        }

        [Test]
        public void EveryBuildingIsAvailableFromTheStart()
        {
            GameWorld world = Shipped.NewGuild();

            foreach (BuildingDefinition building in world.Content.Buildings)
            {
                Assert.That(world.GuildState.IsAvailable(building), Is.True,
                    $"'{building.Id}' is not available at the starting tier. The MVP set is three buildings, all " +
                    "from Village; a tier-gated building would be a design change, not a data one.");
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
