using System;
using System.Collections.Generic;

namespace Vertigo.WheelOfFortune.Core
{
    public interface ICurrencyStore
    {
        /// <summary>False when nothing has been saved yet for <paramref name="currencyId"/>.</summary>
        bool TryLoad(string currencyId, out long balance);
        void Save(string currencyId, long balance);
    }

    public interface ICurrencyWallet
    {
        long GetBalance(string currencyId);
        void Credit(string currencyId, long amount);
        bool TrySpend(string currencyId, long amount);
    }

    public readonly struct CurrencyBalanceChanged
    {
        public string CurrencyId { get; }
        public long Balance { get; }

        public CurrencyBalanceChanged(string currencyId, long balance)
        {
            CurrencyId = currencyId;
            Balance = balance;
        }
    }

    /// <summary>
    /// Persistent currency balances, independent of rewards at risk in a run. A currency with no saved
    /// balance starts at its configured starting balance; once saved, the stored value always wins.
    /// </summary>
    public sealed class CurrencyWallet : ICurrencyWallet
    {
        #region Dependencies

        private readonly ICurrencyStore _store;
        private readonly IEventBus _events;
        private readonly Dictionary<string, long> _balances = new Dictionary<string, long>();
        private readonly Dictionary<string, long> _startingBalances = new Dictionary<string, long>();

        public CurrencyWallet(ICurrencyStore store, IEventBus events,
            IReadOnlyDictionary<string, long> startingBalances = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            if (startingBalances == null)
            {
                return;
            }
            foreach (KeyValuePair<string, long> pair in startingBalances)
            {
                ValidateId(pair.Key);
                if (pair.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(startingBalances), "Starting balances cannot be negative.");
                }
                _startingBalances.Add(pair.Key, pair.Value);
            }
        }

        #endregion

        #region Transactions

        public long GetBalance(string currencyId)
        {
            ValidateId(currencyId);
            if (!_balances.TryGetValue(currencyId, out long balance))
            {
                if (!_store.TryLoad(currencyId, out balance))
                {
                    _startingBalances.TryGetValue(currencyId, out balance);
                }
                if (balance < 0)
                {
                    throw new InvalidOperationException("Stored currency balance cannot be negative.");
                }
                _balances.Add(currencyId, balance);
            }
            return balance;
        }

        public void Credit(string currencyId, long amount)
        {
            ValidateAmount(amount);
            long balance = checked(GetBalance(currencyId) + amount);
            Commit(currencyId, balance);
        }

        public bool TrySpend(string currencyId, long amount)
        {
            ValidateAmount(amount);
            long balance = GetBalance(currencyId);
            if (balance < amount)
            {
                return false;
            }
            Commit(currencyId, balance - amount);
            return true;
        }

        private void Commit(string currencyId, long balance)
        {
            // Persist first: a failing store must not change the in-memory balance or notify UI.
            _store.Save(currencyId, balance);
            _balances[currencyId] = balance;
            _events.Publish(new CurrencyBalanceChanged(currencyId, balance));
        }

        private static void ValidateId(string currencyId)
        {
            if (string.IsNullOrWhiteSpace(currencyId))
            {
                throw new ArgumentException("Currency requires a nonempty identity.", nameof(currencyId));
            }
        }

        private static void ValidateAmount(long amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Transactions require a positive amount.");
            }
        }

        #endregion
    }
}
