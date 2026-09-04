using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.App;
using IdleGuild.Core;
using IdleGuild.Guild;
using IdleGuild.Quests;
using IdleGuild.Staff;
using NUnit.Framework;
using UnityEditor;

namespace IdleGuild.Tests
{
    /// <summary>
    /// The shipping catalogue, loaded from disk.
    ///
    /// These tests run against the real <c>.asset</c> files rather than fixtures built
    /// in code, and that is the whole point of them. Every content failure this project
    /// has actually had was a wrong value in a shipped asset — the Inn that was handed
    /// the *cost* curve as its bed curve, so a level-1 Inn granted fifty beds instead of
    /// two — and a fixture would have been written from the same misreading that
    /// produced the asset. Reading what is on disk is the only version of the check that
    /// could ever have caught it.
    ///
    /// The consequence to keep in mind: these tests fail when the *data* changes, which
    /// is intended. See the BalanceCanary category for the handful that are expected to
    /// be updated deliberately.
    /// </summary>
    internal static class Shipped
    {
        public const string ContentAssetPath = "Assets/_Project/Data/GameContent.asset";
        public const string DataFolder = "Assets/_Project/Data";

        private static GameContent _content;

        public static GameContent Content
        {
            get
            {
                if (_content == null)
                {
                    _content = AssetDatabase.LoadAssetAtPath<GameContent>(ContentAssetPath);
                    Assert.That(_content, Is.Not.Null,
                        $"No GameContent at {ContentAssetPath}. If the catalogue moved, this constant moves with it.");
                }

                return _content;
            }
        }

        /// <summary>Tiers in progression order, which is Order rather than array position.</summary>
        public static List<GuildTierDefinition> TiersInOrder()
        {
            List<GuildTierDefinition> tiers = new List<GuildTierDefinition>();
            foreach (GuildTierDefinition tier in Content.Tiers)
            {
                if (tier != null)
                {
                    tiers.Add(tier);
                }
            }

            tiers.Sort((left, right) => left.Order.CompareTo(right.Order));
            return tiers;
        }

        public static BuildingDefinition Building(string id)
        {
            BuildingDefinition building = Content.FindBuilding(id);
            Assert.That(building, Is.Not.Null, $"No building with Id '{id}' in the catalogue.");
            return building;
        }

        public static GuildTierDefinition Tier(string id)
        {
            GuildTierDefinition tier = Content.FindTier(id);
            Assert.That(tier, Is.Not.Null, $"No guild tier with Id '{id}' in the catalogue.");
            return tier;
        }

        public static QuestDefinition Quest(string id)
        {
            QuestDefinition quest = Content.FindQuest(id);
            Assert.That(quest, Is.Not.Null, $"No quest with Id '{id}' in the catalogue.");
            return quest;
        }

        public static StaffDefinition Staff(string id)
        {
            StaffDefinition staff = Content.FindStaff(id);
            Assert.That(staff, Is.Not.Null, $"No staff kind with Id '{id}' in the catalogue.");
            return staff;
        }

        /// <summary>
        /// The staff ladder in the order a player climbs it, which is the tier each kind
        /// unlocks at rather than array position. Empty today: no staff assets are
        /// authored, and §6 of Docs/Day16_Staff_And_Revenue.md says why.
        /// </summary>
        public static StaffDefinition[] StaffInTierOrder()
        {
            List<StaffDefinition> ladder = new List<StaffDefinition>();
            foreach (StaffDefinition staff in Content.Staff)
            {
                if (staff != null)
                {
                    ladder.Add(staff);
                }
            }

            ladder.Sort((left, right) => left.MinimumTierOrder.CompareTo(right.MinimumTierOrder));
            return ladder.ToArray();
        }

        public static AdventurerDefinition Adventurer(string id)
        {
            AdventurerDefinition adventurer = Content.FindAdventurer(id);
            Assert.That(adventurer, Is.Not.Null, $"No adventurer with Id '{id}' in the catalogue.");
            return adventurer;
        }

        /// <summary>
        /// A new guild holding its starting balances. The random source is seeded so that
        /// anything which rolls is repeatable — a failing test should fail every time.
        /// </summary>
        public static GameWorld NewGuild()
        {
            GameWorld world = new GameWorld(Content, new SystemRandomSource(20261124));
            world.ApplyStartingState();
            return world;
        }

        /// <summary>
        /// Put the guild into an arbitrary shape without playing to it. Levels below zero
        /// are left alone, so a caller can set one building and ignore the rest.
        ///
        /// Day 18 replaced the Training Room with the Barracks and split the Inn in two,
        /// and every caller had to be re-read rather than renamed, because <c>inn:</c> had
        /// two meanings in this file: the hotel, and the beds. Beds are the Barracks now.
        /// Named arguments are what made that safe — a positional call would have silently
        /// pointed the old middle argument at a different room.
        /// </summary>
        public static void SetLevels(
            GameWorld world,
            int tavern = -1,
            int frontDesk = -1,
            int barracks = -1,
            int inn = -1,
            int provisioner = -1)
        {
            if (tavern >= 0)
            {
                world.GuildState.SetLevel("tavern", tavern);
            }

            if (frontDesk >= 0)
            {
                world.GuildState.SetLevel("front_desk", frontDesk);
            }

            if (barracks >= 0)
            {
                world.GuildState.SetLevel("barracks", barracks);
            }

            if (inn >= 0)
            {
                world.GuildState.SetLevel("inn", inn);
            }

            if (provisioner >= 0)
            {
                world.GuildState.SetLevel("provisioner", provisioner);
            }
        }

        /// <summary>
        /// Build exactly enough Barracks to sleep <paramref name="beds"/> adventurers.
        ///
        /// Beds come from two places since Day 18 — the tier grants a couple so that a
        /// Village guild with no Barracks can still recruit, which is Day 4-5's opening
        /// deadlock closed for the third time — so a test that wants a roster of a given
        /// size can no longer name a level and has to ask for the size it means. Which is
        /// also what makes those tests survive the Barracks being re-costed: §2 of
        /// Docs/Tests.md, assert the shape rather than the number, applied to the setup
        /// rather than to the assertion.
        /// </summary>
        public static void SetBeds(GameWorld world, int beds)
        {
            BuildingDefinition barracks = Building("barracks");
            for (int level = 0; level <= barracks.MaxLevel; level++)
            {
                world.GuildState.SetLevel("barracks", level);
                if (world.Roster.CapacityWith(world.Stats) >= beds)
                {
                    return;
                }
            }

            Assert.Fail($"A maxed Barracks sleeps fewer than {beds}, so no test can ask for that roster.");
        }

        public static void MoveTo(GameWorld world, string tierId)
        {
            world.GuildState.AdvanceTo(Tier(tierId));
        }

        /// <summary>Every asset of a type under Data/, whether or not GameContent lists it.</summary>
        public static List<T> EverythingOnDisk<T>() where T : UnityEngine.Object
        {
            List<T> found = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { DataFolder }))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    found.Add(asset);
                }
            }

            return found;
        }
    }
}
