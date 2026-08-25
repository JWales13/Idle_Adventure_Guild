using IdleGuild.App;
using IdleGuild.App.Saves;
using IdleGuild.Core;
using IdleGuild.Core.Events;
using IdleGuild.Guild;
using IdleGuild.Staff;
using NUnit.Framework;

namespace IdleGuild.Tests
{
    /// <summary>
    /// The payroll, and the ratchet it would have been.
    ///
    /// §6C's third finding is that staff slots are a one-way ratchet — fill them cheaply
    /// and you can never upgrade — and that this is the Days 10-11 bed problem, which
    /// Day 12 had to retrofit a fix for. These tests are the Day 12 roster tests wearing
    /// staff clothes, written on the same day as the feature rather than three days
    /// later, which was the whole instruction.
    ///
    /// They hire against the <i>gate</i> rather than against a number — the pattern Day
    /// 12 established deliberately, so that a later balance pass changing how many slots
    /// a Tavern gives leaves every one of them just as true.
    /// </summary>
    public sealed class StaffRatchetTests
    {
        [SetUp]
        public void ClearBus()
        {
            EventBus.ClearAll();
        }

        private static GameWorld PayrollGuild(out StaffService staff, out StaffDefinition cheap, out StaffDefinition better)
        {
            // Three slots, and two kinds of employee. Enough to fill a payroll and want
            // it back.
            BuildingDefinition tavern = TradeFixture.EarningRoom(
                "tavern", demandPerHour: 1000f, seatsAtLevelOne: 100f, spendPerCustomer: 5f, staffSlots: 3f);
            cheap = TradeFixture.Employee("potboy", hireCost: 10d, servicePerHour: 10f);
            better = TradeFixture.Employee("server", hireCost: 100d, servicePerHour: 200f, minimumTierOrder: 1);

            GameContent content = TradeFixture.Catalogue(
                new[] { tavern },
                new[]
                {
                    TradeFixture.Tier("village", 0, baseServicePerHour: 5f),
                    TradeFixture.Tier("town", 1, marketSize: 6f, baseServicePerHour: 5f)
                },
                new[] { cheap, better },
                startingGold: 10000d);

            GameWorld world = TradeFixture.Guild(content, "tavern");
            staff = new StaffService(world);
            return world;
        }

        [Test]
        public void AFullPayrollCanAlwaysMakeRoom()
        {
            // The Day 12 test, one subsystem over: fill every slot with the cheapest help
            // and the way back has to exist. Before Day 12 the roster's equivalent of
            // this was simply impossible, and a player who spent their spare beds during
            // City could never hire the Legendary that Capital unlocked.
            GameWorld world = PayrollGuild(out StaffService staff, out StaffDefinition cheap, out StaffDefinition _);

            while (staff.Preview(cheap) == HireOutcome.Hired)
            {
                staff.TryHire(cheap, out StaffMember _);
            }

            Assert.That(staff.Preview(cheap), Is.EqualTo(HireOutcome.NoFreeSlot));
            Assert.That(staff.TryLetGoLeastCapable(out StaffMember released), Is.EqualTo(LetGoOutcome.LetGo));
            Assert.That(released, Is.Not.Null);
            Assert.That(staff.Preview(cheap), Is.EqualTo(HireOutcome.Hired),
                "A payroll with no way out is a decision the player can only get wrong once.");
        }

        [Test]
        public void TheLeastCapableIsTheOneWhoGoes()
        {
            GameWorld world = PayrollGuild(out StaffService staff, out StaffDefinition cheap, out StaffDefinition better);
            world.GuildState.AdvanceTo(world.Content.FindTier("town"));

            staff.TryHire(better, out StaffMember _);
            staff.TryHire(cheap, out StaffMember _);

            staff.TryLetGoLeastCapable(out StaffMember released);

            Assert.That(released.Definition, Is.EqualTo(cheap),
                "Making room for somebody better must not throw the better one out.");
        }

        [Test]
        public void LettingSomebodyGoRefundsNothing()
        {
            // The same rule adventurers follow. A rebate would make hire-and-fire a free
            // churn loop; what the payroll lacked was reversibility, not a discount.
            GameWorld world = PayrollGuild(out StaffService staff, out StaffDefinition cheap, out StaffDefinition _);
            staff.TryHire(cheap, out StaffMember hired);

            double afterHiring = world.Economy.Get(CurrencyType.Gold);
            staff.TryLetGo(hired);

            Assert.That(world.Economy.Get(CurrencyType.Gold), Is.EqualTo(afterHiring));
        }

        [Test]
        public void LettingSomebodyGoStopsTheirWagesAndFreesTheirSlot()
        {
            GameWorld world = PayrollGuild(out StaffService staff, out StaffDefinition cheap, out StaffDefinition _);
            SimulationClock clock = TradeFixture.Clock(world);
            staff.TryHire(cheap, out StaffMember hired);

            double withThem = clock.Trade.WagesPerHour();
            staff.TryLetGo(hired);

            Assert.That(clock.Trade.WagesPerHour(), Is.LessThan(withThem));
            Assert.That(staff.Employed, Is.EqualTo(0));
        }

        [Test]
        public void HiringIsRefusedByTierBeforeSlotAndBySlotBeforeGold()
        {
            // The refusals are pinned in the order the player can clear them, exactly as
            // PreviewDismissal's two are. Every refusal in this game names what is in the
            // way, and naming the wrong obstacle sends the player to fix something that
            // was not stopping them — so a subtly unhelpful sentence should be a failing
            // test rather than a shrug.
            GameWorld world = PayrollGuild(out StaffService staff, out StaffDefinition cheap, out StaffDefinition better);
            StaffDefinition ruinous = TradeFixture.Employee("steward", hireCost: 1000000000d, servicePerHour: 9000f);

            Assert.That(staff.Preview(better), Is.EqualTo(HireOutcome.TierLocked),
                "Tier first: nothing else the player does can help until the settlement grows.");

            world.GuildState.AdvanceTo(world.Content.FindTier("town"));

            // A slot free and nowhere near enough gold: the money is the obstacle.
            Assert.That(staff.Preview(ruinous), Is.EqualTo(HireOutcome.UnknownStaff),
                "Guard: this one is not in the catalogue, so it can never reach the gold check.");

            while (staff.Preview(cheap) == HireOutcome.Hired)
            {
                staff.TryHire(cheap, out StaffMember _);
            }

            Assert.That(staff.Preview(better), Is.EqualTo(HireOutcome.NoFreeSlot),
                "Slot before gold: this player can afford a Server and still has nowhere to put one, " +
                "so telling them about the price would send them off to earn gold they already have.");
            Assert.That(
                world.Economy.CanAfford(CurrencyType.Gold, better.HireCostGold), Is.True,
                "The assertion above only means anything while the gold is genuinely there.");
        }

        [Test]
        public void AnEmployeeOutsideTheCatalogueIsNotHireable()
        {
            PayrollGuild(out StaffService staff, out StaffDefinition _, out StaffDefinition _);
            StaffDefinition stranger = TradeFixture.Employee("stranger", 1d, 1f);

            Assert.That(staff.Preview(stranger), Is.EqualTo(HireOutcome.UnknownStaff));
            Assert.That(staff.TryHire(stranger, out StaffMember hired), Is.EqualTo(HireOutcome.UnknownStaff));
            Assert.That(hired, Is.Null);
        }

        [Test]
        public void ARefusedDismissalDoesNotHalfHappen()
        {
            GameWorld world = PayrollGuild(out StaffService staff, out StaffDefinition cheap, out StaffDefinition _);
            staff.TryHire(cheap, out StaffMember hired);
            staff.TryLetGo(hired);

            Assert.That(staff.TryLetGo(hired), Is.EqualTo(LetGoOutcome.UnknownStaff));
            Assert.That(world.Staff.Count, Is.EqualTo(0));
        }

        // ---- the save -------------------------------------------------------------

        [Test]
        public void ThePayrollAndTheTillSurviveARoundTrip()
        {
            GameWorld world = PayrollGuild(out StaffService staff, out StaffDefinition cheap, out StaffDefinition _);
            SimulationClock clock = TradeFixture.Clock(world);
            staff.TryHire(cheap, out StaffMember _);
            clock.Advance(600d);

            SaveGameData data = SaveCapture.Capture(world, clock, System.DateTime.UtcNow);

            GameWorld reloaded = TradeFixture.Guild(world.Content);
            SimulationClock reloadedClock = TradeFixture.Clock(reloaded);
            SaveRestoreReport report = SaveRestore.Restore(reloaded, reloadedClock, data);

            Assert.That(report.HasRepairs, Is.False, $"The restore had to repair something: {report}.");
            Assert.That(reloaded.Staff.Count, Is.EqualTo(1));
            Assert.That(reloaded.Staff.Employees[0].Definition, Is.EqualTo(cheap));
            Assert.That(reloadedClock.GrossEarned, Is.EqualTo(clock.GrossEarned).Within(0.001d));
            Assert.That(reloadedClock.Takings.WaitingCustomers,
                Is.EqualTo(clock.Takings.WaitingCustomers).Within(0.001d));
        }

        [Test]
        public void ASaveFromBeforeTheRevisionRestoresAsAGuildWithNoStaffAndNoRepairs()
        {
            // The compatibility rule's promise, tested rather than assumed: fields are
            // only ever added, and everything an old save did not write arrives at a
            // neutral default. JsonUtility leaves an absent array NULL rather than empty,
            // so this is the case that would otherwise throw on load — and every one of
            // the four checked-in fixtures is exactly this shape.
            GameWorld world = PayrollGuild(out StaffService staff, out StaffDefinition cheap, out StaffDefinition _);
            SimulationClock clock = TradeFixture.Clock(world);
            staff.TryHire(cheap, out StaffMember _);

            SaveGameData data = SaveCapture.Capture(world, clock, System.DateTime.UtcNow);
            data.Staff = null;
            data.Trade = null;

            GameWorld reloaded = TradeFixture.Guild(world.Content);
            SimulationClock reloadedClock = TradeFixture.Clock(reloaded);
            SaveRestoreReport report = SaveRestore.Restore(reloaded, reloadedClock, data);

            Assert.That(report.HasRepairs, Is.False,
                "A guild that genuinely had no staff must not be reported as damaged.");
            Assert.That(report.DroppedStaff, Is.EqualTo(0));
            Assert.That(reloaded.Staff.Count, Is.EqualTo(0));
            Assert.That(reloadedClock.GrossEarned, Is.EqualTo(0d));
        }

        [Test]
        public void AnEmployeeWhoseKindNoLongerExistsIsDroppedAndCounted()
        {
            GameWorld world = PayrollGuild(out StaffService staff, out StaffDefinition cheap, out StaffDefinition _);
            SimulationClock clock = TradeFixture.Clock(world);
            staff.TryHire(cheap, out StaffMember _);

            SaveGameData data = SaveCapture.Capture(world, clock, System.DateTime.UtcNow);
            data.Staff[0].DefinitionId = "a_kind_of_help_no_build_has_ever_had";

            GameWorld reloaded = TradeFixture.Guild(world.Content);
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, new System.Text.RegularExpressions.Regex("employee of kind"));
            SaveRestoreReport report = SaveRestore.Restore(reloaded, TradeFixture.Clock(reloaded), data);

            Assert.That(report.DroppedStaff, Is.EqualTo(1));
            Assert.That(report.HasRepairs, Is.True, "Dropping somebody is a repair and has to be reported as one.");
            Assert.That(reloaded.Staff.Count, Is.EqualTo(0));
        }

        [Test]
        public void StartingOverEmptiesThePayrollAndTheTillAndNotJustTheFile()
        {
            // The Day 6 lesson, applied to the two things Day 16 added: a destructive
            // action that does not also invalidate the live state it describes will be
            // undone by whatever writes that state next.
            GameWorld world = PayrollGuild(out StaffService staff, out StaffDefinition cheap, out StaffDefinition _);
            SimulationClock clock = TradeFixture.Clock(world);
            staff.TryHire(cheap, out StaffMember _);
            clock.Advance(600d);

            SaveRestore.Reset(world, clock);

            Assert.That(world.Staff.Count, Is.EqualTo(0));
            Assert.That(clock.GrossEarned, Is.EqualTo(0d));
            Assert.That(clock.WagesPaid, Is.EqualTo(0d));
            Assert.That(clock.Takings.WaitingCustomers, Is.EqualTo(0d));
            Assert.That(clock.Takings.LifetimeTakings, Is.EqualTo(0d));
        }

        // ---- the shipping ladder, which does not exist yet -------------------------

        [Test]
        public void AHigherStaffTierNeverCostsMoreGoldPerPointOfService()
        {
            // Day 13's invariant, in staff clothing, and it is owed one. The rarity
            // ladder tripled in training cost per band while power only doubled, and in
            // four days of hunting that exact symptom nothing had ever divided one
            // authored number by the other, because power lived on one curve and price
            // on another.
            //
            // THIS TEST IS IGNORED TODAY AND THAT IS THE POINT. No staff assets are
            // authored yet, and the reason is written up in §6 of
            // Docs/Day16_Staff_And_Revenue.md: the tuned model hires a hundred and five
            // Potboys and never buys a single employee from the three tiers above, at
            // every integration step — not because they are mispriced but because the
            // model can only ever APPEND staff, so slots once filled with the cheapest
            // help are filled forever and the ladder is unreachable at any price. The
            // ladder was therefore free to be priced arbitrarily and was: gold per point
            // of service climbs 0.47, 2.06, 8.63, 32.69.
            //
            // Ignored rather than absent, and ignored rather than vacuously green,
            // because a canary set that does not watch a value is quieter than no canary
            // set — its silence reads as a pass. This one says out loud that it is not
            // watching anything yet.
            if (Shipped.Content.Staff.Length < 2)
            {
                Assert.Ignore(
                    "Fewer than two staff kinds are authored, so there is no ladder to check. " +
                    "See §6 of Docs/Day16_Staff_And_Revenue.md for why they were deliberately not written today.");
            }

            StaffDefinition[] ladder = Shipped.StaffInTierOrder();
            for (int index = 1; index < ladder.Length; index++)
            {
                Assert.That(
                    ladder[index].GoldPerServicePoint,
                    Is.LessThanOrEqualTo(ladder[index - 1].GoldPerServicePoint * 1.001d),
                    $"{ladder[index].DisplayName} costs more per point of service than " +
                    $"{ladder[index - 1].DisplayName}, so nobody will ever climb to it.");
            }
        }

        [Test]
        public void EveryStaffAssetOnDiskIsListedInTheCatalogue()
        {
            foreach (StaffDefinition onDisk in Shipped.EverythingOnDisk<StaffDefinition>())
            {
                Assert.That(Shipped.Content.FindStaff(onDisk.Id), Is.EqualTo(onDisk),
                    $"{onDisk.name} exists on disk but the catalogue does not list it, so the game cannot hire one.");
            }
        }
    }
}
