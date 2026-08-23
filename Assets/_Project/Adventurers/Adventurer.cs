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
    ///
    /// The quest/rest cycle is tracked here rather than in the quest itself, because
    /// availability is a property of the person: the Inn shortens *their* recovery,
    /// and dispatch needs to ask "who is free" without walking the quest log.
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

        public AdventurerActivity Activity { get; private set; } = AdventurerActivity.Idle;

        /// <summary>Which quest run this member is on, or null when not questing.</summary>
        public string ActiveQuestInstanceId { get; private set; }

        /// <summary>Seconds of rest left. Zero unless <see cref="Activity"/> is Resting.</summary>
        public double RestRemainingSeconds { get; private set; }

        /// <summary>True when this member can be put on a quest right now.</summary>
        public bool IsAvailable => Activity == AdventurerActivity.Idle;

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

        /// <summary>Send this member out on a quest run.</summary>
        public void SendOnQuest(string questInstanceId)
        {
            Activity = AdventurerActivity.OnQuest;
            ActiveQuestInstanceId = questInstanceId;
            RestRemainingSeconds = 0d;
        }

        /// <summary>
        /// Bring this member home to rest. A rest of zero or less returns them straight
        /// to <see cref="AdventurerActivity.Idle"/>, so a very fast Inn cannot strand
        /// someone in a state that never ticks down.
        /// </summary>
        public void BeginRest(double seconds)
        {
            ActiveQuestInstanceId = null;

            if (seconds <= 0d || double.IsNaN(seconds))
            {
                Activity = AdventurerActivity.Idle;
                RestRemainingSeconds = 0d;
                return;
            }

            Activity = AdventurerActivity.Resting;
            RestRemainingSeconds = seconds;
        }

        /// <summary>
        /// Burn down the rest timer. Returns true on the step where this member becomes
        /// available again, which is the signal the simulation uses to re-dispatch a
        /// repeating assignment.
        /// </summary>
        public bool AdvanceRest(double seconds)
        {
            if (Activity != AdventurerActivity.Resting || seconds <= 0d)
            {
                return false;
            }

            RestRemainingSeconds -= seconds;
            if (RestRemainingSeconds > 0d)
            {
                return false;
            }

            RestRemainingSeconds = 0d;
            Activity = AdventurerActivity.Idle;
            return true;
        }

        /// <summary>
        /// Put this member back into a previously saved state. For save restoration only:
        /// it skips the transitions the other methods enforce, which is exactly what
        /// loading needs and exactly what gameplay must not do.
        /// </summary>
        public void RestoreState(AdventurerActivity activity, string activeQuestInstanceId, double restRemainingSeconds)
        {
            Activity = activity;
            ActiveQuestInstanceId = activity == AdventurerActivity.OnQuest ? activeQuestInstanceId : null;
            RestRemainingSeconds = activity == AdventurerActivity.Resting
                ? Math.Max(0d, restRemainingSeconds)
                : 0d;
        }
    }
}
