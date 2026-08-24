using IdleGuild.Adventurers;
using IdleGuild.App;
using IdleGuild.Core;
using NUnit.Framework;

namespace IdleGuild.Tests
{
    /// <summary>
    /// Step 5 of the pass: three gates stand in front of a hire, and which one is in the
    /// way has to be the right one.
    ///
    /// This matters to the player rather than only to the code. A tier lock is something
    /// they travel past; a Tavern lock is something they spend past; a bed shortage is
    /// something they build past. The screen prints a different sentence for each, and
    /// the sentence is only useful if the outcome underneath it is accurate.
    ///
    /// <see cref="RecruitmentService.Preview"/> checks tier before rarity, so tier wins
    /// when both apply — deliberate, and pinned here so a later reorder is a failing test
    /// rather than a subtly wrong explanation on a card.
    /// </summary>
    public sealed class RecruitmentGateTests
    {
        [Test]
        public void TheTavernIsWhatHoldsBackAnUncommonHireAtVillage()
        {
            GameWorld world = Wealthy();
            Shipped.SetLevels(world, tavern: 8, inn: 1);

            RecruitmentService recruitment = new RecruitmentService(world);
            Assert.That(recruitment.Preview(Shipped.Adventurer("hedge_knight")), Is.EqualTo(RecruitOutcome.RarityLocked),
                "At Tavern 8 the guild attracts Common only, and the Hedge Knight is Uncommon.");

            Shipped.SetLevels(world, tavern: 9);
            Assert.That(recruitment.Preview(Shipped.Adventurer("hedge_knight")), Is.EqualTo(RecruitOutcome.Recruited),
                "Tavern 9 is where Uncommon opens.");
        }

        /// <summary>
        /// The anti-tunnelling backstop. A player can push the Tavern hard while ignoring
        /// the Training Room and the Inn, so the Tavern alone must not be able to buy
        /// access to content a whole tier away.
        /// </summary>
        [Test]
        public void AMaxedTavernStillCannotOutrunTheTierGate()
        {
            GameWorld world = Wealthy();
            Shipped.SetLevels(world, tavern: 90, inn: 30);

            RecruitmentService recruitment = new RecruitmentService(world);

            Assert.That(recruitment.Preview(Shipped.Adventurer("wandering_ranger")), Is.EqualTo(RecruitOutcome.TierLocked),
                "The Ranger appears at Town. A Village guild with a level-90 Tavern still cannot have one.");

            Assert.That(recruitment.Preview(Shipped.Adventurer("dragonsworn_champion")), Is.EqualTo(RecruitOutcome.TierLocked));
        }

        [Test]
        public void TheEpicBandOpensOnTheTavernRatherThanTheTier()
        {
            GameWorld world = Wealthy();
            Shipped.MoveTo(world, "city");
            Shipped.SetLevels(world, tavern: 24, inn: 21);

            RecruitmentService recruitment = new RecruitmentService(world);
            AdventurerDefinition battlemage = Shipped.Adventurer("arcane_battlemage");

            Assert.That(recruitment.Preview(battlemage), Is.EqualTo(RecruitOutcome.RarityLocked),
                "City is far enough for the Battlemage; Tavern 24 is not.");

            Shipped.SetLevels(world, tavern: 25);
            Assert.That(recruitment.Preview(battlemage), Is.EqualTo(RecruitOutcome.Recruited));
        }

        /// <summary>
        /// Written down as a test because it reads like a bug and is not: reaching Capital
        /// with the debug console rather than by playing leaves the Tavern far below 32,
        /// and the Champion then reports the Tavern gate rather than the tier gate. That
        /// is correct — Tavern 32 is genuinely still in front of it.
        /// </summary>
        [Test]
        public void AtCapitalOnALowTavernTheChampionIsRarityLockedNotTierLocked()
        {
            GameWorld world = Wealthy();
            Shipped.MoveTo(world, "capital");
            Shipped.SetLevels(world, tavern: 31, inn: 21);

            RecruitmentService recruitment = new RecruitmentService(world);
            AdventurerDefinition champion = Shipped.Adventurer("dragonsworn_champion");

            Assert.That(recruitment.Preview(champion), Is.EqualTo(RecruitOutcome.RarityLocked));

            Shipped.SetLevels(world, tavern: 32);
            Assert.That(recruitment.Preview(champion), Is.EqualTo(RecruitOutcome.Recruited),
                "Tavern 32 is where Legendary opens.");
        }

        [Test]
        public void TheTavernAttractsOneBandHigherAtNineSeventeenTwentyFiveAndThirtyTwo()
        {
            GameWorld world = Shipped.NewGuild();
            RecruitmentService recruitment = new RecruitmentService(world);

            AssertAttracts(world, recruitment, 1, Rarity.Common);
            AssertAttracts(world, recruitment, 8, Rarity.Common);
            AssertAttracts(world, recruitment, 9, Rarity.Uncommon);
            AssertAttracts(world, recruitment, 16, Rarity.Uncommon);
            AssertAttracts(world, recruitment, 17, Rarity.Rare);
            AssertAttracts(world, recruitment, 24, Rarity.Rare);
            AssertAttracts(world, recruitment, 25, Rarity.Epic);
            AssertAttracts(world, recruitment, 31, Rarity.Epic);
            AssertAttracts(world, recruitment, 32, Rarity.Legendary);
            AssertAttracts(world, recruitment, 90, Rarity.Legendary);
        }

        [Test]
        public void TheInnIsWhatRunsOutLast()
        {
            GameWorld world = Wealthy();
            Shipped.SetLevels(world, tavern: 9, inn: 1);

            RecruitmentService recruitment = new RecruitmentService(world);
            AdventurerDefinition recruit = Shipped.Adventurer("militia_recruit");

            Assert.That(recruitment.TotalHousing, Is.EqualTo(2), "A level-1 Inn sleeps two.");

            Assert.That(recruitment.TryRecruit(recruit, out Adventurer _), Is.EqualTo(RecruitOutcome.Recruited));
            Assert.That(recruitment.TryRecruit(recruit, out Adventurer _), Is.EqualTo(RecruitOutcome.Recruited));
            Assert.That(recruitment.Preview(recruit), Is.EqualTo(RecruitOutcome.HousingFull),
                "Both beds are taken, and the gate that reports it should be the Inn rather than the Tavern.");
        }

        [Test]
        public void APennilessGuildIsToldItIsPennilessAndNothingElse()
        {
            GameWorld world = Shipped.NewGuild();
            world.Economy.Restore(CurrencyType.Gold, 0d);
            Shipped.SetLevels(world, tavern: 9, inn: 3);

            RecruitmentService recruitment = new RecruitmentService(world);
            Assert.That(recruitment.Preview(Shipped.Adventurer("hedge_knight")), Is.EqualTo(RecruitOutcome.Unaffordable));
        }

        /// <summary>
        /// A level-1 Inn grants two beds, not fifty. Day 4–5 shipped this asset with the
        /// *cost* curve in the Housing Capacity slot and nothing caught it until the YAML
        /// was read back by hand — a wrong curve looks exactly like a right one in the
        /// Inspector.
        /// </summary>
        [Test]
        [Category("BalanceCanary")]
        public void TheInnGrantsBedsAndNotItsOwnPriceList()
        {
            GameWorld world = Shipped.NewGuild();

            Shipped.SetLevels(world, inn: 1);
            Assert.That(world.Roster.CapacityWith(world.Stats), Is.EqualTo(2));

            Shipped.SetLevels(world, inn: 21);
            Assert.That(world.Roster.CapacityWith(world.Stats), Is.EqualTo(12),
                "Twelve beds at the City gate — four parties of three at Capital.");

            Shipped.SetLevels(world, inn: 30);
            Assert.That(world.Roster.CapacityWith(world.Stats), Is.EqualTo(16));
        }

        private static GameWorld Wealthy()
        {
            GameWorld world = Shipped.NewGuild();
            world.Economy.Grant(CurrencyType.Gold, 10_000_000d);
            return world;
        }

        private static void AssertAttracts(GameWorld world, RecruitmentService recruitment, int tavernLevel, Rarity expected)
        {
            Shipped.SetLevels(world, tavern: tavernLevel);
            Assert.That(recruitment.MaximumRecruitableRarity(), Is.EqualTo(expected),
                $"At Tavern {tavernLevel} the guild should attract up to {expected}.");
        }
    }
}
