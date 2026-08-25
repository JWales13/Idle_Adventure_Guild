using IdleGuild.App;
using IdleGuild.Core;
using IdleGuild.Core.Events;
using IdleGuild.Guild;
using IdleGuild.Staff;
using NUnit.Framework;

namespace IdleGuild.Tests
{
    /// <summary>
    /// The revenue engine's mechanism: three levers that must not overlap, staff shared
    /// out by priority, wages that come out of the till, and a tap that cannot invent a
    /// customer.
    ///
    /// Shape rather than number throughout, per §2 of Docs/Tests.md — no assertion here
    /// names a figure from any room, because no room produces these stats yet. What is
    /// pinned is behaviour that a balance pass must never change: that opening a room
    /// cannot make the guild poorer, that eight hours away cannot go backwards, that a
    /// per-room stat read guild-wide is zero rather than plausible.
    /// </summary>
    public sealed class TradeEngineTests
    {
        [SetUp]
        public void ClearBus()
        {
            EventBus.ClearAll();
        }

        // ---- the three levers ---------------------------------------------------

        [Test]
        public void DemandCapsARoomWhoseSeatsOutrunTheCrowd()
        {
            // Twenty seats at forty turns is eight hundred an hour of capacity against a
            // village that contains ten people. The room is not the constraint.
            BuildingDefinition tavern = TradeFixture.EarningRoom("tavern", demandPerHour: 10f, seatsAtLevelOne: 20f, spendPerCustomer: 1f);
            GameContent content = TradeFixture.Catalogue(
                new[] { tavern }, new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 1000f) });
            GameWorld world = TradeFixture.Guild(content, "tavern");

            RoomTrade trade = TradeFixture.Clock(world).Trade.TradeFor(tavern);

            Assert.That(trade.WantPerHour, Is.EqualTo(10d).Within(0.001d));
            Assert.That(trade.IsTurningPeopleAway, Is.False, "The crowd is the ceiling here, not the seating.");
        }

        [Test]
        public void SeatsCapARoomWhoseCrowdOutrunsIt()
        {
            BuildingDefinition tavern = TradeFixture.EarningRoom("tavern", demandPerHour: 4000f, seatsAtLevelOne: 2f, spendPerCustomer: 1f);
            GameContent content = TradeFixture.Catalogue(
                new[] { tavern }, new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 100000f) });
            GameWorld world = TradeFixture.Guild(content, "tavern");

            RoomTrade trade = TradeFixture.Clock(world).Trade.TradeFor(tavern);

            Assert.That(trade.WantPerHour, Is.EqualTo(80d).Within(0.001d), "Two seats turning over forty times.");
            Assert.That(trade.IsTurningPeopleAway, Is.True,
                "Seats are the ceiling, which is the single most useful thing a room panel can say.");
        }

        [Test]
        public void AdvancingATierMultipliesDemandAndLeavesTheRoomsUntouched()
        {
            // §3.1's rhythm: the settlement grows around the hall, so everything the
            // player owns becomes insufficient at the moment they are rewarded. If a
            // room's own level moved demand, two of the three levers would be one.
            BuildingDefinition tavern = TradeFixture.EarningRoom("tavern", demandPerHour: 10f, seatsAtLevelOne: 100f, spendPerCustomer: 1f);
            GuildTierDefinition town = TradeFixture.Tier("town", 1, marketSize: 6f, baseServicePerHour: 100000f);
            GameContent content = TradeFixture.Catalogue(
                new[] { tavern },
                new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 100000f), town });
            GameWorld world = TradeFixture.Guild(content, "tavern");
            TradeService trade = TradeFixture.Clock(world).Trade;

            double atVillage = trade.TradeFor(tavern).DemandPerHour;
            world.GuildState.AdvanceTo(town);
            double atTown = trade.TradeFor(tavern).DemandPerHour;

            Assert.That(atVillage, Is.EqualTo(10d).Within(0.001d));
            Assert.That(atTown, Is.EqualTo(60d).Within(0.001d));
        }

        // ---- allocation ----------------------------------------------------------

        [Test]
        public void StaffServeTheMostValuableCustomFirst()
        {
            // Ten an hour of service against two rooms wanting ten each. The room paying
            // fifty a head takes all of it; the room paying one takes nothing.
            BuildingDefinition inn = TradeFixture.EarningRoom("inn", demandPerHour: 10f, seatsAtLevelOne: 1f, spendPerCustomer: 50f);
            BuildingDefinition tavern = TradeFixture.EarningRoom("tavern", demandPerHour: 10f, seatsAtLevelOne: 1f, spendPerCustomer: 1f);
            GameContent content = TradeFixture.Catalogue(
                new[] { tavern, inn }, new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 10f) });
            GameWorld world = TradeFixture.Guild(content, "tavern", "inn");
            TradeService trade = TradeFixture.Clock(world).Trade;

            Assert.That(trade.TradeFor(inn).ServedPerHour, Is.EqualTo(10d).Within(0.001d));
            Assert.That(trade.TradeFor(tavern).ServedPerHour, Is.EqualTo(0d).Within(0.001d));
        }

        [Test]
        public void OpeningARoomNeverMakesTheGuildPoorer()
        {
            // Finding #10, pinned. Sharing staff proportionally meant opening the
            // Provisioner diluted the staff already serving the Tavern and Inn — 137,000
            // an hour of damage to gain 4,000 — so its payback was negative and a model
            // sitting on 276 million gold never bought a 9,000-gold room. That is a
            // design failure and not merely a modelling one: a player would have
            // experienced it as the game getting worse when they built something.
            BuildingDefinition inn = TradeFixture.EarningRoom("inn", demandPerHour: 10f, seatsAtLevelOne: 1f, spendPerCustomer: 50f);
            BuildingDefinition provisioner = TradeFixture.EarningRoom("provisioner", demandPerHour: 900f, seatsAtLevelOne: 90f, spendPerCustomer: 1f);
            GameContent content = TradeFixture.Catalogue(
                new[] { inn, provisioner }, new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 10f) });
            GameWorld world = TradeFixture.Guild(content, "inn");
            TradeService trade = TradeFixture.Clock(world).Trade;

            double before = trade.GrossPerHour();
            world.GuildState.SetLevel("provisioner", 1);
            double after = trade.GrossPerHour();

            Assert.That(after, Is.GreaterThanOrEqualTo(before),
                "Opening a room diluted the staff already serving a better one. That is the deadlock.");
        }

        // ---- wages, and the floor -------------------------------------------------

        [Test]
        public void WagesAreChargedAgainstCapacityRatherThanAgainstCustomersServed()
        {
            // The mistake this mechanic exists to make possible. Charged against served
            // customers instead, over-hiring would be free and the second economy would
            // be a slider that only goes up.
            BuildingDefinition tavern = TradeFixture.EarningRoom("tavern", demandPerHour: 10f, seatsAtLevelOne: 1f, spendPerCustomer: 10f);
            StaffDefinition potboy = TradeFixture.Employee("potboy", hireCost: 1d, servicePerHour: 100f);
            GameContent content = TradeFixture.Catalogue(
                new[] { tavern },
                new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 40f) },
                new[] { potboy });
            GameWorld world = TradeFixture.Guild(content, "tavern");
            world.GuildState.SetLevel("tavern", 1);
            TradeService trade = TradeFixture.Clock(world).Trade;

            double grossBefore = trade.GrossPerHour();
            double wagesBefore = trade.WagesPerHour();
            world.Staff.Add(new StaffMember("a", potboy));

            Assert.That(trade.GrossPerHour(), Is.EqualTo(grossBefore).Within(0.001d),
                "Every customer was already being served, so the hire earned nothing.");
            Assert.That(trade.WagesPerHour(), Is.GreaterThan(wagesBefore),
                "Idle hands are still paid. That is the whole tension.");
        }

        [Test]
        public void TheNetIsFlooredAtZeroSoAnAbsenceCanNeverCostGold()
        {
            // §6.1. An idle game whose player returns after eight hours with less gold
            // than they left has punished them for closing it, and no amount of tycoon
            // realism is worth teaching that.
            BuildingDefinition tavern = TradeFixture.EarningRoom("tavern", demandPerHour: 10f, seatsAtLevelOne: 1f, spendPerCustomer: 1f);
            StaffDefinition steward = TradeFixture.Employee("steward", hireCost: 1d, servicePerHour: 100000f);
            GameContent content = TradeFixture.Catalogue(
                new[] { tavern },
                new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 1f) },
                new[] { steward });
            GameWorld world = TradeFixture.Guild(content, "tavern");
            world.Staff.Add(new StaffMember("a", steward));
            SimulationClock clock = TradeFixture.Clock(world);

            Assert.That(clock.Trade.WagesPerHour(), Is.GreaterThan(clock.Trade.GrossPerHour()),
                "This guild is meant to be ruinously over-staffed.");

            double before = world.Economy.Get(CurrencyType.Gold);
            clock.Advance(8d * 3600d);

            Assert.That(clock.Trade.NetPerHour(), Is.EqualTo(0d));
            Assert.That(world.Economy.Get(CurrencyType.Gold), Is.EqualTo(before),
                "Wages come out of the till, not out of the vault.");
        }

        [Test]
        public void LifetimeWagesNeverExceedWhatTheTillActuallyHeld()
        {
            BuildingDefinition tavern = TradeFixture.EarningRoom("tavern", demandPerHour: 10f, seatsAtLevelOne: 1f, spendPerCustomer: 1f);
            StaffDefinition steward = TradeFixture.Employee("steward", hireCost: 1d, servicePerHour: 100000f);
            GameContent content = TradeFixture.Catalogue(
                new[] { tavern },
                new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 1f) },
                new[] { steward });
            GameWorld world = TradeFixture.Guild(content, "tavern");
            world.Staff.Add(new StaffMember("a", steward));
            SimulationClock clock = TradeFixture.Clock(world);

            clock.Advance(3600d);

            Assert.That(clock.WagesPaid, Is.LessThanOrEqualTo(clock.GrossEarned),
                "Recording an unpayable remainder would report a bill the player never paid.");
        }

        // ---- one path for online and offline -------------------------------------

        [Test]
        public void AnHourAwayPaysExactlyWhatAnHourWatchedPays()
        {
            // The Day 4-5 decision paying out for the fourth time. There is no second
            // offline formula able to drift from what the game pays while you watch,
            // because there is no second formula.
            GameContent content = TradeFixture.Catalogue(
                new[] { TradeFixture.EarningRoom("tavern", 60f, 10f, 3f) },
                new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 60f) });

            GameWorld away = TradeFixture.Guild(content, "tavern");
            SimulationClock awayClock = TradeFixture.Clock(away);
            awayClock.Advance(3600d);

            GameWorld watched = TradeFixture.Guild(content, "tavern");
            SimulationClock watchedClock = TradeFixture.Clock(watched);
            for (int second = 0; second < 3600; second++)
            {
                watchedClock.Advance(1d);
            }

            Assert.That(
                watched.Economy.Get(CurrencyType.Gold),
                Is.EqualTo(away.Economy.Get(CurrencyType.Gold)).Within(0.5d));
        }

        // ---- the per-room seam ----------------------------------------------------

        [Test]
        public void APerRoomStatReadsZeroThroughTheGuildWideSeamRatherThanPlausible()
        {
            // The dangerous shape is not absence, which this project has learned four
            // times it cannot detect — it is a plausible wrong answer. Sixty-eight seats
            // summed across five rooms reads exactly like a real figure.
            BuildingDefinition tavern = TradeFixture.EarningRoom("tavern", 10f, 20f, 1f);
            BuildingDefinition inn = TradeFixture.EarningRoom("inn", 10f, 30f, 1f);
            GameContent content = TradeFixture.Catalogue(
                new[] { tavern, inn }, new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 10f) });
            GameWorld world = TradeFixture.Guild(content, "tavern", "inn");

            Assert.That(world.Stats.Get(GuildStat.ServiceSeats), Is.EqualTo(0f),
                "A per-room stat summed across the guild means nothing and must not read as fifty.");
            Assert.That(world.GuildState.EffectFor(tavern, GuildStat.ServiceSeats), Is.EqualTo(20f));
            Assert.That(world.GuildState.EffectFor(inn, GuildStat.ServiceSeats), Is.EqualTo(30f));
        }

        [Test]
        public void EveryStatIsEitherPerRoomOrGuildWideAndTheScopeSaysWhich()
        {
            // An invariant over the enum itself, so that appending a stat without
            // deciding which kind it is fails here rather than in a room panel.
            foreach (GuildStat stat in System.Enum.GetValues(typeof(GuildStat)))
            {
                Assert.That(
                    GuildStatScope.IsPerBuilding(stat) != GuildStatScope.IsGuildWide(stat),
                    Is.True,
                    $"{stat} is neither or both.");
            }
        }

        [Test]
        public void AGuildWideStatAgreesWhetherItIsReadPerRoomOrAggregated()
        {
            // The two combination rules have to match, because a stat that means one
            // thing guild-wide and another per-room is a stat nobody can balance.
            BuildingDefinition tavern = TradeFixture.EarningRoom("tavern", 10f, 1f, 1f, staffSlots: 7f);
            GameContent content = TradeFixture.Catalogue(
                new[] { tavern }, new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 10f) });
            GameWorld world = TradeFixture.Guild(content, "tavern");

            Assert.That(world.Stats.Get(GuildStat.StaffSlots), Is.EqualTo(7f));
            Assert.That(world.GuildState.EffectFor(tavern, GuildStat.StaffSlots), Is.EqualTo(7f));
        }

        // ---- the tap --------------------------------------------------------------

        [Test]
        public void TappingIsWorthNothingOnceTheStaffCoverEveryRoom()
        {
            // The property that makes the mechanic safe to sell a familiar against: it
            // decays on its own, so there is no late-game balance problem to tune away
            // and a familiar bought late is a familiar wasted.
            GameContent content = TradeFixture.Catalogue(
                new[] { TradeFixture.EarningRoom("tavern", 10f, 10f, 5f) },
                new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 1000f) });
            GameWorld world = TradeFixture.Guild(content, "tavern");
            SimulationClock clock = TradeFixture.Clock(world);

            clock.Advance(3600d);

            Assert.That(clock.Trade.UnservedWantPerHour(), Is.EqualTo(0d).Within(0.001d));
            Assert.That(clock.Takings.WaitingCustomers, Is.EqualTo(0d));
            Assert.That(clock.Takings.TryCollect(out double _, out BuildingDefinition _), Is.False);
        }

        [Test]
        public void TheQueueStopsAtItsCapSoAnAbsenceCannotBankTaps()
        {
            // Coming back to a wall of free gold would make the tap a reason to close the
            // game, which is the exact inversion of what it is for.
            GameContent content = TradeFixture.Catalogue(
                new[] { TradeFixture.EarningRoom("tavern", 100000f, 100000f, 1f) },
                new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 1f) },
                maxWaiting: 40f);
            GameWorld world = TradeFixture.Guild(content, "tavern");
            SimulationClock clock = TradeFixture.Clock(world);

            clock.Advance(8d * 3600d);

            Assert.That(clock.Takings.WaitingCustomers, Is.EqualTo(40d).Within(0.001d));
        }

        [Test]
        public void AServedCustomerComesFromTheBestPayingRoomStillGoingUnserved()
        {
            BuildingDefinition inn = TradeFixture.EarningRoom("inn", demandPerHour: 100f, seatsAtLevelOne: 10f, spendPerCustomer: 50f);
            BuildingDefinition tavern = TradeFixture.EarningRoom("tavern", demandPerHour: 100f, seatsAtLevelOne: 10f, spendPerCustomer: 1f);
            GameContent content = TradeFixture.Catalogue(
                new[] { tavern, inn }, new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 1f) });
            GameWorld world = TradeFixture.Guild(content, "tavern", "inn");
            SimulationClock clock = TradeFixture.Clock(world);

            clock.Advance(600d);
            bool served = clock.Takings.TryCollect(out double gold, out BuildingDefinition room);

            Assert.That(served, Is.True);
            Assert.That(room, Is.EqualTo(inn), "You serve the good table first, by hand as well as by staff.");
            Assert.That(gold, Is.EqualTo(50d).Within(0.001d));
        }

        [Test]
        public void ATapTakesOneCustomerOutOfTheQueueAndAnnouncesItself()
        {
            // Announced, unlike idle income, because a tap is something the player did
            // and wants to see land.
            GameContent content = TradeFixture.Catalogue(
                new[] { TradeFixture.EarningRoom("tavern", 100000f, 100000f, 7f) },
                new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 1f) });
            GameWorld world = TradeFixture.Guild(content, "tavern");
            SimulationClock clock = TradeFixture.Clock(world);
            clock.Advance(600d);

            int announced = 0;
            System.Action<TakingsCollected> handler = _ => announced++;
            EventBus.Subscribe(handler);

            double queued = clock.Takings.WaitingCustomers;
            clock.Takings.TryCollect(out double gold, out BuildingDefinition _);
            EventBus.Unsubscribe(handler);

            Assert.That(clock.Takings.WaitingCustomers, Is.EqualTo(queued - 1d).Within(0.001d));
            Assert.That(gold, Is.EqualTo(7d).Within(0.001d));
            Assert.That(announced, Is.EqualTo(1));
        }

        [Test]
        public void TakingsCountAsRoomIncomeAndNotAsSomethingElse()
        {
            // Keeping the thumb inside the room total is what stops the mechanic quietly
            // moving the 70/30 split the whole revision exists to create.
            GameContent content = TradeFixture.Catalogue(
                new[] { TradeFixture.EarningRoom("tavern", 100000f, 100000f, 7f) },
                new[] { TradeFixture.Tier("village", 0, baseServicePerHour: 1f) });
            GameWorld world = TradeFixture.Guild(content, "tavern");
            SimulationClock clock = TradeFixture.Clock(world);
            clock.Advance(600d);

            clock.Takings.TryCollect(out double gold, out BuildingDefinition _);

            Assert.That(clock.Takings.LifetimeTakings, Is.EqualTo(gold).Within(0.001d));
        }

        // ---- the shipping catalogue, which does not produce any of this yet ---------

        [Test]
        public void EveryTierCarriesABaseServiceOnceAnyRoomAsksForCustom()
        {
            // The cold-start trap, as an invariant rather than a value. With service
            // coming from staff alone an unstaffed room earns nothing, so a room upgrade
            // has no marginal value AND neither does the first employee — each needs the
            // other to exist first, and the model run that found this hired no staff
            // across a hundred and fifty hours.
            //
            // THIS TEST IS VACUOUS TODAY and says so rather than passing quietly: no
            // shipped room produces ServiceDemand until the five rooms are authored. Its
            // silence is documented, which is the difference between this and the canary
            // set Day 13 found watching nothing.
            bool anyRoomTrades = false;
            foreach (BuildingDefinition room in Shipped.Content.Buildings)
            {
                if (room != null && room.Produces(GuildStat.ServiceDemand))
                {
                    anyRoomTrades = true;
                    break;
                }
            }

            if (!anyRoomTrades)
            {
                Assert.Ignore(
                    "No shipped room produces Service Demand yet — the revenue engine exists and the five " +
                    "rooms do not. This guard becomes live on the day they are authored.");
            }

            foreach (GuildTierDefinition tier in Shipped.TiersInOrder())
            {
                Assert.That(tier.BaseServicePerHour, Is.GreaterThan(0f),
                    $"{tier.DisplayName} has no base service, so an unstaffed guild there can never start trading.");
            }
        }

        [Test]
        public void TheCatalogueCarriesUsableTradeConstants()
        {
            Assert.That(Shipped.Content.CustomerTurnsPerHour, Is.GreaterThan(0f));
            Assert.That(Shipped.Content.WageShareOfSpend, Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(1f));
            Assert.That(Shipped.Content.MaxWaitingCustomers, Is.GreaterThanOrEqualTo(1f));
        }
    }
}
