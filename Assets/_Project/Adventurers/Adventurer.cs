using System;
using IdleGuild.Core;
using UnityEngine;

namespace IdleGuild.Adventurers
{
    /// <summary>
    /// One member of the roster: a definition plus the state that belongs to this
    /// individual rather than to the archetype.
    ///
    /// Derived numbers are methods taking <see cref="IGuildStats"/> rather than cached
    /// fields, so a Training Room or Inn upgrade is reflected immediately without this
    /// class subscribing to anything.
    /// </summary>
    public sealed class Adventurer
    {
        public Adventurer(string instanceId, AdventurerDefinition definition, int level = 1)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException("Roster members need a stable instance id.", nameof(instanceId));
            }

            InstanceId = instanceId;
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            Level = Math.Clamp(level, 1, definition.MaxLevel);
        }

        /// <summary>Identifies this specific roster member. Saves reference this, not the definition.</summary>
        public string InstanceId { get; }

        public AdventurerDefinition Definition { get; }

        public int Level { get; private set; }

        /// <summary>Raise this adventurer's level, clamped to the archetype's maximum.</summary>
        public void SetLevel(int level)
        {
            Level = Math.Clamp(level, 1, Definition.MaxLevel);
        }

        /// <summary>
        /// Effective Power: the archetype's own curve plus the Training Room's flat
        /// bonus. Quest duration and success chance both read this.
        /// </summary>
        public float PowerWith(IGuildStats guildStats)
        {
            float basePower = Definition.BasePowerAt(Level);
            float trainingBonus = guildStats?.Get(GuildStat.AdventurerPower) ?? 0f;
            return basePower + trainingBonus;
        }

        /// <summary>
        /// Seconds this adventurer rests after a quest, shortened by the Inn's recovery
        /// speed. Guarded against a zero or negative multiplier so a misconfigured Inn
        /// asset cannot produce an infinite or negative rest.
        /// </summary>
        public float RecoverySecondsWith(IGuildStats guildStats)
        {
            float speed = guildStats?.Get(GuildStat.RecoverySpeed) ?? 1f;
            if (speed <= 0f)
            {
                speed = 1f;
            }

            return Mathf.Max(0f, Definition.BaseRecoverySeconds / speed);
        }
    }
}
