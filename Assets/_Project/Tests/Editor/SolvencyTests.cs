using IdleGuild.Adventurers;
using IdleGuild.App;
using IdleGuild.App.Saves;
using IdleGuild.Core;
using IdleGuild.Core.Events;
using IdleGuild.Guild;
using IdleGuild.UI;
using IdleGuild.UI.Views;
using NUnit.Framework;
using UnityEngine.UIElements;

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

        /// <summary>
        /// What it costs to put one body on the roster right now. The reference every
        /// solvency figure in this file is measured against, because an adventurer is the
        /// cheapest thing that turns a stranded guild back into a working one.
        /// </summary>
        private static double CheapestRecruitAt(GameWorld world)
        {
            double cheapest = double.PositiveInfinity;
            foreach (AdventurerDefinition archetype in Shipped.Content.Adventurers)
            {
                if (archetype != null && archetype.MinimumTierOrder <= world.GuildState.CurrentTier.Order)
                {
                    cheapest = System.Math.Min(cheapest, archetype.RecruitCostGold);
                }
            }

            return cheapest;
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
        public void TheDay16DeadEndCannotEvenBeWalkedAnyMore()
        {
            // The original path was Tavern, Tavern, Inn — 147.50 of 150 starting gold in a
            // build where gold came only from contracts and a contract needed an
            // adventurer. Day 18 closed it twice over, and it is worth asserting both
            // because either one alone would be a coincidence rather than a property.
            //
            // First, the path is not walkable: the Inn is a Town room now, so the third
            // purchase in that sequence is refused before it can empty the treasury.
            // Second, and the part that actually matters, the two purchases that ARE
            // available are rooms that earn — so a guild that has spent everything is
            // trading rather than waiting on the post.
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);
            BuildingUpgradeService buildings = new BuildingUpgradeService(world);

            Assert.That(buildings.TryUpgrade(Shipped.Building("tavern")), Is.EqualTo(UpgradeOutcome.Upgraded));
            Assert.That(buildings.TryUpgrade(Shipped.Building("tavern")), Is.EqualTo(UpgradeOutcome.Upgraded));

            Assert.That(buildings.TryUpgrade(Shipped.Building("inn")), Is.EqualTo(UpgradeOutcome.TierLocked),
                "The third purchase of the Day 16 playtest is a Town room, and a Village guild is told so " +
                "rather than being allowed to spend its last coin on it.");

            TradeService trade = new TradeService(world);
            Assert.That(trade.GrossPerHour(), Is.GreaterThan(0d),
                "Two Tavern levels and the guild still earns nothing an hour. The guildmaster works the bar " +
                "unaided — that is what the tier's base service is for — and without it the first room is a " +
                "purchase with no return.");

            Assert.That(trade.UnservedWantPerHour(), Is.GreaterThan(0d),
                "Nobody is being turned away, so there is nothing for a thumb to do. Seats are meant to bind " +
                "at every tier: that is the queue outside the door, and it is what makes tapping worth 87% " +
                "of early income.");

            // And the till fills without the player doing anything at all.
            double before = world.Economy.Get(CurrencyType.Gold);
            clock.Advance(600d);
            Assert.That(world.Economy.Get(CurrencyType.Gold), Is.GreaterThan(before),
                "Ten minutes of a built Tavern paid nothing.");
        }

        [Test]
        [Category("BalanceCanary")]
        public void AStrandedGuildWithARoomRecoversInMinutesRatherThanInTwelveOfThem()
        {
            // §5 of Docs/Day16_Followup_Solvency.md, cashed in: "the cost is largely an
            // artefact of the build it was written in. Nothing earns gold today, so the
            // mailbox is the only income there is. Once the five rooms are authored, a
            // stranded player also has room income and a working takings tap, and twelve
            // minutes stops describing anything a player will meet. If it still does on
            // the room day, the hardship line is the thing to reach for."
            //
            // It does not. This is the same measurement as the canary below, run against
            // the guild a player actually has rather than one that has built nothing, and
            // the answer is a couple of minutes — so the hardship line stays designed and
            // unbuilt, and the crown stays unconditional.
            //
            // The tap is what does it rather than the rooms: a Tavern this small earns
            // about ten gold an hour idle, and the queue outside it is worth roughly forty
            // times that to a thumb. That is §7's "tapping is 87% of early income" arriving
            // as a number in the shipped build for the first time.
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);
            BuildingUpgradeService buildings = new BuildingUpgradeService(world);

            buildings.TryUpgrade(Shipped.Building("tavern"));
            buildings.TryUpgrade(Shipped.Building("tavern"));
            world.Economy.TrySpend(CurrencyType.Gold, world.Economy.Get(CurrencyType.Gold));

            double cheapestRecruit = CheapestRecruitAt(world);

            int seconds = 0;
            while (world.Economy.Get(CurrencyType.Gold) < cheapestRecruit && seconds < 3600)
            {
                clock.Advance(1d);
                seconds++;
                while (clock.Takings.TryCollect(out double _, out BuildingDefinition _))
                {
                }
            }

            Assert.That(seconds, Is.LessThan(600),
                $"A guild with a Tavern took {seconds}s of tapping to afford the cheapest adventurer, which " +
                "is no better than the empty-handed case the mailbox was sized for. Either the tap is not " +
                "reaching the queue or the queue is not filling.");
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
            // Worth knowing when reading this later: this is the WORST case rather than
            // the likely one, and since Day 18 it is a guild that has built nothing at all
            // — the mailbox is the only income a hall with no rooms in it has. The case a
            // player actually meets is AStrandedGuildWithARoomRecoversInMinutesRatherThanInTwelveOfThem
            // above, which is a couple of minutes. Both are kept: this one is what the
            // stipend was sized against and is the floor under §01, and moving it should
            // still be a decision somebody made on purpose.
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);
            world.Economy.TrySpend(CurrencyType.Gold, world.Economy.Get(CurrencyType.Gold));

            double cheapestRecruit = CheapestRecruitAt(world);

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

        // ---- can the player actually reach it ------------------------------------

        [Test]
        public void TheTreasuryBarPutsTheStipendWhereThePlayerCanSeeIt()
        {
            // This is the test that was missing, and its absence cost a playtest.
            //
            // The stipend shipped working, saved, tested and documented — and only in the
            // debug console. The player-facing screen had no reference to it anywhere, so
            // the answer to "where is the mailbox" was that it did not exist. That is the
            // same shape as the takings tap shipping inert the day before, and as an
            // interface that built into the void for fifteen days: A GUARANTEE THE PLAYER
            // CANNOT REACH IS NOT A GUARANTEE.
            //
            // A UI Toolkit element builds its children in its constructor with no panel
            // attached, so this is assertable in EditMode even though the pixels are not.
            // It does not prove the control is legible — that is still the hand-check this
            // project has owed for three days — but it does prove it is there, which is
            // the half that failed.
            GameWorld world = Shipped.NewGuild();
            SimulationClock clock = ClockFor(world);
            GuildContext context = new GuildContext(
                world,
                new BuildingUpgradeService(world),
                new RecruitmentService(world),
                new TrainingService(world),
                new QuestDispatchService(world),
                new TierAdvancementService(world),
                clock.Stipend,
                (message, ok) => { });

            TreasuryBar bar = new TreasuryBar();
            bar.Refresh(context);

            Button mailbox = bar.Q<Button>(className: "stipend");

            Assert.That(mailbox, Is.Not.Null,
                "The treasury bar carries no stipend control, so the one action that always works " +
                "is invisible to the player.");
            Assert.That(mailbox.text, Is.Not.Empty);
            Assert.That(mailbox.enabledSelf, Is.False, "A new guild starts on a fresh cooldown.");

            clock.Advance(clock.Stipend.CooldownSeconds + 1d);
            bar.Refresh(context);

            Assert.That(mailbox.enabledSelf, Is.True, "A delivery has arrived and the control is still dead.");
            Assert.That(mailbox.ClassListContains("stipend--ready"), Is.True,
                "Nothing distinguishes a mailbox with post in it from an empty one.");
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
