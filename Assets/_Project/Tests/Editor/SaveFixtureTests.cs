using System;
using System.IO;
using IdleGuild.Adventurers;
using IdleGuild.App;
using IdleGuild.App.Saves;
using IdleGuild.Core;
using NUnit.Framework;
using UnityEngine;

namespace IdleGuild.Tests
{
    /// <summary>
    /// Real save files, kept in the repo, loaded by today's build.
    ///
    /// <see cref="SaveRoundTripTests"/> writes with today's <see cref="SaveCapture"/> and
    /// reads with today's <see cref="SaveRestore"/>, which proves those two agree with
    /// each other and nothing else. Compatibility is a different question: can this build
    /// read a file some *earlier* build wrote? That needs a file it did not write, and the
    /// only way to have one is to keep it.
    ///
    /// So each fixture beside this file is a permanent artefact. Add one whenever the save
    /// format or the meaning of a value in it changes, and this suite grows a compatibility
    /// history instead of a mirror.
    ///
    /// One thing worth knowing when reading these: `SaveSchema.CurrentVersion` has never
    /// been bumped, because no field has ever changed shape. Days 10–11 changed what a
    /// *value* means — Max Level went from 10 to 25 — which needs no migration and is
    /// exactly the kind of change that slips through unnoticed. It is why the second
    /// fixture exists.
    /// </summary>
    public sealed class SaveFixtureTests
    {
        private const string FixtureFolder = "_Project/Tests/Editor/Fixtures";

        /// <summary>
        /// A genuine play session: 219 quests completed, a run mid-timer carrying its
        /// dispatch-time snapshot, two standing orders, and members in three different
        /// activities. Richer than anything worth constructing by hand, which is the
        /// argument for keeping real files rather than only synthetic ones.
        /// </summary>
        [Test]
        public void ARealPlaySessionStillLoadsCleanly()
        {
            GameWorld world = Load("save_real_session.json", out SaveRestoreReport report);

            Assert.That(report.HasRepairs, Is.False, $"The restore had to repair something: {report}.");

            Assert.That(world.GuildState.CurrentTier.Id, Is.EqualTo("village"));
            Assert.That(world.GuildState.GetLevel("tavern"), Is.EqualTo(5));
            Assert.That(world.GuildState.GetLevel("training_room"), Is.EqualTo(4));
            Assert.That(world.GuildState.GetLevel("inn"), Is.EqualTo(5));

            Assert.That(world.Economy.Get(CurrencyType.Gold), Is.EqualTo(24054.18d).Within(0.5d));
            Assert.That(world.Economy.Get(CurrencyType.Reputation), Is.EqualTo(1678.16d).Within(0.5d));

            Assert.That(world.Roster.Count, Is.EqualTo(3));
            Assert.That(world.QuestLog.ActiveCount, Is.EqualTo(1), "The run that was in flight was lost.");
            Assert.That(world.Assignments.Count, Is.EqualTo(2),
                "A standing order was lost, which is what makes a loaded guild stop earning.");

            int onQuest = 0;
            int resting = 0;
            foreach (Adventurer member in world.Roster.Members)
            {
                Assert.That(member.Definition.Id, Is.EqualTo("militia_recruit"));
                Assert.That(member.Level, Is.EqualTo(2));

                if (member.Activity == AdventurerActivity.OnQuest)
                {
                    onQuest++;
                }
                else if (member.Activity == AdventurerActivity.Resting)
                {
                    resting++;
                }
            }

            Assert.That(onQuest, Is.EqualTo(1), "The member who was out on a quest did not come back out on it.");
            Assert.That(resting, Is.EqualTo(2), "Rest timers did not survive the round trip.");
        }

        /// <summary>
        /// Days 10–11 raised every archetype's Max Level from 10 to 25. This fixture holds
        /// a roster sitting at the old ceiling, which is what any save written before that
        /// change could contain.
        ///
        /// Raising a ceiling is safe — <see cref="Adventurer"/>'s constructor clamps to the
        /// definition's maximum, so a bigger maximum changes nothing. Lowering one silently
        /// re-levels people, which is the failure this test is really standing guard over:
        /// if a future balance pass shortens a track, this goes red rather than quietly
        /// demoting somebody's fully-trained Champion.
        /// </summary>
        [Test]
        public void AdventurersAtTheOldCeilingKeepTheirLevelAndCanTrainAgain()
        {
            GameWorld world = Load("save_v1_adventurers_at_old_ceiling.json", out SaveRestoreReport report);

            Assert.That(report.HasRepairs, Is.False, $"The restore had to repair something: {report}.");
            Assert.That(world.Roster.Count, Is.EqualTo(3));

            TrainingService training = new TrainingService(world);

            foreach (Adventurer member in world.Roster.Members)
            {
                Assert.That(member.Level, Is.EqualTo(10),
                    "A raised ceiling must not re-level anybody, and a lowered one must not demote them silently.");

                Assert.That(member.Definition.MaxLevel, Is.GreaterThan(10),
                    "This fixture only means something while the ceiling is above the level it holds.");

                Assert.That(member.Definition.HasLevel(11), Is.True);
                Assert.That(training.CostOfNextLevel(member), Is.GreaterThan(0d),
                    "Zero reads as 'free' rather than 'not trainable' — the trap TrainingCostToReach warns about.");
                Assert.That(training.Preview(member), Is.Not.EqualTo(TrainingOutcome.MaxLevel),
                    "There are fifteen levels of track in front of them now; the Train button should be live.");
            }
        }

        /// <summary>
        /// The rule that keeps saves in the wild alive: a save is never trusted to match
        /// today's catalogue. A quest renamed in Week 2 or an archetype cut in Week 3
        /// leaves files pointing at nothing, and the difference between a game that
        /// survives that and one that does not is entirely in <see cref="SaveRestore"/>.
        ///
        /// This fixture points at a tier, a building, an archetype and a quest that no
        /// build has ever had. Every one of them should be dropped and counted, and the
        /// guild around them should be left standing.
        /// </summary>
        [Test]
        public void ASaveNamingContentThisBuildLacksIsRepairedRatherThanRefused()
        {
            GameWorld world = Load("save_v1_content_since_removed.json", out SaveRestoreReport report);

            Assert.That(report.TierFellBack, Is.True, "An unknown tier should fall back to the starting tier.");
            Assert.That(report.UnknownBuildings, Is.EqualTo(1));
            Assert.That(report.DroppedAdventurers, Is.EqualTo(1));
            Assert.That(report.DroppedQuestRuns, Is.EqualTo(1));
            Assert.That(report.DroppedAssignments, Is.EqualTo(1),
                "The order whose party lost a member should go; a trimmed order would sit there looking " +
                "active and never run again.");
            Assert.That(report.RepairedAdventurers, Is.EqualTo(1),
                "The member who was out on the dropped run has to be sent home, or nothing ever brings them back.");

            // What matters as much as the counts: the guild is still there.
            Assert.That(world.GuildState.CurrentTier.Id, Is.EqualTo(world.Content.StartingTier.Id));
            Assert.That(world.GuildState.GetLevel("tavern"), Is.EqualTo(5), "Building levels are not collateral damage.");
            Assert.That(world.Economy.Get(CurrencyType.Gold), Is.EqualTo(24054.18d).Within(0.5d));
            Assert.That(world.Roster.Count, Is.EqualTo(2), "Only the member with the missing archetype should go.");

            foreach (Adventurer member in world.Roster.Members)
            {
                Assert.That(member.Activity, Is.Not.EqualTo(AdventurerActivity.OnQuest),
                    "Nobody should still be out on a quest that no longer exists.");
            }

            Assert.That(report.HasRepairs, Is.True);
            Assert.That(report.ToString(), Is.Not.EqualTo("clean"),
                "The report is what the console and the logs show; it has to say something.");
        }

        [Test]
        public void EveryFixtureIsWhereTheTestsExpectIt()
        {
            foreach (string name in new[]
                     {
                         "save_real_session.json",
                         "save_v1_adventurers_at_old_ceiling.json",
                         "save_v1_content_since_removed.json"
                     })
            {
                Assert.That(File.Exists(PathTo(name)), Is.True,
                    $"Fixture '{name}' is missing. These files are the only record of what earlier builds wrote — " +
                    "once one is gone it cannot be recreated, only approximated.");
            }
        }

        private static string PathTo(string fixtureName)
        {
            return Path.Combine(Application.dataPath, FixtureFolder, fixtureName);
        }

        /// <summary>
        /// Push a fixture through the whole shipping load path — the version probe, the
        /// migration ladder, restore — rather than deserialising it directly, so the test
        /// exercises what a device does.
        /// </summary>
        private static GameWorld Load(string fixtureName, out SaveRestoreReport report)
        {
            string path = PathTo(fixtureName);
            Assert.That(File.Exists(path), Is.True, $"No fixture at {path}.");

            InMemorySaveStore store = new InMemorySaveStore();
            store.Write(GameSaveService.DefaultSaveKey, File.ReadAllText(path));

            GameWorld world = Shipped.NewGuild();
            GameSaveService service = new GameSaveService(
                world, new SimulationClock(world, new QuestDispatchService(world)), store);

            Assert.That(service.TryLoad(out DateTime _), Is.EqualTo(SaveLoadResult.Loaded),
                $"'{fixtureName}' could not be loaded at all. A save this build cannot read is a player's guild lost.");

            report = service.LastRestoreReport;
            return world;
        }
    }
}
