using System.Collections.Generic;
using IdleGuild.App;
using IdleGuild.Core;
using IdleGuild.Guild;
using IdleGuild.Staff;
using UnityEditor;
using UnityEngine;

namespace IdleGuild.Tests
{
    /// <summary>
    /// A guild built in memory, for testing the revenue engine's mechanism.
    ///
    /// <b>This deliberately breaks the suite's usual rule, and the reason is worth
    /// stating rather than leaving as an inconsistency.</b> §1 of Docs/Tests.md says the
    /// tests load the real <c>.asset</c> files, because every content failure this
    /// project has had was a wrong value in a shipped asset and a hand-built fixture
    /// would have been written from the same misreading. That argument is about
    /// asserting *content*. Nothing here asserts content: these fixtures exist to check
    /// that priority allocation serves the good table first, that the wage floor holds,
    /// and that a tap cannot invent a customer — mechanism, which is logic and which
    /// legitimately supplies its own inputs.
    ///
    /// The corollary matters more than the exception: <b>no room in the shipping
    /// catalogue produces a single one of these stats yet.</b> Day 16 built the engine;
    /// the five rooms are authored later in the revision. So the value-asserting half of
    /// this subsystem — the seats curves, the spend curves, whether a Provisioner is
    /// worth nine thousand gold — has no coverage at all today, and it will not get any
    /// by accident. It arrives on the day the rooms do, and that day owes this suite a
    /// canary or two.
    ///
    /// Assets are authored through <see cref="SerializedObject"/> rather than by adding
    /// public setters. A setter that exists only for tests is a setter the game can call.
    /// </summary>
    internal static class TradeFixture
    {
        /// <summary>A room that trades: demand, seats and spend, flat across levels unless asked otherwise.</summary>
        public static BuildingDefinition EarningRoom(
            string id,
            float demandPerHour,
            float seatsAtLevelOne,
            float spendPerCustomer,
            float seatsPerLevel = 0f,
            float staffSlots = 0f,
            int maxLevel = 10)
        {
            BuildingDefinition room = ScriptableObject.CreateInstance<BuildingDefinition>();
            SerializedObject serialized = new SerializedObject(room);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = id;
            serialized.FindProperty("_maxLevel").intValue = maxLevel;
            Curve(serialized.FindProperty("_costToReachLevel"), 10f, 0f, 0.1f);

            List<(GuildStat, float, float)> effects = new List<(GuildStat, float, float)>
            {
                (GuildStat.ServiceDemand, demandPerHour, 0f),
                (GuildStat.ServiceSeats, seatsAtLevelOne, seatsPerLevel),
                (GuildStat.CustomerSpend, spendPerCustomer, 0f)
            };

            if (staffSlots > 0f)
            {
                effects.Add((GuildStat.StaffSlots, staffSlots, 0f));
            }

            WriteEffects(serialized.FindProperty("_effects"), effects);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return room;
        }

        /// <summary>A room that earns nothing directly. The Barracks, in other words.</summary>
        public static BuildingDefinition SupportRoom(string id, GuildStat stat, float valueAtLevelOne, float perLevel = 0f)
        {
            BuildingDefinition room = ScriptableObject.CreateInstance<BuildingDefinition>();
            SerializedObject serialized = new SerializedObject(room);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = id;
            serialized.FindProperty("_maxLevel").intValue = 10;
            Curve(serialized.FindProperty("_costToReachLevel"), 10f, 0f, 0.1f);
            WriteEffects(
                serialized.FindProperty("_effects"),
                new List<(GuildStat, float, float)> { (stat, valueAtLevelOne, perLevel) });
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return room;
        }

        public static StaffDefinition Employee(string id, double hireCost, float servicePerHour, int minimumTierOrder = 0)
        {
            StaffDefinition employee = ScriptableObject.CreateInstance<StaffDefinition>();
            SerializedObject serialized = new SerializedObject(employee);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = id;
            serialized.FindProperty("_hireCostGold").doubleValue = hireCost;
            serialized.FindProperty("_servicePerHour").floatValue = servicePerHour;
            serialized.FindProperty("_minimumTierOrder").intValue = minimumTierOrder;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return employee;
        }

        public static GuildTierDefinition Tier(
            string id,
            int order,
            float marketSize = 1f,
            float baseServicePerHour = 0f,
            int baseHousingCapacity = 0)
        {
            GuildTierDefinition tier = ScriptableObject.CreateInstance<GuildTierDefinition>();
            SerializedObject serialized = new SerializedObject(tier);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = id;
            serialized.FindProperty("_order").intValue = order;
            serialized.FindProperty("_questSlots").intValue = 1;
            serialized.FindProperty("_maxQuestTier").intValue = 1;
            serialized.FindProperty("_marketSize").floatValue = marketSize;
            serialized.FindProperty("_contractRewardScale").floatValue = 1f;
            serialized.FindProperty("_baseServicePerHour").floatValue = baseServicePerHour;
            serialized.FindProperty("_baseHousingCapacity").intValue = baseHousingCapacity;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return tier;
        }

        /// <summary>
        /// A catalogue holding exactly these rooms, tiers and employees, and the three
        /// trade constants. Starting gold is generous because no test here is about
        /// affordability.
        /// </summary>
        public static GameContent Catalogue(
            IReadOnlyList<BuildingDefinition> rooms,
            IReadOnlyList<GuildTierDefinition> tiers,
            IReadOnlyList<StaffDefinition> employees = null,
            float turnsPerHour = 40f,
            float wageShare = 0.2f,
            float maxWaiting = 40f,
            double startingGold = 1000000d)
        {
            GameContent content = ScriptableObject.CreateInstance<GameContent>();
            SerializedObject serialized = new SerializedObject(content);
            WriteObjectArray(serialized.FindProperty("_buildings"), rooms);
            WriteObjectArray(serialized.FindProperty("_tiers"), tiers);
            WriteObjectArray(serialized.FindProperty("_staff"), employees);
            serialized.FindProperty("_adventurers").arraySize = 0;
            serialized.FindProperty("_quests").arraySize = 0;
            serialized.FindProperty("_startingGold").doubleValue = startingGold;
            serialized.FindProperty("_customerTurnsPerHour").floatValue = turnsPerHour;
            serialized.FindProperty("_wageShareOfSpend").floatValue = wageShare;
            serialized.FindProperty("_maxWaitingCustomers").floatValue = maxWaiting;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return content;
        }

        /// <summary>A world on this catalogue, holding its starting gold, with every room built to level 1.</summary>
        public static GameWorld Guild(GameContent content, params string[] roomsAtLevelOne)
        {
            GameWorld world = new GameWorld(content, new SystemRandomSource(20260816));
            world.ApplyStartingState();
            foreach (string id in roomsAtLevelOne)
            {
                world.GuildState.SetLevel(id, 1);
            }

            return world;
        }

        /// <summary>The clock, which owns the trade layer and the takings queue.</summary>
        public static SimulationClock Clock(GameWorld world)
        {
            return new SimulationClock(world, new QuestDispatchService(world));
        }

        private static void WriteEffects(SerializedProperty array, IReadOnlyList<(GuildStat stat, float baseValue, float perLevel)> effects)
        {
            array.arraySize = effects.Count;
            for (int index = 0; index < effects.Count; index++)
            {
                SerializedProperty entry = array.GetArrayElementAtIndex(index);
                // intValue rather than enumValueIndex: enumValueIndex is a position in
                // the declaration list, and the moment GuildStat gains a gap between its
                // values those two stop agreeing. The stored number is what matters.
                entry.FindPropertyRelative("Stat").intValue = (int)effects[index].stat;
                entry.FindPropertyRelative("Kind").intValue = (int)ModifierKind.Additive;
                Curve(entry.FindPropertyRelative("ValuePerLevel"), effects[index].baseValue, effects[index].perLevel, 0f);
            }
        }

        private static void Curve(SerializedProperty curve, float baseValue, float linearPerLevel, float growthPerLevel)
        {
            curve.FindPropertyRelative("BaseValue").floatValue = baseValue;
            curve.FindPropertyRelative("LinearPerLevel").floatValue = linearPerLevel;
            curve.FindPropertyRelative("GrowthPerLevel").floatValue = growthPerLevel;
        }

        private static void WriteObjectArray<T>(SerializedProperty array, IReadOnlyList<T> values) where T : Object
        {
            int count = values?.Count ?? 0;
            array.arraySize = count;
            for (int index = 0; index < count; index++)
            {
                array.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }
    }
}
