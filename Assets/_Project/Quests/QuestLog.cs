using System.Collections.Generic;
using IdleGuild.Core;
using UnityEngine;

namespace IdleGuild.Quests
{
    /// <summary>
    /// Every quest run currently in flight, and the slot limit on how many there can
    /// be.
    ///
    /// Slots are read from <see cref="IGuildStats"/> on demand. Today that number comes
    /// from the guild tier alone; when the Quest Board ships it contributes additively
    /// to the same stat and this class does not change.
    /// </summary>
    public sealed class QuestLog
    {
        private readonly List<ActiveQuest> _active = new List<ActiveQuest>();

        /// <summary>Save/load reads this directly.</summary>
        public IReadOnlyList<ActiveQuest> Active => _active;

        public int ActiveCount => _active.Count;

        /// <summary>Simultaneous runs allowed. Never negative.</summary>
        public int SlotsWith(IGuildStats guildStats)
        {
            float slots = guildStats?.Get(GuildStat.QuestSlots) ?? 0f;
            return Mathf.Max(0, Mathf.FloorToInt(slots));
        }

        public bool HasFreeSlotWith(IGuildStats guildStats)
        {
            return ActiveCount < SlotsWith(guildStats);
        }

        /// <summary>Begin tracking a run. Refuses duplicates by instance id.</summary>
        public bool Add(ActiveQuest quest)
        {
            if (quest == null || Find(quest.InstanceId) != null)
            {
                return false;
            }

            _active.Add(quest);
            return true;
        }

        public bool Remove(string instanceId)
        {
            for (int index = 0; index < _active.Count; index++)
            {
                if (_active[index].InstanceId == instanceId)
                {
                    _active.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        public ActiveQuest Find(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return null;
            }

            foreach (ActiveQuest quest in _active)
            {
                if (quest.InstanceId == instanceId)
                {
                    return quest;
                }
            }

            return null;
        }

        /// <summary>
        /// Seconds until the next run finishes, or <see cref="double.PositiveInfinity"/>
        /// when nothing is running. The simulation steps from event to event rather than
        /// in fixed slices, and this is half of what it needs to know.
        /// </summary>
        public double NextCompletionSeconds()
        {
            double soonest = double.PositiveInfinity;
            foreach (ActiveQuest quest in _active)
            {
                if (quest.RemainingSeconds < soonest)
                {
                    soonest = quest.RemainingSeconds;
                }
            }

            return soonest;
        }

        /// <summary>Advance every run by the same step.</summary>
        public void Advance(double seconds)
        {
            if (seconds <= 0d)
            {
                return;
            }

            foreach (ActiveQuest quest in _active)
            {
                quest.Advance(seconds);
            }
        }

        /// <summary>
        /// Fill <paramref name="buffer"/> with the runs whose timers have finished,
        /// clearing it first. They stay in the log until the caller removes them, so a
        /// caller that fails partway through resolution does not lose the reward.
        /// </summary>
        public void CollectCompleted(List<ActiveQuest> buffer)
        {
            if (buffer == null)
            {
                return;
            }

            buffer.Clear();
            foreach (ActiveQuest quest in _active)
            {
                if (quest.IsComplete)
                {
                    buffer.Add(quest);
                }
            }
        }

        /// <summary>Drop every run. For save loading, which rebuilds the log from scratch.</summary>
        public void Clear()
        {
            _active.Clear();
        }
    }
}
