using IdleGuild.Adventurers;
using IdleGuild.App;
using IdleGuild.Core;
using NUnit.Framework;

namespace IdleGuild.Tests
{
    /// <summary>
    /// The one-way ratchet, and the action Day 12 added to unwind it.
    ///
    /// Days 10–11 found this by modelling rather than by playing: the Inn tops out at
    /// sixteen beds, a Capital guild fields twelve, and nothing in the game could dismiss
    /// an adventurer. A bed, once filled, was filled for the rest of the run — so a
    /// player who spent their spare beds on Epics during City could never hire the
    /// Legendary that Capital exists to unlock, whatever gold they ended up with.
    ///
    /// These assert shape rather than numbers on purpose. Not one of them names a bed
    /// count, a recruit cost or a rarity threshold, so Day 13 and Day 21 can move every
    /// figure in the game without any of them flickering. What they pin is the property:
    /// a full guild can always make room, and a refusal never half-happens.
    /// </summary>
    public sealed class RosterRatchetTests
    {
        [Test]
        public void RetiringFreesTheBedItWasHolding()
        {
            GameWorld world = GuildWith("village", tavern: 1, inn: 1);
            RecruitmentService recruitment = new RecruitmentService(world);
            AdventurerDefinition recruit = Shipped.Adventurer("militia_recruit");

            FillEveryBed(recruitment, recruit);
            Adventurer first = world.Roster.Members[0];

            Assert.That(recruitment.Preview(recruit), Is.EqualTo(RecruitOutcome.HousingFull));

            Assert.That(recruitment.TryDismiss(first), Is.EqualTo(DismissOutcome.Dismissed));
            Assert.That(world.Roster.Find(first.InstanceId), Is.Null);
            Assert.That(recruitment.Preview(recruit), Is.EqualTo(RecruitOutcome.Recruited),
                "The bed is free again, which is the entire reason the action exists.");
        }

        /// <summary>
        /// The trap, played out end to end.
        ///
        /// This is the model's impatient player: every bed spent on the best archetype
        /// available at the time, and then a better one arrives. Before Day 12 the last
        /// assertion here was unreachable for the rest of the run. It names no bed count
        /// so that a Day 13 Inn of fourteen or eighteen leaves it just as true.
        /// </summary>
        [Test]
        public void AGuildThatSpentEveryBedOnEpicsCanStillFieldALegendary()
        {
            GameWorld world = GuildWith("capital", tavern: 32, inn: 30);
            RecruitmentService recruitment = new RecruitmentService(world);

            AdventurerDefinition battlemage = Shipped.Adventurer("arcane_battlemage");
            AdventurerDefinition champion = Shipped.Adventurer("dragonsworn_champion");

            FillEveryBed(recruitment, battlemage);

            Assert.That(recruitment.Preview(champion), Is.EqualTo(RecruitOutcome.HousingFull),
                "Every bed spent on Epics — the state a greedy run finishes in.");

            Assert.That(recruitment.TryDismiss(world.Roster.Members[0]), Is.EqualTo(DismissOutcome.Dismissed));

            Assert.That(recruitment.Preview(champion), Is.EqualTo(RecruitOutcome.Recruited),
                "An irreversible decision made on incomplete information is a trap wearing a " +
                "decision's clothes. This assertion is the day that stopped being true.");
        }

        [Test]
        public void SomebodyOutInTheFieldCannotBeRetired()
        {
            GameWorld world = GuildWith("village", tavern: 1, inn: 1);
            RecruitmentService recruitment = new RecruitmentService(world);
            QuestDispatchService dispatch = new QuestDispatchService(world);

            recruitment.TryRecruit(Shipped.Adventurer("militia_recruit"), out Adventurer member);
            Assert.That(
                dispatch.TryDispatchAvailableParty(Shipped.Quest("rat_infested_cellar"), true, out QuestAssignment _),
                Is.EqualTo(DispatchOutcome.Dispatched));

            Assert.That(member.Activity, Is.EqualTo(AdventurerActivity.OnQuest));
            Assert.That(recruitment.PreviewDismissal(member), Is.EqualTo(DismissOutcome.OnQuest));

            Assert.That(recruitment.TryDismiss(member), Is.EqualTo(DismissOutcome.OnQuest));
            Assert.That(world.Roster.Count, Is.EqualTo(1), "A refused dismissal must not half-happen.");
        }

        /// <summary>
        /// Resting between runs of a repeating order looks idle and is not free.
        ///
        /// Worth its own test because it is the state the roster screen reports as "Idle"
        /// and the one where a naive availability check would let a member be dismissed
        /// out from under an order — leaving <c>TryStartRun</c> failing silently for the
        /// rest of the run, with a standing order on screen that simply never goes out
        /// again. That is the failure shape the Ledger warns about: a destructive action
        /// that does not invalidate the live state it describes.
        /// </summary>
        [Test]
        public void RestingBetweenRunsIsNotTheSameAsBeingFree()
        {
            GameWorld world = GuildWith("village", tavern: 1, inn: 1);
            RecruitmentService recruitment = new RecruitmentService(world);
            QuestDispatchService dispatch = new QuestDispatchService(world);
            SimulationClock clock = new SimulationClock(world, dispatch);

            recruitment.TryRecruit(Shipped.Adventurer("militia_recruit"), out Adventurer member);
            dispatch.TryDispatchAvailableParty(Shipped.Quest("rat_infested_cellar"), true, out QuestAssignment _);

            clock.Advance(world.QuestLog.NextCompletionSeconds() + 0.01d);

            Assert.That(member.Activity, Is.EqualTo(AdventurerActivity.Resting),
                "The run has landed and the recovery has started.");
            Assert.That(world.IsAssigned(member.InstanceId), Is.True);
            Assert.That(recruitment.PreviewDismissal(member), Is.EqualTo(DismissOutcome.OnStandingOrder));
        }

        /// <summary>
        /// Somebody who is both out on a quest and committed to the order that sent them
        /// is told about the quest.
        ///
        /// Pinned because the order of the two checks is a judgement rather than an
        /// accident, and the same judgement <see cref="RecruitmentService.Preview"/>
        /// makes about tier before rarity: report the obstacle the player can act on
        /// first. Being in the field clears itself with time; nothing they do to the
        /// order helps until that run lands.
        /// </summary>
        [Test]
        public void TheNearerOfTheTwoRefusalsIsTheOneReported()
        {
            GameWorld world = GuildWith("village", tavern: 1, inn: 1);
            RecruitmentService recruitment = new RecruitmentService(world);
            QuestDispatchService dispatch = new QuestDispatchService(world);

            recruitment.TryRecruit(Shipped.Adventurer("militia_recruit"), out Adventurer member);
            dispatch.TryDispatchAvailableParty(Shipped.Quest("rat_infested_cellar"), true, out QuestAssignment _);

            Assert.That(member.Activity, Is.EqualTo(AdventurerActivity.OnQuest));
            Assert.That(world.IsAssigned(member.InstanceId), Is.True, "Both refusals genuinely apply here.");

            Assert.That(recruitment.PreviewDismissal(member), Is.EqualTo(DismissOutcome.OnQuest));
        }

        [Test]
        public void RetiringSomebodyTwiceIsRefusedRatherThanRepeated()
        {
            GameWorld world = GuildWith("village", tavern: 1, inn: 1);
            RecruitmentService recruitment = new RecruitmentService(world);

            recruitment.TryRecruit(Shipped.Adventurer("militia_recruit"), out Adventurer member);

            Assert.That(recruitment.TryDismiss(member), Is.EqualTo(DismissOutcome.Dismissed));
            Assert.That(recruitment.TryDismiss(member), Is.EqualTo(DismissOutcome.UnknownAdventurer),
                "The Adventurer object outlives its place on the roster. Asking twice is a " +
                "double-tapped button, not a second retirement.");
            Assert.That(world.Roster.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Nothing comes back. A rebate would make hiring and firing a free churn loop,
        /// and what the roster was short of was reversibility rather than a refund — the
        /// player who guessed wrong needs a way out, not a way to guess for nothing.
        /// </summary>
        [Test]
        public void RetiringRefundsNothing()
        {
            GameWorld world = GuildWith("village", tavern: 1, inn: 1);
            RecruitmentService recruitment = new RecruitmentService(world);

            recruitment.TryRecruit(Shipped.Adventurer("militia_recruit"), out Adventurer member);
            double goldAfterHiring = world.Economy.Get(CurrencyType.Gold);

            recruitment.TryDismiss(member);

            Assert.That(world.Economy.Get(CurrencyType.Gold), Is.EqualTo(goldAfterHiring));
        }

        private static GameWorld GuildWith(string tierId, int tavern, int inn)
        {
            GameWorld world = Shipped.NewGuild();
            world.Economy.Grant(CurrencyType.Gold, 10_000_000d);
            Shipped.MoveTo(world, tierId);
            Shipped.SetLevels(world, tavern: tavern, inn: inn);
            return world;
        }

        /// <summary>
        /// Hire this archetype until the Inn refuses. Written against the gate rather than
        /// against a bed count so that the tests above say nothing about how many beds
        /// there are, which is a Day 13 number.
        /// </summary>
        private static void FillEveryBed(RecruitmentService recruitment, AdventurerDefinition archetype)
        {
            int guard = 0;
            while (recruitment.Preview(archetype) == RecruitOutcome.Recruited)
            {
                Assert.That(++guard, Is.LessThan(200), "The Inn never ran out of beds, which cannot be right.");
                recruitment.TryRecruit(archetype, out Adventurer _);
            }

            Assert.That(recruitment.Preview(archetype), Is.EqualTo(RecruitOutcome.HousingFull),
                "The gate that stopped the hiring should be the Inn rather than gold or the Tavern.");
        }
    }
}
