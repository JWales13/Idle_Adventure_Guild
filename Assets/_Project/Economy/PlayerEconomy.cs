using System;
using System.Collections.Generic;
using IdleGuild.Core;
using IdleGuild.Core.Events;

namespace IdleGuild.Economy
{
    /// <summary>
    /// Every balance the player holds, and the only place they change.
    ///
    /// Plain C# with no Unity lifecycle and no knowledge of UI. Balances are
    /// <see cref="double"/> rather than a big-number type: four guild tiers do not
    /// reach the point where double loses useful precision, and the simplicity is
    /// worth more over 27 days than headroom the MVP will never use. Revisit only if
    /// a post-launch prestige loop pushes totals past roughly 1e15.
    /// </summary>
    public sealed class PlayerEconomy
    {
        private readonly Dictionary<CurrencyType, double> _balances = new Dictionary<CurrencyType, double>();

        public PlayerEconomy()
        {
            foreach (CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
            {
                _balances[currency] = 0d;
            }
        }

        /// <summary>All balances. Save/load reads this directly.</summary>
        public IReadOnlyDictionary<CurrencyType, double> Balances => _balances;

        public double Get(CurrencyType currency)
        {
            return _balances.TryGetValue(currency, out double balance) ? balance : 0d;
        }

        public bool CanAfford(CurrencyType currency, double amount)
        {
            return amount <= 0d || Get(currency) >= amount;
        }

        /// <summary>
        /// Add to a balance and announce it. Non-positive amounts are ignored rather
        /// than treated as a spend — a negative grant is a caller bug, and letting it
        /// silently drain a balance would be far harder to trace than doing nothing.
        /// </summary>
        public void Grant(CurrencyType currency, double amount)
        {
            if (amount <= 0d || double.IsNaN(amount))
            {
                return;
            }

            double updated = Get(currency) + amount;
            _balances[currency] = updated;
            EventBus.Publish(new CurrencyChanged(currency, updated, amount));
        }

        /// <summary>
        /// Deduct if affordable. Returns false and changes nothing otherwise, so callers
        /// can gate a purchase on the return value without checking the balance first.
        /// </summary>
        public bool TrySpend(CurrencyType currency, double amount)
        {
            if (double.IsNaN(amount))
            {
                return false;
            }

            if (amount <= 0d)
            {
                return true;
            }

            double balance = Get(currency);
            if (balance < amount)
            {
                return false;
            }

            double updated = balance - amount;
            _balances[currency] = updated;
            EventBus.Publish(new CurrencyChanged(currency, updated, -amount));
            return true;
        }

        /// <summary>
        /// Overwrite a balance outright. For save restoration only — it bypasses the
        /// affordability rules that <see cref="TrySpend"/> enforces, and still announces
        /// the change so UI bound to the bus reflects a loaded game.
        /// </summary>
        public void Restore(CurrencyType currency, double balance)
        {
            if (double.IsNaN(balance) || balance < 0d)
            {
                balance = 0d;
            }

            double previous = Get(currency);
            _balances[currency] = balance;
            EventBus.Publish(new CurrencyChanged(currency, balance, balance - previous));
        }
    }
}
