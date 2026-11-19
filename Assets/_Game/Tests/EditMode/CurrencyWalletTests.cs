using System;
using System.Collections.Generic;
using NUnit.Framework;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class CurrencyWalletTests
    {
        private sealed class MemoryStore : ICurrencyStore
        {
            private readonly Dictionary<string, long> _balances = new Dictionary<string, long>();
            public bool HasSaveFailure { get; set; }

            public long Load(string currencyId)
            {
                return _balances.TryGetValue(currencyId, out long balance) ? balance : 0;
            }

            public void Save(string currencyId, long balance)
            {
                if (HasSaveFailure)
                {
                    throw new InvalidOperationException("Simulated persistence failure.");
                }
                _balances[currencyId] = balance;
            }
        }

        private MemoryStore _store;
        private CurrencyWallet _wallet;
        private EventBus _events;
        private int _notifications;

        [SetUp]
        public void SetUp()
        {
            _store = new MemoryStore();
            _events = new EventBus();
            _wallet = new CurrencyWallet(_store, _events);
            _notifications = 0;
            _events.Subscribe<CurrencyBalanceChanged>(OnBalanceChanged);
        }

        [Test]
        public void CurrenciesHaveIndependentBalances()
        {
            _wallet.Credit("gold", 100);
            _wallet.Credit("cash", 20);
            Assert.IsTrue(_wallet.TrySpend("gold", 30));
            Assert.AreEqual(70, _wallet.GetBalance("gold"));
            Assert.AreEqual(20, _wallet.GetBalance("cash"));
        }

        [Test]
        public void InsufficientFundsDoNotPersistOrNotify()
        {
            Assert.IsFalse(_wallet.TrySpend("gold", 1));
            Assert.AreEqual(0, _wallet.GetBalance("gold"));
            Assert.AreEqual(0, _notifications);
        }

        [Test]
        public void ExactBalanceCanBeSpent()
        {
            _wallet.Credit("gold", 100);
            Assert.IsTrue(_wallet.TrySpend("gold", 100));
            Assert.AreEqual(0, _wallet.GetBalance("gold"));
            Assert.AreEqual(2, _notifications);
        }

        [Test]
        public void BalanceSurvivesServiceRecreation()
        {
            _wallet.Credit("gold", 100);
            _wallet.TrySpend("gold", 25);
            var reloaded = new CurrencyWallet(_store, new EventBus());
            Assert.AreEqual(75, reloaded.GetBalance("gold"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonpositiveTransactionsAreRejected(long amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.Credit("gold", amount));
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.TrySpend("gold", amount));
        }

        [Test]
        public void OverflowDoesNotMutateBalance()
        {
            _wallet.Credit("gold", long.MaxValue);
            Assert.Throws<OverflowException>(() => _wallet.Credit("gold", 1));
            Assert.AreEqual(long.MaxValue, _wallet.GetBalance("gold"));
            Assert.AreEqual(1, _notifications);
        }

        [Test]
        public void PersistenceFailureDoesNotMutateOrNotify()
        {
            _wallet.Credit("gold", 100);
            _store.HasSaveFailure = true;
            Assert.Throws<InvalidOperationException>(() => _wallet.TrySpend("gold", 25));
            Assert.AreEqual(100, _wallet.GetBalance("gold"));
            Assert.AreEqual(1, _notifications);
        }

        private void OnBalanceChanged(CurrencyBalanceChanged message)
        {
            _notifications++;
        }
    }
}
