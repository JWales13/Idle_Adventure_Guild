using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.App;
using IdleGuild.Core;
using IdleGuild.Quests;
using NUnit.Framework;

namespace IdleGuild.Tests
{
    /// <summary>
    /// Who goes on a quest, and what changing that answer is allowed to disturb.
    ///
    /// The second half of what Days 10–11 handed to Day 12. A standing order used to hold
    /// its party for the life of the order, so hiring a Dragonsworn Champion changed
    /// nothing until the player worked out unaided that they had to cancel and dispatch
    /// again — the best adventurer in the game sitting on the bench with nothing on
    /// screen admitting it.
    ///
    /// The property that makes re-forming safe is the same one that makes a quest's
    /// numbers immune to an upgrade bought halfway through it: <see cref="ActiveQuest"/>
    /// snapshots its own party at dispatch and the clock sends *that* snapshot home. The
    /// first test here is that snapshot, asserted directly.
    /// </summary>
    public sealed class PartyFormationTests
    {
        [Test]
        public void ReformingLeavesTheRunAlreadyInFlightExactlyAsItWas()
        {
            Fixture guild = new Fixture();

            Adventurer alwin = guild.Hire("militia_recruit");
            Adventurer bern = guild.Hire("militia_recruit");
            QuestAssignment order = guild.SendParty(Patrol, alwin, bern);

            ActiveQuest run = guild.World.QuestLog.Find(order.ActiveQuestInstanceId);
            double remainingBefore = run.RemainingSeconds;
            float riskBefore = run.FailureChance;
            double goldBefore = run.GoldOnSuccess;

            Adventurer cass = guild.Hire("hedge_knight");

            Assert.That(
                guild.Dispatch.TryReformParty(order.Id, Ids(alwin, cass)),
                Is.EqualTo(DispatchOutcome.Dispatched));

            Assert.That(run.PartyInstanceIds, Is.EquivalentTo(Ids(alwin, bern)),
                "The run holds its own party. Re-forming decides who goes next, not who is out now.");
            Assert.That(run.RemainingSeconds, Is.EqualTo(remainingBefore), "No timer moves under the player.");
            Assert.That(run.FailureChance, Is.EqualTo(riskBefore));
            Assert.That(run.GoldOnSuccess, Is.EqualTo(goldBefore));
            Assert.That(bern.Activity, Is.EqualTo(AdventurerActivity.OnQuest), "Nobody is recalled mid-dungeon.");

            Assert.That(order.MemberInstanceIds, Is.EquivalentTo(Ids(alwin, cass)));
        }

        [Test]
        public void TheNewPartyIsTheOneThatGoesOutNext()
        {
            Fixture guild = new Fixture();

            Adventurer alwin = guild.Hire("militia_recruit");
            Adventurer bern = guild.Hire("militia_recruit");
            QuestAssignment order = guild.SendParty(Patrol, alwin, bern);
            string firstRunId = order.ActiveQuestInstanceId;

            Adventurer cass = guild.Hire("hedge_knight");
            guild.Dispatch.TryReformParty(order.Id, Ids(alwin, cass));

            ActiveQuest next = guild.AdvanceToRunAfter(firstRunId);

            Assert.That(next, Is.Not.Null, "The order should start again once its new party is rested.");
            Assert.That(next.PartyInstanceIds, Is.EquivalentTo(Ids(alwin, cass)));
            Assert.That(guild.World.IsAssigned(bern.InstanceId), Is.False,
                "Dropped from the order, and therefore free to be sent elsewhere or retired.");
        }

        /// <summary>
        /// Re-forming does not require recalling first.
        ///
        /// Deliberate, and the reason is a timing one rather than a purity one: the window
        /// between runs of a repeating order is a few seconds of rest, and an edit a
        /// player can only make by catching that window is an edit they will never make.
        /// So the order's own members stay eligible for it whatever they are doing.
        /// </summary>
        [Test]
        public void AnOrdersOwnMembersStayEligibleWhileTheyAreOut()
        {
            Fixture guild = new Fixture();

            Adventurer alwin = guild.Hire("militia_recruit");
            Adventurer bern = guild.Hire("militia_recruit");
            QuestAssignment order = guild.SendParty(Patrol, alwin, bern);

            Assert.That(alwin.Activity, Is.EqualTo(AdventurerActivity.OnQuest));
            Assert.That(guild.Dispatch.IsFreeForParty(alwin.InstanceId, order), Is.True);
            Assert.That(guild.Dispatch.IsFreeForParty(alwin.InstanceId), Is.False,
                "Free for their own order, and free for nothing else.");

            Assert.That(guild.Dispatch.PreviewReform(order.Id, order.MemberInstanceIds),
                Is.EqualTo(DispatchOutcome.Dispatched));
        }

        [Test]
        public void SomebodyOnAnotherOrderCannotBeBorrowed()
        {
            Fixture guild = new Fixture();

            Adventurer alwin = guild.Hire("militia_recruit");
            Adventurer bern = guild.Hire("militia_recruit");
            Adventurer cass = guild.Hire("militia_recruit");
            Adventurer dorn = guild.Hire("militia_recruit");

            guild.SendParty(Patrol, alwin, bern);
            QuestAssignment second = guild.SendParty(Patrol, cass, dorn);

            Assert.That(guild.Dispatch.PreviewReform(second.Id, Ids(cass, alwin)),
                Is.EqualTo(DispatchOutcome.MemberUnavailable),
                "Two orders quietly sharing an adventurer would double-count their power in both.");
        }

        /// <summary>
        /// A party is exactly the size the quest asks for.
        ///
        /// The lower bound has always been enforced. The upper one is new, and it is new
        /// because it was previously unreachable: no caller could assemble an over-size
        /// party by hand, so nothing had to say no. The picker can, and every duration and
        /// failure figure in the game was derived against the number the quest names — a
        /// player sending four on a three-person job would be handed a speed multiplier
        /// nothing has been tuned for. Widening this is a design decision for a later day,
        /// not a side effect of building a screen.
        /// </summary>
        [Test]
        public void APartyIsExactlyTheSizeTheQuestAsksFor()
        {
            Fixture guild = new Fixture();

            Adventurer alwin = guild.Hire("militia_recruit");
            Adventurer bern = guild.Hire("militia_recruit");
            Adventurer cass = guild.Hire("militia_recruit");

            Assert.That(guild.Dispatch.Preview(Patrol, Ids(alwin)), Is.EqualTo(DispatchOutcome.PartyTooSmall));
            Assert.That(guild.Dispatch.Preview(Patrol, Ids(alwin, bern, cass)), Is.EqualTo(DispatchOutcome.PartyTooLarge));
            Assert.That(guild.Dispatch.Preview(Patrol, Ids(alwin, bern)), Is.EqualTo(DispatchOutcome.Dispatched));
        }

        [Test]
        public void NobodyCanBeInThePartyTwice()
        {
            Fixture guild = new Fixture();
            Adventurer alwin = guild.Hire("militia_recruit");

            Assert.That(guild.Dispatch.Preview(Patrol, Ids(alwin, alwin)), Is.EqualTo(DispatchOutcome.DuplicateMember),
                "A duplicated id would count one person's power twice and shorten the quest " +
                "for a party that does not exist.");
        }

        [Test]
        public void ReformingAnOrderThatIsNoLongerThereSaysSo()
        {
            Fixture guild = new Fixture();
            Adventurer alwin = guild.Hire("militia_recruit");
            Adventurer bern = guild.Hire("militia_recruit");

            Assert.That(guild.Dispatch.TryReformParty("no-such-order", Ids(alwin, bern)),
                Is.EqualTo(DispatchOutcome.UnknownOrder));
        }

        /// <summary>
        /// The suggested party is the strongest available rather than the first on the
        /// roster.
        ///
        /// Roster order used to decide this, which quietly sent the weaker adventurer
        /// whenever the player happened to hire them first — and it is also the choice
        /// <c>guild_model.py</c> makes on the player's behalf, so the two disagreeing is
        /// how a modelled arc stops describing the real one.
        /// </summary>
        [Test]
        public void TheSuggestedPartyIsTheStrongestAvailable()
        {
            Fixture guild = new Fixture();

            Adventurer weaker = guild.Hire("militia_recruit");
            Adventurer stronger = guild.Hire("hedge_knight");

            Assert.That(stronger.PowerWith(guild.World.Stats), Is.GreaterThan(weaker.PowerWith(guild.World.Stats)),
                "Premise: the higher band is stronger at level 1. Without that this test means nothing.");

            List<string> suggested = new List<string>();
            guild.Dispatch.SuggestParty(Shipped.Quest("rat_infested_cellar"), suggested);

            Assert.That(suggested, Is.EqualTo(new[] { stronger.InstanceId }));
        }

        /// <summary>
        /// The two halves of Day 12 as one route.
        ///
        /// Retiring refuses while somebody is committed, which on its own would be a wall
        /// rather than a rule — a guild whose every bed is on a standing order could never
        /// retire anyone. Re-forming is what releases them, and this walks the whole way
        /// from "no" to a freed bed to make sure the route actually connects.
        /// </summary>
        [Test]
        public void ReformingIsWhatReleasesSomebodyAnOrderIsHolding()
        {
            Fixture guild = new Fixture();

            Adventurer alwin = guild.Hire("militia_recruit");
            Adventurer bern = guild.Hire("militia_recruit");
            QuestAssignment order = guild.SendParty(Patrol, alwin, bern);
            string firstRunId = order.ActiveQuestInstanceId;

            Assert.That(guild.Recruitment.PreviewDismissal(bern), Is.EqualTo(DismissOutcome.OnQuest));

            Adventurer cass = guild.Hire("hedge_knight");
            Assert.That(guild.Dispatch.TryReformParty(order.Id, Ids(alwin, cass)),
                Is.EqualTo(DispatchOutcome.Dispatched));

            guild.AdvanceToRunAfter(firstRunId);

            Assert.That(bern.Activity, Is.EqualTo(AdventurerActivity.Idle));
            Assert.That(guild.Recruitment.TryDismiss(bern), Is.EqualTo(DismissOutcome.Dismissed),
                "Re-form to release, then retire. If this ever fails, a full guild has no way " +
                "to make room and the ratchet is back.");
        }

        private static QuestDefinition Patrol => Shipped.Quest("bandit_patrol");

        private static string[] Ids(params Adventurer[] members)
        {
            string[] ids = new string[members.Length];
            for (int index = 0; index < members.Length; index++)
            {
                ids[index] = members[index].InstanceId;
            }

            return ids;
        }

        /// <summary>
        /// A guild rich enough and far enough along to have choices: City, so there are
        /// quest slots for more than one order at a time, and an Inn with room to spare.
        /// </summary>
        private sealed class Fixture
        {
            internal Fixture()
            {
                World = Shipped.NewGuild();
                World.Economy.Grant(CurrencyType.Gold, 10_000_000d);
                Shipped.MoveTo(World, "city");
                Shipped.SetLevels(World, tavern: 9, inn: 21);

                Dispatch = new QuestDispatchService(World);
                Recruitment = new RecruitmentService(World);
                Clock = new SimulationClock(World, Dispatch);
            }

            internal GameWorld World { get; }

            internal QuestDispatchService Dispatch { get; }

            internal RecruitmentService Recruitment { get; }

            internal SimulationClock Clock { get; }

            internal Adventurer Hire(string archetypeId)
            {
                RecruitOutcome outcome = Recruitment.TryRecruit(Shipped.Adventurer(archetypeId), out Adventurer member);
                Assert.That(outcome, Is.EqualTo(RecruitOutcome.Recruited),
                    $"The fixture could not hire a {archetypeId}, so the test never got to its own subject.");
                return member;
            }

            internal QuestAssignment SendParty(QuestDefinition quest, params Adventurer[] party)
            {
                DispatchOutcome outcome = Dispatch.TryDispatch(quest, Ids(party), true, out QuestAssignment assignment);
                Assert.That(outcome, Is.EqualTo(DispatchOutcome.Dispatched),
                    $"The fixture could not send a party on {quest.DisplayName}.");
                return assignment;
            }

            /// <summary>
            /// Step from event to event until a run other than <paramref name="previousRunId"/>
            /// is in flight. Stepping rather than advancing one large slice so the test
            /// stops at the *next* run rather than at whichever one happens to be out after
            /// an arbitrary hour.
            /// </summary>
            internal ActiveQuest AdvanceToRunAfter(string previousRunId)
            {
                for (int step = 0; step < 64; step++)
                {
                    double next = Clock.NextEventSeconds();
                    if (double.IsInfinity(next))
                    {
                        return null;
                    }

                    Clock.Advance(next + 0.01d);

                    foreach (ActiveQuest candidate in World.QuestLog.Active)
                    {
                        if (candidate.InstanceId != previousRunId)
                        {
                            return candidate;
                        }
                    }
                }

                return null;
            }
        }
    }
}
