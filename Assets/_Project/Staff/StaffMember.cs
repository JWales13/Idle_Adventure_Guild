using System;

namespace IdleGuild.Staff
{
    /// <summary>
    /// One employee on the books.
    ///
    /// Thin on purpose, and the thinness is the design: an employee has no activity, no
    /// rest timer and no level. Adventurers have all three because an adventurer is
    /// somebody the player follows; a potboy is throughput with a name on it. The
    /// revision cut individual adventurer training precisely so that gold flows into
    /// rooms rather than into people, and giving staff a progression track would put
    /// that sink straight back with a different label on it.
    ///
    /// It is a class rather than a struct anyway, for the reason <c>Adventurer</c> is:
    /// the archetype is shared and the individual is not, two Potboys are two people
    /// with their own ids, and a save references the individual.
    /// </summary>
    public sealed class StaffMember
    {
        public StaffMember(string instanceId, StaffDefinition definition)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                throw new ArgumentException("An employee needs an instance id to be saved and found again.", nameof(instanceId));
            }

            InstanceId = instanceId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        /// <summary>Identifies this employee. Saves reference this, not the array position.</summary>
        public string InstanceId { get; }

        /// <summary>The archetype they were hired from.</summary>
        public StaffDefinition Definition { get; }

        /// <summary>Customers per hour they get through.</summary>
        public float ServicePerHour => Definition.ServicePerHour;
    }
}
