using IdleGuild.App;
using IdleGuild.Core;
using IdleGuild.Quests;
using NUnit.Framework;

namespace IdleGuild.Tests
{
    /// <summary>
    /// The numbers a player actually sees, walked end to end: a curve evaluated, folded
    /// into a guild stat, consumed by <see cref="QuestResolution"/>.
    ///
    /// This is step 3 of the Days 10–11 verification pass. Doing it by hand meant
    /// starting a new guild, buying three things in order and reading four figures off
    /// the debug console; it is the same check, in about a millisecond.
    ///
    /// The figures here are marked BalanceCanary because they are *values*, not shapes.
    /// Day 13 and Day 21 will move them on purpose, and updating them is part of that
    /// work rather than a sign anything broke. The invariants live next door in
    /// <see cref="AssetInvariantTests"/> and are expected to survive both passes untouched.
    /// </summary>
    public sealed class QuestResolutionTests
    {
        [Test]
        [Category("BalanceCanary")]
        public void TheFirstQuestOfANewGuildReadsAsWritten()
        {
            GameWorld world = Shipped.NewGuild();

            QuestDefinition ratCellar = Shipped.Quest("rat_infested_cellar");
            float partyPower = Shipped.Adventurer("militia_recruit").BasePowerAt(1)
                               + world.Stats.Get(GuildStat.AdventurerPower);

            Assert.That(partyPower, Is.EqualTo(3f).Within(0.01f),
                "A level-1 Militia Recruit in a guild with no Barracks should contribute 3 power.");

            Assert.That(QuestResolution.DurationSeconds(ratCellar, partyPower), Is.EqualTo(51.96d).Within(0.1d));
            Assert.That(QuestResolution.FailureChance(ratCellar, partyPower, world.Stats), Is.EqualTo(0.0625f).Within(0.001f));
            Assert.That(QuestResolution.GoldReward(ratCellar, world.Stats), Is.EqualTo(48d).Within(0.01d),
                "With no Front Desk, Reward Yield is neutral and the quest pays exactly what the asset says.");
        }

        [Test]
        [Category("BalanceCanary")]
        public void TheFrontDeskRaisesWhatEveryQuestPays()
        {
            // Reward Yield moved from the Tavern to the Front Desk on Day 18 — a Tavern
            // that both multiplied contract gold and generated its own would compound
            // twice, which §2 of Vision_Revision.md is explicit about. The curve was
            // re-spaced from ninety levels onto fifty-two so that it lands on the same
            // ceiling, and its BASE was left alone — which is why every figure in this
            // test is unchanged and only the room's name moved.
            GameWorld world = Shipped.NewGuild();
            Shipped.SetLevels(world, frontDesk: 1);

            Assert.That(world.Stats.Get(GuildStat.RewardYield), Is.EqualTo(1.2f).Within(0.001f),
                "Reward Yield is a bonus fraction accumulated onto 1.0, so a level-1 Front Desk reads x1.20 — " +
                "never x0.20, which is what a multiplicative effect starting from zero would give.");

            Assert.That(QuestResolution.GoldReward(Shipped.Quest("rat_infested_cellar"), world.Stats),
                Is.EqualTo(57.6d).Within(0.01d));
        }

        [Test]
        [Category("BalanceCanary")]
        public void TheBarracksShortensAQuestAndCutsItsRisk()
        {
            // The Training Room was retired on Day 18 and the Barracks houses and drills
            // the roster in one room. Same base, re-spaced growth, same figures.
            GameWorld world = Shipped.NewGuild();
            Shipped.SetLevels(world, frontDesk: 1, barracks: 1);

            QuestDefinition ratCellar = Shipped.Quest("rat_infested_cellar");
            float partyPower = Shipped.Adventurer("militia_recruit").BasePowerAt(1)
                               + world.Stats.Get(GuildStat.AdventurerPower);

            Assert.That(partyPower, Is.EqualTo(5f).Within(0.01f), "3 from the archetype, +2 from a level-1 Barracks.");
            Assert.That(QuestResolution.DurationSeconds(ratCellar, partyPower), Is.EqualTo(40.25d).Within(0.1d));
            Assert.That(QuestResolution.FailureChance(ratCellar, partyPower, world.Stats), Is.EqualTo(0.0375f).Within(0.001f));
        }

        /// <summary>
        /// The clamps are why the Barracks' tree is short and steep while the Tavern's is
        /// long and shallow: power stops buying speed at four times the recommendation, so
        /// a geometric price for it would eventually buy nothing.
        /// </summary>
        [Test]
        public void SpeedIsClampedAtBothEnds()
        {
            QuestDefinition quest = Shipped.Quest("rat_infested_cellar");
            float recommended = quest.RecommendedPower;

            Assert.That(QuestResolution.DurationSeconds(quest, recommended * 0.0001f),
                Is.EqualTo(quest.BaseDurationSeconds / QuestResolution.MinimumSpeedMultiplier).Within(0.01d),
                "A hopeless party should take the longest a quest can ever take, not an unbounded amount of time.");

            Assert.That(QuestResolution.DurationSeconds(quest, recommended * 400f),
                Is.EqualTo(quest.BaseDurationSeconds / QuestResolution.MaximumSpeedMultiplier).Within(0.01d),
                "Power past 4x the recommendation buys no further speed, which is the ceiling the building " +
                "trees were shaped around.");
        }

        [Test]
        public void FailureRisesAsPowerFallsAndVanishesAtTwiceTheRecommendation()
        {
            GameWorld world = Shipped.NewGuild();
            QuestDefinition quest = Shipped.Quest("ruined_watchtower");

            Assert.That(QuestResolution.FailureChance(quest, quest.RecommendedPower, world.Stats),
                Is.EqualTo(quest.BaseFailureChance).Within(0.001f));

            // 1.5x, not 2x: the multiplier is (2 - ratio), so it only doubles at no power
            // at all. The comment on FailureChance said "doubles at half power" until this
            // test was written, which is the sort of thing a doc comment gets away with
            // until something checks it.
            Assert.That(QuestResolution.FailureChance(quest, quest.RecommendedPower * 0.5f, world.Stats),
                Is.EqualTo(quest.BaseFailureChance * 1.5f).Within(0.001f));

            Assert.That(QuestResolution.FailureChance(quest, 0f, world.Stats),
                Is.EqualTo(quest.BaseFailureChance * 2f).Within(0.001f));

            Assert.That(QuestResolution.FailureChance(quest, quest.RecommendedPower * 2f, world.Stats),
                Is.EqualTo(0f).Within(0.001f),
                "At twice the recommended power a quest is a formality. Anything above this is spare capacity.");
        }

        /// <summary>
        /// Step 8 of the pass, or the half of it a test can hold: sending three level-1
        /// recruits at the hardest quest in the game should hit both clamps at once. The
        /// other half — whether it is *appropriately* hard at a guild that earned its way
        /// to Capital — needs a played-in save and belongs to Day 14.
        /// </summary>
        [Test]
        [Category("BalanceCanary")]
        public void TheHardestQuestOverwhelmsAStarterParty()
        {
            GameWorld world = Shipped.NewGuild();
            Shipped.SetLevels(world, frontDesk: 1, barracks: 1);
            Shipped.MoveTo(world, "capital");

            QuestDefinition roost = Shipped.Quest("dragons_roost");
            float partyPower = 3f * (Shipped.Adventurer("militia_recruit").BasePowerAt(1)
                                     + world.Stats.Get(GuildStat.AdventurerPower));

            Assert.That(QuestResolution.IsAvailable(roost, world.GuildState.CurrentTier.Order, world.Stats), Is.True,
                "Dragon's Roost should be on offer at Capital.");

            Assert.That(QuestResolution.DurationSeconds(roost, partyPower), Is.EqualTo(720d).Within(1d));
            Assert.That(QuestResolution.FailureChance(roost, partyPower, world.Stats), Is.EqualTo(0.4f).Within(0.01f),
                "Twice the base failure rate — the ceiling. Three farmhands at a dragon should look like this.");
        }

        /// <summary>
        /// The hardest quest has to be harder than the one below it at the same party, or
        /// the tier it unlocks with is a reward with nothing in it.
        /// </summary>
        [Test]
        public void EachQuestTierIsHarderThanTheOneBelow()
        {
            GameWorld world = Shipped.NewGuild();
            QuestDefinition[] byTier =
            {
                Shipped.Quest("rat_infested_cellar"),
                Shipped.Quest("ruined_watchtower"),
                Shipped.Quest("sunken_crypt"),
                Shipped.Quest("dragons_roost")
            };

            for (int index = 1; index < byTier.Length; index++)
            {
                Assert.That(byTier[index].QuestTier, Is.GreaterThan(byTier[index - 1].QuestTier));

                Assert.That(byTier[index].RecommendedPower, Is.GreaterThan(byTier[index - 1].RecommendedPower),
                    $"'{byTier[index].Id}' asks for no more power than '{byTier[index - 1].Id}'.");

                Assert.That(QuestResolution.GoldReward(byTier[index], world.Stats),
                    Is.GreaterThan(QuestResolution.GoldReward(byTier[index - 1], world.Stats)),
                    $"'{byTier[index].Id}' pays no more than '{byTier[index - 1].Id}' despite being harder.");
            }
        }
    }
}
