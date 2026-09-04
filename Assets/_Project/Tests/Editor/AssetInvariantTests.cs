using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.Core;
using IdleGuild.Guild;
using IdleGuild.Quests;
using NUnit.Framework;
using UnityEngine;

namespace IdleGuild.Tests
{
    /// <summary>
    /// The properties the content has to hold whatever the numbers are.
    ///
    /// This is the <c>--checks</c> block of <c>Docs/tools/guild_model.py</c>, moved
    /// somewhere it cannot drift: the Python model is a *copy* of the balance values and
    /// its answers go quietly wrong when the assets move without it. These assertions
    /// read the assets themselves, so there is nothing to keep in step.
    ///
    /// Every test here asserts a **shape**, not a figure — no dead levels, gates that
    /// only tighten, a ladder that doubles, an opening that is solvent. Day 13 and Day 21
    /// will move every number in the game and none of these should so much as flicker. A
    /// failure here means a curve stopped doing its job, not that a value changed.
    /// </summary>
    public sealed class AssetInvariantTests
    {
        [Test]
        public void EveryDefinitionHasAUniqueNonEmptyId()
        {
            AssertIdsAreSound<BuildingDefinition>(Shipped.Content.Buildings, b => b.Id, "building");
            AssertIdsAreSound<GuildTierDefinition>(Shipped.Content.Tiers, t => t.Id, "guild tier");
            AssertIdsAreSound<AdventurerDefinition>(Shipped.Content.Adventurers, a => a.Id, "adventurer");
            AssertIdsAreSound<QuestDefinition>(Shipped.Content.Quests, q => q.Id, "quest");
        }

        /// <summary>
        /// An asset that exists but is not in the catalogue is invisible to the game and
        /// perfectly healthy in the Inspector, which is a combination worth a test of its
        /// own — it is the shape of mistake a content day is most likely to make.
        /// </summary>
        [Test]
        public void EverythingUnderDataIsListedInGameContent()
        {
            AssertCatalogued(Shipped.EverythingOnDisk<BuildingDefinition>(), Shipped.Content.Buildings, "building");
            AssertCatalogued(Shipped.EverythingOnDisk<GuildTierDefinition>(), Shipped.Content.Tiers, "guild tier");
            AssertCatalogued(Shipped.EverythingOnDisk<AdventurerDefinition>(), Shipped.Content.Adventurers, "adventurer");
            AssertCatalogued(Shipped.EverythingOnDisk<QuestDefinition>(), Shipped.Content.Quests, "quest");
        }

        /// <summary>
        /// A building whose effect has stopped improving before its last level is the same
        /// bug as a level nobody can afford, wearing a different hat: the player is still
        /// being offered a purchase that buys them nothing.
        /// </summary>
        [Test]
        public void NoBuildingEffectIsDeadAtMaxLevel()
        {
            foreach (BuildingDefinition building in Shipped.Content.Buildings)
            {
                Assert.That(building.MaxLevel, Is.GreaterThan(1), $"{building.Id} has no levels to climb.");

                foreach (BuildingEffect effect in building.Effects)
                {
                    if (IsDeliberatelyFlat(effect.Stat))
                    {
                        continue;
                    }

                    float lastStep = effect.ValuePerLevel.Evaluate(building.MaxLevel);
                    float previousStep = effect.ValuePerLevel.Evaluate(building.MaxLevel - 1);

                    Assert.That(lastStep, Is.GreaterThan(previousStep),
                        $"{building.Id}'s {effect.Stat} does not improve from level {building.MaxLevel - 1} " +
                        $"to {building.MaxLevel}. The last level is a purchase that buys nothing.");
                }
            }
        }

        /// <summary>
        /// The one stat a room is not meant to buy more of.
        ///
        /// Day 18 walked this invariant into its first honest exception. "No dead levels"
        /// was written on Days 8-9 and assumed, without ever saying so, that every effect
        /// is a thing the player upgrades — and <see cref="GuildStat.ServiceDemand"/> is
        /// not. Demand belongs to the tier: how many people want in is the settlement
        /// growing around the hall, and a room whose own level raised its demand would
        /// collapse two of §3.1's three levers into one and delete the rhythm the whole
        /// revision is built on. So its curve is flat by design and reads the same at
        /// level one and level fifty-seven.
        ///
        /// Named here rather than skipped by a special case at the call site, and named by
        /// stat rather than by room, for the same reason <see cref="GuildStatScope"/>
        /// names its three: a rule about which stats behave differently belongs in one
        /// place where somebody appending a stat will meet it.
        ///
        /// The exemption is narrow on purpose. It says a flat curve is allowed here, not
        /// that one is expected — <see cref="ADemandCurveIsFlatAndTheOtherTwoAreNot"/> is
        /// what asserts it actually is flat, so this cannot quietly become a hole for a
        /// curve somebody forgot to fill in.
        /// </summary>
        private static bool IsDeliberatelyFlat(GuildStat stat)
        {
            return stat == GuildStat.ServiceDemand;
        }

        [Test]
        public void NoArchetypeStopsGettingStrongerBeforeMaxLevel()
        {
            foreach (AdventurerDefinition archetype in Shipped.Content.Adventurers)
            {
                Assert.That(archetype.MaxLevel, Is.GreaterThan(1), $"{archetype.Id} cannot be trained at all.");

                Assert.That(archetype.BasePowerAt(archetype.MaxLevel),
                    Is.GreaterThan(archetype.BasePowerAt(archetype.MaxLevel - 1)),
                    $"{archetype.Id} gains no power from its final training level.");

                Assert.That(archetype.TrainingCostToReach(archetype.MaxLevel),
                    Is.GreaterThan(archetype.TrainingCostToReach(2)),
                    $"{archetype.Id}'s training cost does not rise with level.");
            }
        }

        [Test]
        public void EveryUpgradeCostRisesWithLevel()
        {
            foreach (BuildingDefinition building in Shipped.Content.Buildings)
            {
                for (int level = 2; level <= building.MaxLevel; level++)
                {
                    Assert.That(building.CostToReach(level), Is.GreaterThan(building.CostToReach(level - 1)),
                        $"{building.Id} costs no more at level {level} than at {level - 1}.");
                }
            }
        }

        /// <summary>
        /// The tier-gate rule from §02 of the Ledger, as a test rather than a convention:
        /// advancement spans several buildings so a player cannot tunnel into one.
        /// </summary>
        [Test]
        public void EveryTierGateSpansAtLeastTwoBuildings()
        {
            foreach (GuildTierDefinition tier in Shipped.TiersInOrder())
            {
                if (tier.IsFinalTier)
                {
                    continue;
                }

                HashSet<string> gated = new HashSet<string>();
                foreach (BuildingLevelRequirement requirement in tier.RequirementsToAdvance)
                {
                    if (requirement.Building != null)
                    {
                        gated.Add(requirement.Building.Id);
                    }
                }

                Assert.That(gated.Count, Is.GreaterThanOrEqualTo(2),
                    $"{tier.Id} is gated on {gated.Count} building(s). The rule exists so a player " +
                    "cannot tunnel into one building and skip the others.");
            }
        }

        [Test]
        public void TierGatesOnlyEverTighten()
        {
            List<GuildTierDefinition> tiers = Shipped.TiersInOrder();

            for (int index = 1; index < tiers.Count; index++)
            {
                GuildTierDefinition previous = tiers[index - 1];
                GuildTierDefinition current = tiers[index];

                if (current.IsFinalTier)
                {
                    continue;
                }

                foreach (BuildingLevelRequirement requirement in current.RequirementsToAdvance)
                {
                    if (requirement.Building == null)
                    {
                        continue;
                    }

                    int before = MinimumLevelFor(previous, requirement.Building.Id);
                    Assert.That(requirement.MinimumLevel, Is.GreaterThan(before),
                        $"{current.Id} asks for {requirement.Building.Id} {requirement.MinimumLevel}, which is not " +
                        $"more than {previous.Id} already asked for ({before}). A gate that does not tighten is not a gate.");
                }
            }
        }

        [Test]
        public void ReputationThresholdsOnlyEverRise()
        {
            List<GuildTierDefinition> tiers = Shipped.TiersInOrder();

            for (int index = 1; index < tiers.Count; index++)
            {
                if (tiers[index].IsFinalTier)
                {
                    continue;
                }

                Assert.That(tiers[index].ReputationToAdvance, Is.GreaterThan(tiers[index - 1].ReputationToAdvance),
                    $"{tiers[index].Id} asks for no more reputation than {tiers[index - 1].Id}.");
            }
        }

        [Test]
        public void QuestSlotsAndDifficultyOnlyEverRise()
        {
            List<GuildTierDefinition> tiers = Shipped.TiersInOrder();

            for (int index = 1; index < tiers.Count; index++)
            {
                Assert.That(tiers[index].QuestSlots, Is.GreaterThanOrEqualTo(tiers[index - 1].QuestSlots),
                    $"{tiers[index].Id} offers fewer quest slots than the tier below it.");

                Assert.That(tiers[index].MaxQuestTier, Is.GreaterThanOrEqualTo(tiers[index - 1].MaxQuestTier),
                    $"{tiers[index].Id} offers an easier hardest quest than the tier below it.");
            }
        }

        /// <summary>
        /// The deadlock Day 4–5 found in data rather than code, restated for the build the
        /// revision produced.
        ///
        /// Housing Capacity has a neutral base of zero, so for fifteen days a guild with no
        /// Inn had no beds, could recruit nobody, and could never earn anything. That was
        /// answered by starting gold covering a bed-granting building plus somebody to
        /// sleep in it — which was a hope rather than a solution, as Day 16's playtest
        /// proved by spending the gold on something else and finishing the game on the
        /// third purchase.
        ///
        /// Day 18 moved the beds into a Barracks the player cannot build until Town, and
        /// answered it properly: the settlement grants beds of its own, so the opening no
        /// longer depends on the player buying the right thing first. The assertion moves
        /// with it. What has to be true is not "starting gold covers a bunkhouse" but
        /// "somebody can be hired at all, from the first frame, whatever the player does" —
        /// which is §01's rule at the one moment it is cheapest to get wrong.
        /// </summary>
        [Test]
        public void TheOpeningIsSolvent()
        {
            GuildTierDefinition village = Shipped.TiersInOrder()[0];

            Assert.That(village.BaseHousingCapacity, Is.GreaterThanOrEqualTo(1),
                "The starting tier grants no beds of its own, and the Barracks is not available at Village, " +
                "so a new guild can recruit nobody — unwinnable rather than merely slow.");

            double cheapestRecruit = double.MaxValue;
            foreach (AdventurerDefinition archetype in Shipped.Content.Adventurers)
            {
                if (archetype.MinimumTierOrder == 0 && archetype.Rarity == Rarity.Common)
                {
                    cheapestRecruit = System.Math.Min(cheapestRecruit, archetype.RecruitCostGold);
                }
            }

            Assert.That(cheapestRecruit, Is.LessThan(double.MaxValue),
                "No Common archetype is available at the starting tier, so nobody can be hired first.");

            Assert.That(Shipped.Content.StartingGold, Is.GreaterThanOrEqualTo(cheapestRecruit),
                $"Starting gold ({Shipped.Content.StartingGold:N0}) does not cover the cheapest recruit " +
                $"({cheapestRecruit:N0}), and a bed with nobody in it earns nothing.");

            // And the other half, which is new: the guild has to be able to trade before it
            // has hired anybody, or the first room is a purchase with no return and the
            // cold-start trap §6C finding #2 describes is back. This is the assertion the
            // tier's base service exists for.
            Assert.That(village.BaseServicePerHour, Is.GreaterThan(0f),
                "Nobody serves the first customer. With service coming from staff alone an unstaffed room " +
                "earns nothing, so a room upgrade has no marginal value and neither does the first employee — " +
                "each needs the other to already exist.");

            BuildingDefinition cheapestEarner = null;
            foreach (BuildingDefinition building in Shipped.Content.Buildings)
            {
                if (building.MinimumTierOrder > 0 || !building.Produces(GuildStat.ServiceDemand))
                {
                    continue;
                }

                if (cheapestEarner == null || building.CostToReach(1) < cheapestEarner.CostToReach(1))
                {
                    cheapestEarner = building;
                }
            }

            Assert.That(cheapestEarner, Is.Not.Null,
                "No room available at the starting tier draws a crowd, so the tycoon half of the game does " +
                "not begin until the player advances a tier they can only advance by playing it.");

            Assert.That(Shipped.Content.StartingGold, Is.GreaterThanOrEqualTo(cheapestEarner.CostToReach(1)),
                $"Starting gold does not cover {cheapestEarner.Id} at level 1 " +
                $"({cheapestEarner.CostToReach(1):N0}), which is the guild's only way to earn.");
        }

        /// <summary>Content nobody can ever reach is content that should not have been written.</summary>
        [Test]
        public void EveryQuestBecomesAvailableAtSomeTier()
        {
            foreach (QuestDefinition quest in Shipped.Content.Quests)
            {
                bool reachable = false;
                foreach (GuildTierDefinition tier in Shipped.TiersInOrder())
                {
                    if (quest.MinimumTierOrder <= tier.Order && quest.QuestTier <= tier.MaxQuestTier)
                    {
                        reachable = true;
                        break;
                    }
                }

                Assert.That(reachable, Is.True,
                    $"Quest '{quest.Id}' is tier {quest.QuestTier} from tier order {quest.MinimumTierOrder}, and no " +
                    "guild tier ever offers both. It can never appear.");
            }
        }

        [Test]
        public void EveryArchetypeBecomesRecruitable()
        {
            BuildingDefinition tavern = Shipped.Building("tavern");
            int highestTierOrder = 0;
            foreach (GuildTierDefinition tier in Shipped.TiersInOrder())
            {
                highestTierOrder = Mathf.Max(highestTierOrder, tier.Order);
            }

            float bestRarity = 0f;
            foreach (BuildingEffect effect in tavern.Effects)
            {
                if (effect.Stat == GuildStat.RecruitableRarity)
                {
                    bestRarity = Mathf.Max(bestRarity, effect.ValuePerLevel.Evaluate(tavern.MaxLevel));
                }
            }

            foreach (AdventurerDefinition archetype in Shipped.Content.Adventurers)
            {
                Assert.That(archetype.MinimumTierOrder, Is.LessThanOrEqualTo(highestTierOrder),
                    $"'{archetype.Id}' unlocks at tier order {archetype.MinimumTierOrder}, beyond the end of the arc.");

                Assert.That(Mathf.FloorToInt(bestRarity), Is.GreaterThanOrEqualTo((int)archetype.Rarity),
                    $"'{archetype.Id}' is {archetype.Rarity}, and a fully levelled Tavern only ever attracts up to " +
                    $"{(Rarity)Mathf.FloorToInt(bestRarity)}. It can never be hired.");
            }
        }

        /// <summary>
        /// Rarity has to be a decision rather than a badge, which means each band must be
        /// a real multiple of the one below rather than a rounding error next to the
        /// Barracks' guild-wide bonus. Days 10–11 set that multiple at 2.00 by
        /// generating the ladder from a rule instead of picking five sets of numbers.
        /// </summary>
        [Test]
        [Category("BalanceCanary")]
        public void EachRarityBandDoublesThePowerOfTheOneBelow()
        {
            List<AdventurerDefinition> ladder = new List<AdventurerDefinition>(Shipped.Content.Adventurers);
            ladder.Sort((left, right) => left.Rarity.CompareTo(right.Rarity));

            for (int index = 1; index < ladder.Count; index++)
            {
                AdventurerDefinition lower = ladder[index - 1];
                AdventurerDefinition upper = ladder[index];

                Assert.That(upper.Rarity, Is.GreaterThan(lower.Rarity), "Two archetypes share a rarity band.");

                float ratio = upper.BasePowerAt(upper.MaxLevel) / lower.BasePowerAt(lower.MaxLevel);
                Assert.That(ratio, Is.EqualTo(2f).Within(1f).Percent,
                    $"Fully trained, {upper.Id} is {ratio:F2}x {lower.Id} rather than 2x. The ladder is generated " +
                    "from one rule; a band that drifts off it was probably edited by hand.");

                Assert.That(upper.RecruitCostGold, Is.GreaterThan(lower.RecruitCostGold * 3d),
                    $"{upper.Id} costs less than 3x {lower.Id} to hire. A band that doubles power for pocket " +
                    "change is not a decision.");
            }
        }

        /// <summary>
        /// The ladder as five figures rather than as a ratio, because the invariant above
        /// compares bands against each other and a slip that scaled all five the same way
        /// would sail past it. This project's most expensive failures have all been one
        /// wrong number in one shipped asset — Day 4–5 handed the Inn its own cost curve
        /// as its bed curve and nothing caught it until the YAML was read back by hand.
        ///
        /// Level 2 is the first trainable level, so its cost is the base times one step of
        /// growth. Expected to move on Day 21; updating it is part of that work.
        /// </summary>
        [Test]
        [Category("BalanceCanary")]
        public void TheTrainingLadderReadsAsWritten()
        {
            (string Id, double FirstLevel)[] expected =
            {
                ("militia_recruit", 26.8d),
                ("hedge_knight", 53.6d),
                ("wandering_ranger", 107.2d),
                ("arcane_battlemage", 214.4d),
                ("dragonsworn_champion", 428.8d),
            };

            foreach ((string id, double firstLevel) in expected)
            {
                Assert.That(Shipped.Adventurer(id).TrainingCostToReach(2), Is.EqualTo(firstLevel).Within(0.05d),
                    $"{id}'s first training level. The bases double per band with power — 20 / 40 / 80 / " +
                    "160 / 320 at 34% growth — so that a bed costs what it delivers whatever is sleeping in it.");
            }
        }

        /// <summary>
        /// A band that doubles power must not cost more than double the gold to realise
        /// it. Otherwise rarity is taxed twice — once at the Tavern gate that unlocks it
        /// and again on every training level for the rest of the run — and the whole
        /// ladder above Common becomes a trap the player pays to walk into.
        ///
        /// This is the defect Day 13 found, and it had survived three days of looking
        /// straight at it. The training bases tripled per band (20 / 60 / 180 / 540 /
        /// 1620) while power doubled, so a Legendary bed cost 81x a Common bed and
        /// returned 16x the power — 6,268 gold per point against 1,236. Days 8–9, 10–11
        /// and 12 each concluded that "higher rarities feel pointless" and each looked
        /// for the reason in the power numbers, the recruitment gates and the player
        /// policy in turn. It was in the price list the whole time.
        ///
        /// Deliberately an invariant rather than a BalanceCanary: it names no figure, and
        /// any future retune that keeps rarity honest will pass it untouched. The 10%
        /// slack is for the recruit-cost ladder, which climbs 5x per band but is around
        /// 1% of what a bed costs over its life. The failure this guards against is 50%
        /// per band, so the margin is wide on purpose.
        /// </summary>
        [Test]
        public void AHigherRarityBandNeverCostsMoreGoldPerPointOfPower()
        {
            List<AdventurerDefinition> ladder = new List<AdventurerDefinition>(Shipped.Content.Adventurers);
            ladder.Sort((left, right) => left.Rarity.CompareTo(right.Rarity));

            for (int index = 1; index < ladder.Count; index++)
            {
                AdventurerDefinition lower = ladder[index - 1];
                AdventurerDefinition upper = ladder[index];

                double cheaper = GoldPerPointOfPower(lower);
                double dearer = GoldPerPointOfPower(upper);

                Assert.That(dearer, Is.LessThanOrEqualTo(cheaper * 1.10d),
                    $"A bed holding {upper.Id} costs {dearer:N0} gold per point of power against " +
                    $"{cheaper:N0} for {lower.Id}. Beds are capped, so rarity is what a player buys when " +
                    "they cannot buy another body — charging a premium for it on top makes the band " +
                    "above strictly worse than the one below.");
            }
        }

        // ---- the rooms, authored on Day 18 --------------------------------------
        //
        // §7 of Docs/Tests.md, written on Day 16: "the seats curves, the spend curves and
        // whether a Provisioner is worth nine thousand gold have no coverage at all, and
        // will not get any by accident. The day the rooms are authored owes this suite its
        // canaries." This is that debt.

        /// <summary>
        /// A room that draws a crowd has to be able to seat it and charge it.
        ///
        /// Seats without spend earns nothing; spend without seats has nobody to charge;
        /// demand without either is a queue outside a door that never opens. Any one of
        /// the three alone is a room that looks authored and is not, and the failure is
        /// silent in every direction — the room simply reads zero, and this project has
        /// been caught by an absence six times.
        ///
        /// Deliberately an "all or none" rather than a list of which rooms earn, so that
        /// authoring a sixth department stays one new .asset and zero edits here.
        /// </summary>
        [Test]
        public void AnEarningRoomProducesAllThreeLeversOrNoneOfThem()
        {
            foreach (BuildingDefinition room in Shipped.Content.Buildings)
            {
                bool seats = room.Produces(GuildStat.ServiceSeats);
                bool spend = room.Produces(GuildStat.CustomerSpend);
                bool demand = room.Produces(GuildStat.ServiceDemand);

                if (!seats && !spend && !demand)
                {
                    continue;
                }

                Assert.That(seats && spend && demand, Is.True,
                    $"'{room.Id}' carries some of seats/spend/demand and not all three " +
                    $"(seats {seats}, spend {spend}, demand {demand}). A room missing one of them earns " +
                    "exactly zero and says nothing about why.");

                Assert.That(room.EffectAt(GuildStat.ServiceSeats, 1), Is.GreaterThan(0f),
                    $"'{room.Id}' opens with no seats, so building it changes nothing until it is upgraded.");
                Assert.That(room.EffectAt(GuildStat.CustomerSpend, 1), Is.GreaterThan(0f),
                    $"'{room.Id}' opens charging nothing.");
                Assert.That(room.EffectAt(GuildStat.ServiceDemand, 1), Is.GreaterThan(0f),
                    $"'{room.Id}' opens with nobody wanting in.");
            }
        }

        /// <summary>
        /// §3.1's three levers, as an assertion: demand comes from the tier, capacity from
        /// the room's level, throughput from staff, and none of them may overlap.
        ///
        /// The half that is checkable in the assets is that a room's own level moves its
        /// seats and its spend and does <b>not</b> move its demand. Get that wrong and
        /// advancing a tier stops being the moment everything you own becomes insufficient
        /// — which is the rhythm the whole revision is built on, and it would fail as a
        /// vague sense that the game was flat rather than as anything anybody could find.
        /// </summary>
        [Test]
        public void ADemandCurveIsFlatAndTheOtherTwoAreNot()
        {
            foreach (BuildingDefinition room in Shipped.Content.Buildings)
            {
                if (!room.Produces(GuildStat.ServiceDemand))
                {
                    continue;
                }

                Assert.That(room.EffectAt(GuildStat.ServiceDemand, room.MaxLevel),
                    Is.EqualTo(room.EffectAt(GuildStat.ServiceDemand, 1)).Within(0.0001f),
                    $"'{room.Id}' draws a bigger crowd as it levels. Demand belongs to the settlement, " +
                    "and a room that grows its own collapses two of the three levers into one.");

                Assert.That(room.EffectAt(GuildStat.ServiceSeats, room.MaxLevel),
                    Is.GreaterThan(room.EffectAt(GuildStat.ServiceSeats, 1)),
                    $"'{room.Id}' never gains a seat, so there is nothing to spend gold on.");

                Assert.That(room.EffectAt(GuildStat.CustomerSpend, room.MaxLevel),
                    Is.GreaterThan(room.EffectAt(GuildStat.CustomerSpend, 1)),
                    $"'{room.Id}' never charges more per head.");
            }
        }

        /// <summary>
        /// The settlement only ever grows, and the Village is its unit. Every demand figure
        /// in the game is a room's own number multiplied by this, so a market that went
        /// backwards would quietly shrink every room at once.
        /// </summary>
        [Test]
        public void TheMarketOnlyEverGrowsAndTheVillageIsItsUnit()
        {
            List<GuildTierDefinition> tiers = Shipped.TiersInOrder();

            Assert.That(tiers[0].MarketSize, Is.EqualTo(1f).Within(0.0001f),
                "The Village is the unit every room's authored demand is expressed in.");

            for (int index = 1; index < tiers.Count; index++)
            {
                Assert.That(tiers[index].MarketSize, Is.GreaterThan(tiers[index - 1].MarketSize),
                    $"{tiers[index].Id} draws no bigger a crowd than {tiers[index - 1].Id}, so advancing to it " +
                    "leaves every room exactly as sufficient as it already was.");
            }
        }

        /// <summary>
        /// The Tavern's seats at the four moments the design document describes them.
        ///
        /// §3 of Docs/World_View_Design.md rests its entire case on a table — four seats at
        /// Village against four hundred wanting in, sixty at the ceiling against ninety
        /// thousand — and until today no room produced a single one of those numbers, so
        /// the table was a claim about a spreadsheet. It is now a claim about the game, and
        /// this is what keeps it one: the view draws a seat per floored point of the stat,
        /// so these are literally how many chairs are in the room.
        ///
        /// A canary rather than an invariant. A balance pass is expected to move these, and
        /// the thing it must not do is move them without noticing that §3's table, the
        /// world view's seat count and the tuned model all said the same thing first.
        /// </summary>
        [Test]
        [Category("BalanceCanary")]
        public void TheTavernsSeatsReadAsTheWorldViewWasDesignedAgainst()
        {
            BuildingDefinition tavern = Shipped.Building("tavern");

            Assert.That(Mathf.FloorToInt(tavern.EffectAt(GuildStat.ServiceSeats, 1)), Is.EqualTo(4),
                "Four seats at the moment the Village opens.");
            Assert.That(Mathf.FloorToInt(tavern.EffectAt(GuildStat.ServiceSeats, 17)), Is.EqualTo(19),
                "Nineteen at the Town gate.");
            Assert.That(Mathf.FloorToInt(tavern.EffectAt(GuildStat.ServiceSeats, 36)), Is.EqualTo(38),
                "Thirty-eight at the City gate.");
            Assert.That(Mathf.FloorToInt(tavern.EffectAt(GuildStat.ServiceSeats, tavern.MaxLevel)), Is.EqualTo(59),
                "Fifty-nine at the ceiling.");

            Assert.That(tavern.EffectAt(GuildStat.ServiceDemand, 1), Is.EqualTo(400f).Within(0.01f),
                "Four hundred want in at Village, against a hundred and sixty the seats can take. Seats are " +
                "the binding constraint at every tier, which is why there is always a queue outside the door " +
                "and why the view has permanent content for free.");
        }

        /// <summary>
        /// The Front Desk's commission is authored nowhere, and that is deliberate — but
        /// for a reason that is one word different from the one recorded on Day 16.
        ///
        /// It was written down as "declared with no producer: the Front Desk is authored
        /// later", which reads as though it were waiting for an asset. Day 18 authored the
        /// Front Desk and found it is waiting for a <b>consumer</b> as well: nothing in the
        /// game reads <see cref="GuildStat.ContractCommission"/>, and nothing reads a
        /// tier's contract reward scale either. A contract still pays its full authored
        /// gold through Reward Yield, unbounded, and the same multiplier is applied to
        /// reputation — where the model pays a saturating cut of a tier-scaled reward and
        /// leaves reputation alone. Two different mechanisms, and the tuned "rooms are 65%
        /// of lifetime income" is a fact about the model's one.
        ///
        /// So this asserts the absence, with the reason attached, because a curve authored
        /// onto the desk before the mechanism exists would produce a stat nothing reads on
        /// a room that visibly earns nothing — the sixth costume of the same failure. When
        /// somebody writes the consumer, this test is what they delete.
        /// </summary>
        [Test]
        public void TheContractCommissionHasNoProducerBecauseItStillHasNoConsumer()
        {
            foreach (BuildingDefinition room in Shipped.Content.Buildings)
            {
                Assert.That(room.Produces(GuildStat.ContractCommission), Is.False,
                    $"'{room.Id}' produces a commission, and nothing in the game spends it. Wire " +
                    "QuestResolution to read it — and the tier's contract reward scale with it — before " +
                    "authoring the curve, or the desk earns exactly nothing while looking like it earns.");
            }

            foreach (GuildTierDefinition tier in Shipped.TiersInOrder())
            {
                Assert.That(tier.ContractRewardScale, Is.EqualTo(1f).Within(0.0001f),
                    $"{tier.Id} scales contract rewards, and nothing reads that either.");
            }
        }

        /// <summary>
        /// What one bed costs over the life of the guild, against what it delivers: the
        /// hire plus every training level, divided by the power the archetype reaches.
        /// </summary>
        private static double GoldPerPointOfPower(AdventurerDefinition archetype)
        {
            double lifetime = archetype.RecruitCostGold;
            for (int level = 2; level <= archetype.MaxLevel; level++)
            {
                lifetime += archetype.TrainingCostToReach(level);
            }

            return lifetime / archetype.BasePowerAt(archetype.MaxLevel);
        }

        private static int MinimumLevelFor(GuildTierDefinition tier, string buildingId)
        {
            foreach (BuildingLevelRequirement requirement in tier.RequirementsToAdvance)
            {
                if (requirement.Building != null && requirement.Building.Id == buildingId)
                {
                    return requirement.MinimumLevel;
                }
            }

            return 0;
        }

        private static void AssertIdsAreSound<T>(IReadOnlyList<T> catalogue, System.Func<T, string> idOf, string kind)
            where T : Object
        {
            HashSet<string> seen = new HashSet<string>();

            foreach (T entry in catalogue)
            {
                Assert.That(entry, Is.Not.Null, $"The {kind} catalogue has an empty slot.");

                string id = idOf(entry);
                Assert.That(string.IsNullOrWhiteSpace(id), Is.False,
                    $"The {kind} asset '{entry.name}' has no Id. Saves reference it by Id, so it cannot persist.");

                Assert.That(seen.Add(id), Is.True,
                    $"Two {kind} assets share the Id '{id}'. Lookups return whichever comes first and saves " +
                    "cannot tell them apart.");
            }
        }

        private static void AssertCatalogued<T>(List<T> onDisk, IReadOnlyList<T> listed, string kind) where T : Object
        {
            HashSet<T> catalogued = new HashSet<T>(listed);

            foreach (T asset in onDisk)
            {
                Assert.That(catalogued.Contains(asset), Is.True,
                    $"The {kind} asset '{asset.name}' exists under {Shipped.DataFolder} but GameContent does not " +
                    "list it, so the game cannot see it. Authoring an asset and forgetting the catalogue looks " +
                    "exactly like everything working.");
            }
        }
    }
}
