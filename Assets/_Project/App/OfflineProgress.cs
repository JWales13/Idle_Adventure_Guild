using System;
using IdleGuild.Core;

namespace IdleGuild.App
{
    /// <summary>What the guild did while the player was away.</summary>
    public readonly struct OfflineReport
    {
        public OfflineReport(
            double secondsAway,
            double secondsSimulated,
            double goldEarned,
            double reputationEarned,
            long questsCompleted)
        {
            SecondsAway = secondsAway;
            SecondsSimulated = secondsSimulated;
            GoldEarned = goldEarned;
            ReputationEarned = reputationEarned;
            QuestsCompleted = questsCompleted;
        }

        /// <summary>Real time since the app was last seen.</summary>
        public double SecondsAway { get; }

        /// <summary>How much of that was actually paid out, after the cap.</summary>
        public double SecondsSimulated { get; }

        /// <summary>Time beyond the cap, which earned nothing. Shown to the player as the reason to come back sooner.</summary>
        public double SecondsForfeited => Math.Max(0d, SecondsAway - SecondsSimulated);

        public double GoldEarned { get; }

        public double ReputationEarned { get; }

        public long QuestsCompleted { get; }

        /// <summary>True when there is something worth putting in a "while you were away" panel.</summary>
        public bool HasEarnings => GoldEarned > 0d || ReputationEarned > 0d;
    }

    /// <summary>
    /// Paying the player for time the app was closed.
    ///
    /// This deliberately owns no maths of its own. It works out how long the player was
    /// away, caps it, and hands that stretch to <see cref="SimulationClock.Advance"/> —
    /// the same call a single frame makes. Offline earnings are therefore exactly what
    /// the guild would have earned had the player watched, including quest failures and
    /// rest gaps, rather than a parallel rate that has to be balanced separately and
    /// inevitably disagrees.
    ///
    /// The cost is that a long absence walks through every quest that would have
    /// happened. At event-per-iteration that is a few hundred steps for eight hours,
    /// which is nothing, and the ceiling inside the clock protects against the
    /// pathological asset.
    ///
    /// The rewarded-ad "2x offline earnings" placement in Week 3 multiplies the earnings
    /// on the returned report; it does not re-run the simulation.
    /// </summary>
    public static class OfflineProgress
    {
        /// <summary>Below this, an absence is treated as a blink and reported as nothing happening.</summary>
        public const double MinimumReportableSeconds = 60d;

        /// <summary>
        /// Run the guild forward by the capped absence and report what changed. Safe to
        /// call with a zero or negative elapsed time, which does nothing.
        /// </summary>
        public static OfflineReport CatchUp(GameWorld world, SimulationClock clock, double elapsedSeconds)
        {
            if (world == null || clock == null || elapsedSeconds <= 0d || double.IsNaN(elapsedSeconds))
            {
                return new OfflineReport(0d, 0d, 0d, 0d, 0L);
            }

            double cap = Math.Max(0d, world.Content.MaximumOfflineSeconds);
            double simulated = Math.Min(elapsedSeconds, cap);

            double goldBefore = world.Economy.Get(CurrencyType.Gold);
            double reputationBefore = world.Economy.Get(CurrencyType.Reputation);
            long questsBefore = clock.QuestsCompleted;

            clock.Advance(simulated);

            return new OfflineReport(
                elapsedSeconds,
                simulated,
                Math.Max(0d, world.Economy.Get(CurrencyType.Gold) - goldBefore),
                Math.Max(0d, world.Economy.Get(CurrencyType.Reputation) - reputationBefore),
                clock.QuestsCompleted - questsBefore);
        }
    }
}
