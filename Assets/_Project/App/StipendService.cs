using System;
using IdleGuild.Core;
using IdleGuild.Core.Events;

namespace IdleGuild.App
{
    /// <summary>
    /// The crown's stipend: a delivery of gold that arrives on a timer and can always be
    /// collected, whatever state the guild is in.
    ///
    /// <b>This exists because a playtest reached an unrecoverable state on the third
    /// purchase of a new guild.</b> Tavern to level 1, Tavern to level 2, Inn to level 1
    /// — 147.50 of 150 starting gold — leaving 2.50 against a 25-gold recruit, in a build
    /// where gold comes only from completed contracts and a contract needs an adventurer.
    /// Income was exactly zero and stayed zero. That is not slow, it is finished.
    ///
    /// The shape is Day 4-5's opening deadlock returning with its teeth in. That one was
    /// "solved in data rather than in code" by granting starting gold — but a data
    /// solution that depends on the player spending it correctly is not a solution, it is
    /// a hope, and this is the same hope failing. §01 of the Ledger now carries the rule
    /// that turns this from a balance question into a bug: <b>no sequence of choices may
    /// leave the player unable to make progress.</b>
    ///
    /// Three properties keep it from becoming a fifth room:
    ///
    ///   * <b>Nothing the player buys improves it.</b> It is not a lever, it never enters
    ///     a payback ranking, and it cannot compete with the four rooms for their gold.
    ///     If it ever shows up in a purchase decision, something has gone wrong.
    ///   * <b>It is not room income.</b> Takings are deliberately counted inside the gross
    ///     so the thumb cannot move the 70/30 split the revision is tuned against; the
    ///     stipend is not room trade and gets its own lifetime line, or it moves that
    ///     ratio quietly instead.
    ///   * <b>Deliveries cap.</b> Eight hours away banks three of them, not nine hundred
    ///     and sixty — the same rule and the same reason as the takings queue. Offline
    ///     earnings are <see cref="OfflineProgress"/>'s job and this must not double-dip.
    ///
    /// It scales with the tier so it stays a floor rather than becoming a relic, which is
    /// a deliberate decision with a stated risk: a stipend that outgrows the settlement
    /// stops being a safety net and becomes the economy. The invariant that keeps it
    /// honest lives in the suite — it may never grow faster than the market it backstops.
    /// </summary>
    public sealed class StipendService
    {
        private readonly GameWorld _world;

        private double _deliveriesWaiting;
        private double _secondsUntilNextDelivery;

        public StipendService(GameWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _secondsUntilNextDelivery = CooldownSeconds;
        }

        /// <summary>Seconds between deliveries, from the catalogue.</summary>
        public double CooldownSeconds => Math.Max(1d, _world.Content.StipendCooldownSeconds);

        /// <summary>Most deliveries that can pile up unopened.</summary>
        public int MaximumDeliveries => Math.Max(1, _world.Content.StipendMaximumCharges);

        /// <summary>Gold one delivery is worth at the guild's current tier.</summary>
        public double GoldPerDelivery => Math.Max(0d, _world.GuildState.CurrentTier.StipendGold);

        /// <summary>Deliveries waiting to be opened.</summary>
        public int DeliveriesWaiting => (int)Math.Floor(_deliveriesWaiting);

        /// <summary>True when there is something in the mailbox.</summary>
        public bool CanCollect => DeliveriesWaiting >= 1 && GoldPerDelivery > 0d;

        /// <summary>
        /// Seconds until the next delivery arrives. Zero once the mailbox is full, since
        /// nothing further is coming until something is taken out of it.
        /// </summary>
        public double SecondsUntilNextDelivery =>
            _deliveriesWaiting >= MaximumDeliveries ? 0d : Math.Max(0d, _secondsUntilNextDelivery);

        /// <summary>Lifetime gold collected this way. Its own line, deliberately outside room income.</summary>
        public double LifetimeStipend { get; private set; }

        /// <summary>
        /// What the stipend is worth per hour at the current tier, if every delivery were
        /// collected the moment it arrived.
        ///
        /// Not used by the simulation — the player collects by hand and a familiar will
        /// collect for them later. It exists so a balance pass can compare this against
        /// what the rooms make, which is the comparison that decides whether a scaling
        /// stipend has quietly become an income stream.
        /// </summary>
        public double GoldPerHourIfAlwaysCollected => GoldPerDelivery * 3600d / CooldownSeconds;

        /// <summary>
        /// Let the mailbox fill for <paramref name="seconds"/>. Driven by the clock, so
        /// it fills identically online and offline and then stops at the cap.
        /// </summary>
        public void Accrue(double seconds)
        {
            if (seconds <= 0d || double.IsNaN(seconds))
            {
                return;
            }

            if (_deliveriesWaiting >= MaximumDeliveries)
            {
                // Full. The timer does not run down against a mailbox nobody is emptying,
                // so a player who returns to a full box starts the next delivery from a
                // whole cooldown rather than receiving one instantly.
                _secondsUntilNextDelivery = CooldownSeconds;
                return;
            }

            double remaining = seconds;
            while (remaining > 0d && _deliveriesWaiting < MaximumDeliveries)
            {
                if (_secondsUntilNextDelivery > remaining)
                {
                    _secondsUntilNextDelivery -= remaining;
                    return;
                }

                remaining -= _secondsUntilNextDelivery;
                _deliveriesWaiting += 1d;
                _secondsUntilNextDelivery = CooldownSeconds;
            }
        }

        /// <summary>
        /// Open one delivery and bank it.
        ///
        /// Granted through <c>PlayerEconomy.Grant</c> rather than the silent accrual the
        /// rooms use, because the player pressed something and wants to see it land.
        /// Returns false when the mailbox is empty, or when the current tier's stipend is
        /// zero — which is an unauthored tier asset rather than a rule, and is worth
        /// noticing rather than silently paying nothing.
        /// </summary>
        public bool TryCollect(out double gold)
        {
            gold = 0d;
            if (!CanCollect)
            {
                return false;
            }

            gold = GoldPerDelivery;
            _deliveriesWaiting -= 1d;
            LifetimeStipend += gold;
            _world.Economy.Grant(CurrencyType.Gold, gold);

            EventBus.Publish(new StipendCollected(
                _world.GuildState.CurrentTier.Id, gold, DeliveriesWaiting));
            return true;
        }

        /// <summary>
        /// Put the mailbox back to a saved reading. Restoration only.
        ///
        /// Deliveries are clamped to today's cap on the way in, so a save written when the
        /// catalogue allowed a deeper mailbox cannot hand the player more than today's
        /// rules do — the same guard, for the same reason, as the takings queue.
        /// </summary>
        public void RestoreState(double deliveriesWaiting, double secondsUntilNextDelivery, double lifetimeStipend)
        {
            _deliveriesWaiting = double.IsNaN(deliveriesWaiting)
                ? 0d
                : Math.Clamp(deliveriesWaiting, 0d, MaximumDeliveries);

            _secondsUntilNextDelivery = double.IsNaN(secondsUntilNextDelivery) || secondsUntilNextDelivery <= 0d
                ? CooldownSeconds
                : Math.Min(secondsUntilNextDelivery, CooldownSeconds);

            LifetimeStipend = double.IsNaN(lifetimeStipend) ? 0d : Math.Max(0d, lifetimeStipend);
        }
    }
}
