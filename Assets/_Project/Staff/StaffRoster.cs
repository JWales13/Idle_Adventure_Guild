using System.Collections.Generic;
using IdleGuild.Core;
using UnityEngine;

namespace IdleGuild.Staff
{
    /// <summary>
    /// Everyone on the payroll, and the slot ceiling that limits them.
    ///
    /// One guild-wide pool rather than an assignment per room. §3.1 settled that: one
    /// number is far less fiddle than five, and what staff produce is speed, which the
    /// trade layer then spends on whichever room is worth the most. A player who had to
    /// post three potboys to the tavern and two to the inn would be doing arithmetic
    /// the game can do better.
    ///
    /// Capacity is read from <see cref="IGuildStats"/> on demand rather than stored, so
    /// a Tavern upgrade opens a slot the moment it lands — the same rule the Inn's beds
    /// followed before the revision moved them to the Barracks.
    ///
    /// Note what capacity being zero means here, because it is the opening position: a
    /// guild with no Tavern has no slots and can employ nobody. That is not a deadlock,
    /// for the same reason the Inn's zero beds were not one on Day 4-5 — the tier
    /// carries a base service so an unstaffed guild still trades, and the Tavern is the
    /// first thing the player buys. Solved in data, no branch in code.
    /// </summary>
    public sealed class StaffRoster
    {
        private readonly List<StaffMember> _employees = new List<StaffMember>();

        /// <summary>Save/load reads this directly.</summary>
        public IReadOnlyList<StaffMember> Employees => _employees;

        public int Count => _employees.Count;

        /// <summary>Slots available, from the Tavern's Staff Slots. Never negative.</summary>
        public int SlotsWith(IGuildStats guildStats)
        {
            float slots = guildStats?.Get(GuildStat.StaffSlots) ?? 0f;
            return Mathf.Max(0, Mathf.FloorToInt(slots));
        }

        public bool HasSlotWith(IGuildStats guildStats)
        {
            return Count < SlotsWith(guildStats);
        }

        /// <summary>
        /// Total customers per hour the payroll can get through. The guildmaster's own
        /// base service is the tier's and is added by the trade layer, not here — this
        /// class knows about employees and nothing else.
        /// </summary>
        public float ServicePerHour()
        {
            float total = 0f;
            foreach (StaffMember employee in _employees)
            {
                total += employee.ServicePerHour;
            }

            return total;
        }

        /// <summary>
        /// Take somebody on. Refuses duplicates by instance id so a replayed save or a
        /// double-tapped button cannot put the same employee on the books twice.
        /// Slot capacity is the caller's check, not this one's — the hiring service
        /// weighs it against cost and the tier gate in one place.
        /// </summary>
        public bool Add(StaffMember employee)
        {
            if (employee == null || Find(employee.InstanceId) != null)
            {
                return false;
            }

            _employees.Add(employee);
            return true;
        }

        public bool Remove(string instanceId)
        {
            for (int index = 0; index < _employees.Count; index++)
            {
                if (_employees[index].InstanceId == instanceId)
                {
                    _employees.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }

        /// <summary>The employee with this instance id, or null.</summary>
        public StaffMember Find(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return null;
            }

            foreach (StaffMember employee in _employees)
            {
                if (employee.InstanceId == instanceId)
                {
                    return employee;
                }
            }

            return null;
        }

        /// <summary>
        /// How many of this archetype are employed. The staff panel groups by archetype
        /// rather than listing ninety-nine identical rows, and this is what it counts.
        /// </summary>
        public int CountOf(StaffDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            int count = 0;
            foreach (StaffMember employee in _employees)
            {
                if (employee.Definition == definition)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// The least valuable employee on the books, or null when nobody is employed.
        ///
        /// This is what makes the ratchet reversible in practice rather than only in
        /// principle. A player who filled every slot with the cheapest help and now
        /// wants better needs one obvious thing to let go of, not a scroll through
        /// ninety-nine identical names. Ties break towards the earliest hired, so
        /// repeated use walks the payroll in a stable order instead of picking at random.
        /// </summary>
        public StaffMember LeastCapable()
        {
            StaffMember weakest = null;
            foreach (StaffMember employee in _employees)
            {
                if (weakest == null || employee.ServicePerHour < weakest.ServicePerHour)
                {
                    weakest = employee;
                }
            }

            return weakest;
        }

        /// <summary>Drop everyone. For save loading, which rebuilds the payroll from scratch.</summary>
        public void Clear()
        {
            _employees.Clear();
        }
    }
}
