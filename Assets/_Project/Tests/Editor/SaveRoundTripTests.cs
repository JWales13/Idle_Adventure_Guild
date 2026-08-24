using System;
using IdleGuild.Adventurers;
using IdleGuild.App;
using IdleGuild.App.Saves;
using IdleGuild.Core;
using IdleGuild.Quests;
using NUnit.Framework;

namespace IdleGuild.Tests
{
    /// <summary>
    /// Step 2 of the pass, plus the regression test for the only real bug this project
    /// has found so far.
    ///
    /// These run the whole save path — capture, JsonUtility, the version probe, the
    /// migration ladder, restore — against an in-memory store, so they exercise the same
    /// code a device does rather than a simplified version of it.
    /// </summary>
    public sealed class SaveRoundTripTests
    {
        [Test]
        public void AGuildComesBackFromDiskUnchangedAndUnrepaired()
        {
            InMemorySaveStore store = new InMemorySaveStore();

            GameWorld saved = ProgressedGuild(out SimulationClock savedClock);
            GameSaveService writer = new GameSaveService(saved, savedClock, store);
            Assert.That(writer.Save(), Is.True, "The store accepted nothing, so there is nothing to load.");

            GameWorld loaded = Shipped.NewGuild();
            SimulationClock loadedClock = ClockFor(loaded);
            GameSaveService reader = new GameSaveService(loaded, loadedClock, store);

            Assert.That(reader.TryLoad(out DateTime _), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(reader.LastRestoreReport.HasRepairs, Is.False,
                $"The restore had to repair something: {reader.LastRestoreReport}. Nothing in the catalogue " +
                "changed between writing and reading, so any repair at all is a bug rather than a migration.");

            Assert.That(loaded.GuildState.CurrentTier.Id, Is.EqualTo(saved.GuildState.CurrentTier.Id));
            Assert.That(loaded.Economy.Get(CurrencyType.Gold), Is.EqualTo(saved.Economy.Get(CurrencyType.Gold)).Within(1d),
                "Balances are doubles printed by JsonUtility at about seven significant figures, so a gold or " +
                "so of drift is expected and anything more is not.");

            foreach (var level in saved.GuildState.BuildingLevels)
            {
                Assert.That(loaded.GuildState.GetLevel(level.Key), Is.EqualTo(level.Value), $"{level.Key} came back at the wrong level.");
            }

            Assert.That(loaded.Roster.Count, Is.EqualTo(saved.Roster.Count));
            for (int index = 0; index < saved.Roster.Count; index++)
            {
                Adventurer before = saved.Roster.Members[index];
                Adventurer after = loaded.Roster.Find(before.InstanceId);

                Assert.That(after, Is.Not.Null, $"Roster member {before.InstanceId} did not come back.");
                Assert.That(after.Level, Is.EqualTo(before.Level));
                Assert.That(after.Definition.Id, Is.EqualTo(before.Definition.Id));
                Assert.That(after.Activity, Is.EqualTo(before.Activity));
            }

            Assert.That(loaded.QuestLog.ActiveCount, Is.EqualTo(saved.QuestLog.ActiveCount), "A run in flight was lost.");
            Assert.That(loaded.Assignments.Count, Is.EqualTo(saved.Assignments.Count),
                "A standing order was lost, which is what makes a loaded guild stop earning.");
        }

        /// <summary>
        /// Days 10–11 raised adventurer Max Level from 10 to 25. A save written before
        /// that change holds people at what was then the top of their track; they must
        /// come back at the same level, and find fifteen levels of track in front of them.
        /// </summary>
        [Test]
        public void AnAdventurerSavedAtTheOldMaximumKeepsTheirLevelAndCanTrainAgain()
        {
            InMemorySaveStore store = new InMemorySaveStore();

            GameWorld saved = Shipped.NewGuild();
            Shipped.SetLevels(saved, inn: 9);
            AdventurerDefinition archetype = Shipped.Adventurer("militia_recruit");
            saved.Roster.Add(new Adventurer("veteran-of-week-one", archetype, 10));

            SimulationClock savedClock = ClockFor(saved);
            Assert.That(new GameSaveService(saved, savedClock, store).Save(), Is.True);

            GameWorld loaded = Shipped.NewGuild();
            GameSaveService reader = new GameSaveService(loaded, ClockFor(loaded), store);
            Assert.That(reader.TryLoad(out DateTime _), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(reader.LastRestoreReport.HasRepairs, Is.False);

            Adventurer veteran = loaded.Roster.Find("veteran-of-week-one");
            Assert.That(veteran, Is.Not.Null);
            Assert.That(veteran.Level, Is.EqualTo(10), "A raised ceiling must not re-level anybody.");
            Assert.That(veteran.Definition.HasLevel(11), Is.True, "Max Level is 25 now, so level 11 exists.");
            Assert.That(veteran.Definition.TrainingCostToReach(11), Is.GreaterThan(0d),
                "Zero here would read as 'free' rather than 'not trainable', which is the trap TrainingCostToReach warns about.");
        }

        /// <summary>
        /// The Day 6 bug, kept as a test because the shape of it will recur: the debug
        /// console's delete removed the file and left the world running, so the next
        /// autosave wrote the same guild straight back and the deletion undid itself
        /// inside thirty seconds.
        ///
        /// **A destructive action that does not also invalidate the live state it
        /// describes will be undone by whatever writes that state next.**
        /// </summary>
        [Test]
        public void StartingOverEmptiesTheGuildAndNotJustTheFile()
        {
            GameWorld world = ProgressedGuild(out SimulationClock clock);

            Assert.That(world.Roster.Count, Is.GreaterThan(0), "The fixture is not actually progressed.");

            SaveRestore.Reset(world, clock);

            Assert.That(world.GuildState.CurrentTier.Id, Is.EqualTo(world.Content.StartingTier.Id));
            Assert.That(world.Roster.Count, Is.EqualTo(0));
            Assert.That(world.QuestLog.ActiveCount, Is.EqualTo(0));
            Assert.That(world.Assignments.Count, Is.EqualTo(0));
            Assert.That(clock.QuestsCompleted, Is.EqualTo(0L));
            Assert.That(clock.TotalSecondsSimulated, Is.EqualTo(0d).Within(0.001d));

            foreach (CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
            {
                Assert.That(world.Economy.Get(currency), Is.EqualTo(0d).Within(0.001d), $"{currency} survived the reset.");
            }

            foreach (var level in world.GuildState.BuildingLevels)
            {
                Assert.That(level.Value, Is.EqualTo(0), $"{level.Key} is still standing after starting over.");
            }
        }

        /// <summary>
        /// Loading mid-session must leave nothing of the previous guild behind. JsonUtility
        /// fills the fields it recognises and defaults the rest, so a restore that only
        /// *added* would quietly merge two guilds together.
        /// </summary>
        [Test]
        public void LoadingOverARunningSessionLeavesNothingOfTheOldGuild()
        {
            InMemorySaveStore store = new InMemorySaveStore();

            GameWorld fresh = Shipped.NewGuild();
            Assert.That(new GameSaveService(fresh, ClockFor(fresh), store).Save(), Is.True);

            GameWorld busy = ProgressedGuild(out SimulationClock busyClock);
            GameSaveService reader = new GameSaveService(busy, busyClock, store);

            Assert.That(reader.TryLoad(out DateTime _), Is.EqualTo(SaveLoadResult.Loaded));
            Assert.That(busy.GuildState.CurrentTier.Id, Is.EqualTo(fresh.Content.StartingTier.Id));
            Assert.That(busy.Roster.Count, Is.EqualTo(0));
            Assert.That(busy.Assignments.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// JsonUtility does not object to a shape it has never seen — it fills what it
        /// recognises and defaults the rest — so without the version probe an unrelated
        /// JSON file would deserialise happily into an empty guild and overwrite a real
        /// one on the next autosave.
        /// </summary>
        [Test]
        public void AnUnrelatedJsonFileIsRefusedRatherThanLoadedOver()
        {
            InMemorySaveStore store = new InMemorySaveStore();
            store.Write(GameSaveService.DefaultSaveKey, "{\"someOtherApp\":true,\"level\":9}");

            GameWorld world = ProgressedGuild(out SimulationClock clock);
            int rosterBefore = world.Roster.Count;

            GameSaveService service = new GameSaveService(world, clock, store);
            Assert.That(service.TryLoad(out DateTime _), Is.EqualTo(SaveLoadResult.Unreadable));
            Assert.That(world.Roster.Count, Is.EqualTo(rosterBefore), "The running guild was overwritten by a file that was not ours.");

            bool quarantined = false;
            foreach (string key in store.Keys)
            {
                quarantined |= key.StartsWith(GameSaveService.DefaultSaveKey + ".corrupt-", StringComparison.Ordinal);
            }

            Assert.That(quarantined, Is.True,
                "The unreadable payload should be kept aside rather than deleted — during Weeks 2 and 3 it is the " +
                "only evidence of why it failed.");
        }

        [Test]
        public void DeletingRemovesEveryRecoverableCopy()
        {
            InMemorySaveStore store = new InMemorySaveStore();
            GameWorld world = ProgressedGuild(out SimulationClock clock);
            GameSaveService service = new GameSaveService(world, clock, store);

            Assert.That(service.Save(), Is.True);
            Assert.That(service.Save(), Is.True, "Two writes, so the store is holding a copy behind the current one.");
            Assert.That(service.HasSave, Is.True);

            Assert.That(service.Delete(), Is.True);
            Assert.That(service.HasSave, Is.False);
            Assert.That(service.TryLoad(out DateTime _), Is.EqualTo(SaveLoadResult.NoSaveFound),
                "A wipe is the player deliberately starting over, so the copy behind it must go too.");
        }

        /// <summary>A guild with levels, people, a run in flight and a standing order.</summary>
        private static GameWorld ProgressedGuild(out SimulationClock clock)
        {
            GameWorld world = Shipped.NewGuild();
            world.Economy.Grant(CurrencyType.Gold, 100_000d);
            Shipped.SetLevels(world, tavern: 6, trainingRoom: 4, inn: 9);
            Shipped.MoveTo(world, "town");

            RecruitmentService recruitment = new RecruitmentService(world);
            AdventurerDefinition recruit = Shipped.Adventurer("militia_recruit");
            recruitment.TryRecruit(recruit, out Adventurer _);
            recruitment.TryRecruit(recruit, out Adventurer _);

            QuestDispatchService dispatch = new QuestDispatchService(world);
            clock = new SimulationClock(world, dispatch);

            QuestDefinition patrol = Shipped.Quest("bandit_patrol");
            Assert.That(dispatch.TryDispatchAvailableParty(patrol, true, out QuestAssignment _),
                Is.EqualTo(DispatchOutcome.Dispatched), "The fixture could not put a party in the field.");

            clock.Advance(10d);
            return world;
        }

        private static SimulationClock ClockFor(GameWorld world)
        {
            return new SimulationClock(world, new QuestDispatchService(world));
        }
    }
}
