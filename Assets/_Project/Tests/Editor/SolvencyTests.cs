using IdleGuild.App;
using IdleGuild.App.Saves;
using IdleGuild.Core;
using IdleGuild.Core.Events;
using IdleGuild.Guild;
using NUnit.Framework;

namespace IdleGuild.Tests
{
    /// <summary>
    /// The rule §01 of the Ledger now carries: <b>no sequence of choices may leave the
    /// player unable to make progress.</b>
    ///
    /// It is here because a playtest walked into exactly that on the third purchase of a
    /// new guild — Tavern to 1, Tavern to 2, Inn to 1, which is 147.50 of 150 starting
    /// gold, leaving 2.50 against a 25-gold recruit in a build where gold comes only from
    /// contracts and a contract needs an adventurer. Income was zero and stayed zero.
    ///
    /// Two things about that are worth keeping in front of whoever reads this next. It is
    /// Day 4-5's opening deadlock returning: that one was "solved in data rather than in
    /// code" by granting starting gold, and <b>a data solution that depends on the player
    /// spending it correctly is a hope rather than a solution.</b> And the tap built the
    /// day before was provably inert in that build — no room produced demand, so unserved
    /// demand was zero, so the queue never filled — which is the fifth appearance of a
    /// failure whose only symptom is the absence of something, and the suite could not see
    /// it because every trade test builds its own rooms.
    ///
    /// So the guarantee is not a balance figure. It is a property, and it is asserted
    /// against the <b>shipped</b> catalogue rather than a fixture, because a fixture would
    /// have been built from the same assumptions that produced the dead end.
    /// </summary>
    public sealed class SolvencyTests
    {
        [SetUp]
        public void ClearBus()
        {
            EventBus.ClearAll();
        }

        private static SimulationClock ClockFor(GameWorld world)
        {
            return new SimulationClock(world, new QuestDispatchService(world));
        }

        // ---- the rule ------------------------------------------------------------

        [Test]
        public void AGuildThatHasSpentEveryCoinCanAlwaysEarnAnother()
        {
            // The whole principle in one assertion. However the treasury was emptied,
            // whatever the guild does or does not own, waiting produces gold.
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);

            world.Economy.TrySpend(CurrencyType.Gold, world.Economy.Get(CurrencyType.Gold));
            Assert.That(world.Economy.Get(CurrencyType.Gold), Is.EqualTo(0d), "Guard: the guild is meant to be destitute.");

            clock.Advance(clock.Stipend.CooldownSeconds + 1d);

            Assert.That(clock.Stipend.CanCollect, Is.True,
                "A destitute guild has no way back. That is the state this rule exists to make unreachable.");
            Assert.That(clock.Stipend.TryCollect(out double gold), Is.True);
            Assert.That(gold, Is.GreaterThan(0d));
            Assert.That(world.Economy.Get(CurrencyType.Gold), Is.EqualTo(gold));
        }

        [Test]
        public void TheExactPlaytestPathIsNoLongerADeadEnd()
        {
            // Tavern, Tavern, Inn — bought through the real service, at shipped prices,
            // from shipped starting gold. Then: can the player afford the cheapest
            // adventurer within a few minutes of tapping?
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);
            BuildingUpgradeService buildings = new BuildingUpgradeService(world);

            Assert.That(buildings.TryUpgrade(Shipped.Building("tavern")), Is.EqualTo(UpgradeOutcome.Upgraded));
            Assert.That(buildings.TryUpgrade(Shipped.Building("tavern")), Is.EqualTo(UpgradeOutcome.Upgraded));
            Assert.That(buildings.TryUpgrade(Shipped.Building("inn")), Is.EqualTo(UpgradeOutcome.Upgraded));

            double cheapestRecruit = double.PositiveInfinity;
            foreach (var archetype in Shipped.Content.Adventurers)
            {
                if (archetype != null && archetype.MinimumTierOrder <= world.GuildState.CurrentTier.Order)
                {
                    cheapestRecruit = System.Math.Min(cheapestRecruit, archetype.RecruitCostGold);
                }
            }

            Assert.That(world.Economy.Get(CurrencyType.Gold), Is.LessThan(cheapestRecruit),
                "Guard: this is meant to reproduce the stranded state, not step over it.");

            // Twenty minutes of collecting the moment each delivery lands. Long,
            // deliberately: see RecoveringFromNothingIsSlowAndThatIsADecision.
            for (int tick = 0; tick < 20 * 60; tick++)
            {
                clock.Advance(1d);
                while (clock.Stipend.TryCollect(out double _))
                {
                }
            }

            Assert.That(world.Economy.Get(CurrencyType.Gold), Is.GreaterThanOrEqualTo(cheapestRecruit),
                "Twenty minutes of collecting did not buy the cheapest adventurer in the game, so the " +
                "player is still stranded — just more slowly, which is worse than being told.");
        }

        [Test]
        [Category("BalanceCanary")]
        public void RecoveringFromNothingIsSlowAndThatIsADecision()
        {
            // A canary rather than an invariant, because the number it pins is a
            // deliberate trade rather than a property.
            //
            // The first sizing aimed at "about a minute" — 15 gold every 30 seconds —
            // and then the model said what that actually was: 1,800 gold an hour against
            // rooms earning 9.5 an hour at Village, which is 189x the entire economy the
            // stipend is meant to sit underneath. The tension is structural rather than a
            // tuning miss: while the mailbox refills continuously, RECOVERY SPEED IS A
            // SUSTAINED RATE, and 25 gold is two and a half hours of Village room income,
            // so anything that rescues you quickly dwarfs the tier it rescues you in.
            //
            // The choice taken was to keep the mailbox simple and unconditional and pay
            // for it in recovery time, rather than add a hardship line that stops accrual
            // above a threshold. So this is what a mistake now costs, measured rather than
            // asserted, and pinned so that changing it has to be deliberate.
            //
            // Worth knowing when reading this later: the cost is largely an artefact of
            // the build it was written in. Nothing earns gold today, so the mailbox is the
            // only income there is. Once the five rooms are authored the stranded player
            // also has room income and a working takings tap, and this figure stops
            // describing anything a player will meet.
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);
            world.Economy.TrySpend(CurrencyType.Gold, world.Economy.Get(CurrencyType.Gold));

            double cheapestRecruit = double.PositiveInfinity;
            foreach (var archetype in Shipped.Content.Adventurers)
            {
                if (archetype != null && archetype.MinimumTierOrder <= world.GuildState.CurrentTier.Order)
                {
                    cheapestRecruit = System.Math.Min(cheapestRecruit, archetype.RecruitCostGold);
                }
            }

            int seconds = 0;
            while (world.Economy.Get(CurrencyType.Gold) < cheapestRecruit && seconds < 3600)
            {
                clock.Advance(1d);
                seconds++;
                while (clock.Stipend.TryCollect(out double _))
                {
                }
            }

            Assert.That(seconds, Is.InRange(600, 900),
                $"Recovering the cheapest adventurer from an empty treasury took {seconds}s. The figure " +
                "this was pinned at is about twelve and a half minutes; if it has moved, the stipend or " +
                "the recruit price moved and somebody should decide whether they meant to.");
        }

        [Test]
        public void AnHourOfTheCrownsStipendIsWorthLessThanTheOpeningItBacksUp()
        {
            // The guard that would have caught the first sizing, and it is deliberately
            // pinned to Village because Village is the only tier where a reference for
            // "how much gold is a lot" exists in shipped data today.
            //
            // Starting gold is the authored answer to "what does it take to get this
            // guild going". If one hour of mailbox exceeds the entire opening budget,
            // then the mailbox IS the opening, whatever the design document says it is.
            // The first sizing paid 1,800 an hour against 150 of starting gold and this
            // test would have failed by a factor of twelve.
            //
            // The higher tiers are covered by TheStipendNeverGrowsFasterThanTheMarketItBacks
            // instead, because no reference for their scale exists until the rooms land.
            GuildTierDefinition village = Shipped.TiersInOrder()[0];
            double perHour = village.StipendGold * 3600d / Shipped.Content.StipendCooldownSeconds;

            Assert.That(perHour, Is.LessThan(Shipped.Content.StartingGold),
                $"An hour of the crown's stipend is {perHour:N0} g against {Shipped.Content.StartingGold:N0} g " +
                "of starting gold, so the opening budget the game hands you is worth less than standing " +
                "still for an hour. That is not a floor, it is the economy.");
        }

        // ---- the mailbox ---------------------------------------------------------

        [Test]
        public void DeliveriesStopAtTheCapSoAnAbsenceCannotBankAnEvening()
        {
            // Same rule and same reason as the takings queue. Offline earnings are
            // OfflineProgress's job; this must not double-dip.
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);

            clock.Advance(8d * 3600d);

            Assert.That(clock.Stipend.DeliveriesWaiting, Is.EqualTo(clock.Stipend.MaximumDeliveries));
        }

        [Test]
        public void CollectingTakesExactlyOneDeliveryAndAnnouncesItself()
        {
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);
            clock.Advance(8d * 3600d);

            int announced = 0;
            System.Action<StipendCollected> handler = _ => announced++;
            EventBus.Subscribe(handler);

            int before = clock.Stipend.DeliveriesWaiting;
            bool collected = clock.Stipend.TryCollect(out double gold);
            EventBus.Unsubscribe(handler);

            Assert.That(collected, Is.True);
            Assert.That(clock.Stipend.DeliveriesWaiting, Is.EqualTo(before - 1));
            Assert.That(gold, Is.EqualTo(Shipped.Tier("village").StipendGold));
            Assert.That(announced, Is.EqualTo(1));
        }

        [Test]
        public void AnEmptyMailboxPaysNothing()
        {
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);

            Assert.That(clock.Stipend.CanCollect, Is.False, "A brand-new guild starts on a fresh cooldown.");
            Assert.That(clock.Stipend.TryCollect(out double gold), Is.False);
            Assert.That(gold, Is.EqualTo(0d));
        }

        [Test]
        public void TheStipendIsNotCountedAsRoomIncome()
        {
            // Takings are deliberately inside the gross so the thumb cannot move the
            // 70/30 split. The stipend is not room trade, so folding it in would move
            // that ratio without anybody choosing to.
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);
            clock.Advance(8d * 3600d);
            clock.Stipend.TryCollect(out double gold);

            Assert.That(gold, Is.GreaterThan(0d));
            Assert.That(clock.GrossEarned, Is.EqualTo(0d), "The rooms earned nothing; only the crown paid.");
            Assert.That(clock.StipendEarned, Is.EqualTo(gold));
        }

        [Test]
        public void TheMailboxSurvivesASaveRoundTripAndAnOldSaveArrivesEmpty()
        {
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);
            clock.Advance(8d * 3600d);
            clock.Stipend.TryCollect(out double _);

            SaveGameData data = SaveCapture.Capture(world, clock, System.DateTime.UtcNow);

            GameWorld reloaded = Shipped.NewGuild();
            SimulationClock reloadedClock = ClockFor(reloaded);
            SaveRestore.Restore(reloaded, reloadedClock, data);

            Assert.That(reloadedClock.Stipend.DeliveriesWaiting, Is.EqualTo(clock.Stipend.DeliveriesWaiting));
            Assert.That(reloadedClock.Stipend.LifetimeStipend, Is.EqualTo(clock.Stipend.LifetimeStipend).Within(0.001d));

            // And the case every checked-in fixture is: written before the mailbox existed.
            data.Stipend = null;
            GameWorld older = Shipped.NewGuild();
            SimulationClock olderClock = ClockFor(older);
            SaveRestoreReport report = SaveRestore.Restore(older, olderClock, data);

            Assert.That(report.HasRepairs, Is.False, $"An older save is not damaged: {report}.");
            Assert.That(olderClock.Stipend.DeliveriesWaiting, Is.EqualTo(0));
            Assert.That(olderClock.Stipend.LifetimeStipend, Is.EqualTo(0d));
        }

        [Test]
        public void StartingOverEmptiesTheMailboxAndNotJustTheFile()
        {
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);
            clock.Advance(8d * 3600d);
            clock.Stipend.TryCollect(out double _);

            SaveRestore.Reset(world, clock);

            Assert.That(clock.Stipend.DeliveriesWaiting, Is.EqualTo(0));
            Assert.That(clock.Stipend.LifetimeStipend, Is.EqualTo(0d));
        }

        // ---- the containment invariant -------------------------------------------

        [Test]
        public void EveryTierPaysAStipendAtAll()
        {
            // A tier with no stipend authored is a tier where the rule above does not
            // hold, and it would fail silently — the mailbox simply never lights up.
            foreach (GuildTierDefinition tier in Shipped.TiersInOrder())
            {
                Assert.That(tier.StipendGold, Is.GreaterThan(0d),
                    $"{tier.DisplayName} pays no stipend, so a player who strands themselves there has no way back.");
            }
        }

        [Test]
        public void TheStipendNeverGrowsFasterThanTheMarketItBacks()
        {
            // The containment invariant, and the reason a SCALING stipend is safe to
            // author. A floor that outgrows the settlement stops being a floor and
            // becomes the economy — at two deliveries a minute, a stipend worth a minute
            // of Capital income would out-earn every room in the game combined. Requiring
            // it to grow no faster than market size means it necessarily decays in
            // relative terms across the arc, however large it gets in absolute terms.
            //
            // IGNORED UNTIL THE ROOMS LAND, and says so: market size is authored with the
            // five rooms, and until then every tier reads 1 and this would compare a
            // growing stipend against a flat market and fail for the wrong reason.
            System.Collections.Generic.List<GuildTierDefinition> tiers = Shipped.TiersInOrder();

            bool marketAuthored = false;
            foreach (GuildTierDefinition tier in tiers)
            {
                if (tier.MarketSize > 1f)
                {
                    marketAuthored = true;
                    break;
                }
            }

            if (!marketAuthored)
            {
                Assert.Ignore(
                    "No tier carries a market size yet — that is authored with the five rooms. " +
                    "This is the guard that keeps a scaling stipend from becoming an income stream, " +
                    "and it goes live on the day the rooms do.");
            }

            for (int index = 1; index < tiers.Count; index++)
            {
                double stipendGrowth = tiers[index].StipendGold / tiers[index - 1].StipendGold;
                double marketGrowth = tiers[index].MarketSize / tiers[index - 1].MarketSize;

                Assert.That(stipendGrowth, Is.LessThanOrEqualTo(marketGrowth * 1.001d),
                    $"The stipend grows {stipendGrowth:F2}x into {tiers[index].DisplayName} against a market " +
                    $"growing {marketGrowth:F2}x, so the crown is outpacing the settlement and the floor is " +
                    "turning into an income stream.");
            }
        }

        [Test]
        [Category("BalanceCanary")]
        public void TheStipendLadderReadsAsWritten()
        {
            // A canary rather than an invariant, deliberately, and it exists because the
            // decision to let the stipend SCALE was taken with a stated risk. A balance
            // pass moving these four numbers is doing its job; a balance pass moving them
            // by accident is the thing this catches. Day 13's lesson: a canary set that
            // does not watch a value is quieter than no canary set.
            Assert.That(Shipped.Tier("village").StipendGold, Is.EqualTo(1d));
            Assert.That(Shipped.Tier("town").StipendGold, Is.EqualTo(2d));
            Assert.That(Shipped.Tier("city").StipendGold, Is.EqualTo(4d));
            Assert.That(Shipped.Tier("capital").StipendGold, Is.EqualTo(8d));
            Assert.That(Shipped.Content.StipendCooldownSeconds, Is.EqualTo(30f));
            Assert.That(Shipped.Content.StipendMaximumCharges, Is.EqualTo(3));
        }
    }
}
