using System.Collections.Generic;
using IdleGuild.Core;
using UnityEngine;

namespace IdleGuild.Adventurers
{
    /// <summary>
    /// Everyone the guild employs, and the Inn's capacity rule that limits it.
    ///
    /// Capacity is read from <see cref="IGuildStats"/> on demand rather than stored,
    /// so upgrading the Inn takes effect the moment the upgrade lands. Note that a
    /// guild with no Inn built has capacity zero and can recruit nobody — that is the
    /// intended opening move, not a bug: starting gold buys the Inn, the Inn makes
    /// room, and only then does recruiting work.
    /// </summary>
    public sealed class AdventurerRoster
    {
        private readonly List<Adventurer> _members = new List<Adventurer>();

        /// <summary>Save/load reads this directly.</summary>
        public IReadOnlyList<Adventurer> Members => _members;

        public int Count => _members.Count;

        /// <summary>Beds available, from the Inn's Housing Capacity. Never negative.</summary>
        public int CapacityWith(IGuildStats guildStats)
        {
            float capacity = guildStats?.Get(GuildStat.HousingCapacity) ?? 0f;
            return Mathf.Max(0, Mathf.FloorToInt(capacity));
        }

        public bool HasRoomWith(IGuildStats guildStats)
        {
            return Count < CapacityWith(guildStats);
        }

        /// <summary>
        /// Take on a new member. Refuses duplicates by instance id so a replayed save or
        /// a double-tapped recruit button cannot put the same person on the roster twice.
        /// Capacity is the caller's check, not this one's — recruitment weighs it against
        /// cost and tier gates in one place.
        /// </summary>
        public bool Add(Adventurer adventurer)
        {
            if (adventurer == null || Find(adventurer.InstanceId) != null)
            {
                return false;
            }

            _members.Add(adventurer);
            return true;
        }

        public bool Remove(string instanceId)
        {
            for (int index = 0; index < _members.Count; index++)
            {
                if (_members[index].InstanceId == instanceId)
                {
                    _members.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        /// <summary>The member with this instance id, or null.</summary>
        public Adventurer Find(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return null;
            }

            foreach (Adventurer member in _members)
            {
                if (member.InstanceId == instanceId)
                {
                    return member;
                }
            }

            return null;
        }

        public int CountAvailable()
        {
            int available = 0;
            foreach (Adventurer member in _members)
            {
                if (member.IsAvailable)
                {
                    available++;
                }
            }

            return available;
        }

        /// <summary>
        /// Fill <paramref name="buffer"/> with up to <paramref name="maximum"/> idle
        /// members, clearing it first. Takes a buffer instead of returning a new list
        /// because the simulation calls this on every re-dispatch, and an idle game
        /// spends most of its life in that loop.
        /// </summary>
        public void CollectAvailable(List<Adventurer> buffer, int maximum)
        {
            if (buffer == null)
            {
                return;
            }

            buffer.Clear();
            if (maximum <= 0)
            {
                return;
            }

            foreach (Adventurer member in _members)
            {
                if (!member.IsAvailable)
                {
                    continue;
                }

                buffer.Add(member);
                if (buffer.Count >= maximum)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Seconds until the next resting member is available, or
        /// <see cref="double.PositiveInfinity"/> when nobody is resting. The simulation
        /// uses this to jump straight to the next thing that happens instead of stepping
        /// through empty time.
        /// </summary>
        public double NextRestCompletionSeconds()
        {
            double soonest = double.PositiveInfinity;
            foreach (Adventurer member in _members)
            {
                if (member.Activity != AdventurerActivity.Resting)
                {
                    continue;
                }

                if (member.RestRemainingSeconds < soonest)
                {
                    soonest = member.RestRemainingSeconds;
                }
            }

            return soonest;
        }

        /// <summary>
        /// Advance every rest timer. Returns true if at least one member finished resting,
        /// which tells the simulation to look for repeating assignments to restart.
        /// </summary>
        public bool AdvanceRest(double seconds)
        {
            if (seconds <= 0d)
            {
                return false;
            }

            bool anyBecameAvailable = false;
            foreach (Adventurer member in _members)
            {
                anyBecameAvailable |= member.AdvanceRest(seconds);
            }

            return anyBecameAvailable;
        }

        /// <summary>Drop everyone. For save loading, which rebuilds the roster from scratch.</summary>
        public void Clear()
        {
            _members.Clear();
        }
    }
}
