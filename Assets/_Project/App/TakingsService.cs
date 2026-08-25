using System;
using IdleGuild.Core;
using IdleGuild.Core.Events;
using IdleGuild.Guild;

namespace IdleGuild.App
{
    /// <summary>
    /// The tap: serving a waiting customer yourself.
    ///
    /// <b>This is an obligation the design took on, not a feature added to it.</b> §6B
    /// sells the whole monetisation model on one sentence — "a free player can do
    /// everything a payer can and simply has to tap" — and the premium pillar is a
    /// familiar that "minds a room while you are away: collecting takings". For that to
    /// be worth a Boon, the takings must otherwise need collecting. So the monetisation
    /// design was load-bearing on a mechanic that neither the code nor the model had
    /// until Day 15 noticed, and there is nothing to automate and nothing to sell
    /// without it.
    ///
    /// <b>It is throughput, which is the lever that already had a home for it.</b> The
    /// tier's base service is the guildmaster working the bar; a tap is one more
    /// customer through that same door. That placement is what makes it safe:
    ///
    ///   * it is capped by unserved demand, so it can never invent custom that is not
    ///     there, and is worth exactly nothing once staff cover the room
    ///   * it therefore decays on its own — large early, irrelevant by City — with no
    ///     late-game balance problem to tune away
    ///   * it touches neither demand (the tier's lever) nor capacity (the room's), so
    ///     §3.1's three levers with no overlap survive intact
    ///
    /// <b>The queue is the part the model does not have and the game needs.</b> The
    /// model treats tapping as a rate, which is fine for a simulation and useless in a
    /// game: unserved demand is customers <i>per hour</i>, and a player with a fast
    /// thumb would otherwise draw an hour of custom out of it in three seconds. So the
    /// rate fills a queue instead — people waiting at the bar, which is what unserved
    /// demand physically is — and a tap serves one of them. The queue is capped, so
    /// eight hours away cannot bank eight hours of taps: coming back to a wall of free
    /// gold would make the mechanic a reason to close the game, which is the exact
    /// inversion of what it is for.
    ///
    /// Its earnings are room income and are counted as such. Keeping them inside the
    /// gross is what stops the thumb quietly moving the 70/30 split the whole revision
    /// exists to create.
    /// </summary>
    public sealed class TakingsService
    {
        private readonly GameWorld _world;
        private readonly TradeService _trade;

        private double _waitingCustomers;

        public TakingsService(GameWorld world, TradeService trade)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _trade = trade ?? throw new ArgumentNullException(nameof(trade));
        }

        /// <summary>
        /// How many are waiting to be served by hand. Fractional, because it fills at a
        /// rate; a tap needs a whole one.
        /// </summary>
        public double WaitingCustomers => _waitingCustomers;

        /// <summary>Whole customers a tap could serve right now.</summary>
        public int ServableNow => (int)Math.Floor(_waitingCustomers);

        /// <summary>Lifetime gold the player has served by hand. Saved, because it is part of the room total.</summary>
        public double LifetimeTakings { get; private set; }

        /// <summary>The most the queue will ever hold, from the catalogue.</summary>
        public double Capacity => _world.Content.MaxWaitingCustomers;

        /// <summary>
        /// Let the queue fill for <paramref name="seconds"/> at the rate custom is
        /// currently going unserved. Driven by the clock, so it fills identically online
        /// and offline — and then stops at the cap, which is what makes an absence worth
        /// no more than a moment's inattention.
        /// </summary>
        public void Accrue(double seconds)
        {
            if (seconds <= 0d || double.IsNaN(seconds))
            {
                return;
            }

            double arriving = _trade.UnservedWantPerHour() * seconds / 3600d;
            if (arriving <= 0d)
            {
                return;
            }

            _waitingCustomers = Math.Min(Capacity, _waitingCustomers + arriving);
        }

        /// <summary>
        /// What <see cref="TryCollect"/> would pay, without changing anything. Zero when
        /// nobody is waiting — which is the honest answer once the guild is properly
        /// staffed, and the reason a familiar bought late is a familiar wasted.
        /// </summary>
        public double PreviewCollect(out BuildingDefinition room)
        {
            room = null;
            if (ServableNow < 1)
            {
                return 0d;
            }

            room = _trade.BestUnservedRoom(out double spend);
            return room == null ? 0d : spend;
        }

        /// <summary>
        /// Serve one waiting customer by hand and take their money.
        ///
        /// They come from the best-paying room that still has custom going unserved,
        /// which is the same priority rule the staff follow: you serve the good table
        /// first. Returns false when nobody is waiting, or when the staff have every
        /// room covered and there is nobody left to serve.
        ///
        /// Announced through <c>PlayerEconomy.Grant</c> rather than the silent
        /// accrual the rooms use, because a tap is something the player did and wants to
        /// see land — the opposite case to idle income, and the reason the two paths are
        /// separate.
        /// </summary>
        public bool TryCollect(out double gold, out BuildingDefinition room)
        {
            gold = PreviewCollect(out room);
            if (room == null || gold <= 0d)
            {
                return false;
            }

            _waitingCustomers -= 1d;
            LifetimeTakings += gold;
            _world.Economy.Grant(CurrencyType.Gold, gold);

            EventBus.Publish(new TakingsCollected(room.Id, gold, _waitingCustomers));
            return true;
        }

        /// <summary>
        /// Put the queue and the lifetime total back to a saved reading. Restoration
        /// only — the queue is filled by time passing and by nothing else.
        ///
        /// The queue is clamped to the current cap on the way in, so a save written when
        /// the catalogue allowed a longer queue cannot hand the player more taps than
        /// today's rules do.
        /// </summary>
        public void RestoreState(double waitingCustomers, double lifetimeTakings)
        {
            _waitingCustomers = double.IsNaN(waitingCustomers)
                ? 0d
                : Math.Clamp(waitingCustomers, 0d, Capacity);

            LifetimeTakings = double.IsNaN(lifetimeTakings) ? 0d : Math.Max(0d, lifetimeTakings);
        }
    }
}
