using System.Collections.Generic;
using IdleGuild.Adventurers;
using IdleGuild.App;
using IdleGuild.Core;
using IdleGuild.Guild;
using IdleGuild.Quests;
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
        /// </summary>
        public static void SetLevels(GameWorld world, int tavern = -1, int trainingRoom = -1, int inn = -1)
        {
            if (tavern >= 0)
            {
                world.GuildState.SetLevel("tavern", tavern);
            }

            if (trainingRoom >= 0)
            {
                world.GuildState.SetLevel("training_room", trainingRoom);
            }

            if (inn >= 0)
            {
                world.GuildState.SetLevel("inn", inn);
            }
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
