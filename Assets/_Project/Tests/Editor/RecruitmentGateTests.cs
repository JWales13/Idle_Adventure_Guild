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
            Shipped.SetLevels(world, tavern: 8);

            RecruitmentService recruitment = new RecruitmentService(world);
            Assert.That(recruitment.Preview(Shipped.Adventurer("hedge_knight")), Is.EqualTo(RecruitOutcome.RarityLocked),
                "At Tavern 8 the guild attracts Common only, and the Hedge Knight is Uncommon.");

            Shipped.SetLevels(world, tavern: 9);
            Assert.That(recruitment.Preview(Shipped.Adventurer("hedge_knight")), Is.EqualTo(RecruitOutcome.Recruited),
                "Tavern 9 is where Uncommon opens.");
        }

        /// <summary>
        /// The anti-tunnelling backstop. A player can push the Tavern hard while ignoring
        /// the Front Desk and the Barracks, so the Tavern alone must not be able to buy
        /// access to content a whole tier away.
        /// </summary>
        [Test]
        public void AMaxedTavernStillCannotOutrunTheTierGate()
        {
            GameWorld world = Wealthy();
            Shipped.SetLevels(world, tavern: 57, barracks: 41);

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
            Shipped.SetLevels(world, tavern: 24);

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
            Shipped.SetLevels(world, tavern: 31);

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
            AssertAttracts(world, recruitment, 57, Rarity.Legendary);
        }

        [Test]
        public void TheBarracksIsWhatRunsOutLast()
        {
            // Was TheInnIsWhatRunsOutLast until Day 18. The Inn is purely a hotel now and
            // grants no beds at all; the roster sleeps in the Barracks, which does not
            // exist at Village — so the tier's own two beds are what a new guild has, and
            // they are what runs out.
            GameWorld world = Wealthy();
            Shipped.SetLevels(world, tavern: 9);

            RecruitmentService recruitment = new RecruitmentService(world);
            AdventurerDefinition recruit = Shipped.Adventurer("militia_recruit");

            Assert.That(recruitment.TotalHousing, Is.EqualTo(2),
                "A Village guild with no Barracks sleeps two, granted by the settlement rather than by a building.");

            Assert.That(recruitment.TryRecruit(recruit, out Adventurer _), Is.EqualTo(RecruitOutcome.Recruited));
            Assert.That(recruitment.TryRecruit(recruit, out Adventurer _), Is.EqualTo(RecruitOutcome.Recruited));
            Assert.That(recruitment.Preview(recruit), Is.EqualTo(RecruitOutcome.HousingFull),
                "Both beds are taken, and the gate that reports it should be housing rather than the Tavern.");
        }

        [Test]
        public void APennilessGuildIsToldItIsPennilessAndNothingElse()
        {
            GameWorld world = Shipped.NewGuild();
            world.Economy.Restore(CurrencyType.Gold, 0d);
            Shipped.SetLevels(world, tavern: 9);

            RecruitmentService recruitment = new RecruitmentService(world);
            Assert.That(recruitment.Preview(Shipped.Adventurer("hedge_knight")), Is.EqualTo(RecruitOutcome.Unaffordable));
        }

        /// <summary>
        /// A level-1 Barracks does not grant its own price list. Day 4-5 shipped the Inn
        /// with the *cost* curve in the Housing Capacity slot, so a level-1 Inn granted
        /// fifty beds, and nothing caught it until the YAML was read back by hand — a
        /// wrong curve looks exactly like a right one in the Inspector. The effect moved
        /// house on Day 18 and the canary moved with it, which is the point of having one.
        ///
        /// The ceiling is deliberately unchanged at sixteen: the bed curve was re-spaced
        /// from the Inn's thirty levels onto the Barracks' forty-one so that it lands on
        /// exactly the number it used to, rather than taking the model's twenty. The
        /// contract economy is not being re-tuned today. See §3 of Docs/Day18_The_Five_Rooms.md.
        /// </summary>
        [Test]
        [Category("BalanceCanary")]
        public void TheBarracksGrantsBedsAndNotItsOwnPriceList()
        {
            GameWorld world = Shipped.NewGuild();

            Shipped.SetLevels(world, barracks: 0);
            Assert.That(world.Roster.CapacityWith(world.Stats), Is.EqualTo(2),
                "The settlement sleeps two before any Barracks is built, or a Village guild can recruit nobody.");

            Shipped.SetLevels(world, barracks: 1);
            Assert.That(world.Roster.CapacityWith(world.Stats), Is.EqualTo(4));

            Shipped.SetLevels(world, barracks: 27);
            Assert.That(world.Roster.CapacityWith(world.Stats), Is.EqualTo(12),
                "Twelve beds at the City gate — four parties of three at Capital.");

            Shipped.SetLevels(world, barracks: 41);
            Assert.That(world.Roster.CapacityWith(world.Stats), Is.EqualTo(16),
                "Sixteen at the ceiling, which is what a maxed Inn granted before the effect moved.");
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
