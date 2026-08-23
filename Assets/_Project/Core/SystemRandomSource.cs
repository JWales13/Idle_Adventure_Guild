using System;

namespace IdleGuild.Core
{
    /// <summary>
    /// Default <see cref="IRandomSource"/>, backed by <see cref="Random"/>.
    ///
    /// Deliberately not UnityEngine.Random: that is global mutable state shared with
    /// every other system in the process, so a seeded playthrough would be perturbed
    /// by anything else that happened to draw a number. An instance owns its own
    /// stream.
    /// </summary>
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        /// <summary>Unseeded, for normal play.</summary>
        public SystemRandomSource()
            : this(Environment.TickCount)
        {
        }

        /// <summary>Seeded, for reproducible runs.</summary>
        public SystemRandomSource(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        /// <summary>The seed this stream started from. Worth logging with a bug report.</summary>
        public int Seed { get; }

        /// <inheritdoc />
        public float NextUnitFloat()
        {
            return (float)_random.NextDouble();
        }
    }
}
