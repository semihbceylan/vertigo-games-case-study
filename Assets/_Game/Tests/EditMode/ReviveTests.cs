using System;
using System.Collections.Generic;
using NUnit.Framework;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ReviveTests
    {
        private const int TEST_MAX_REVIVES = 4;

        private sealed class Store : ICurrencyStore
        {
            private readonly Dictionary<string, long> _balances = new Dictionary<string, long>();

            public long Load(string currencyId)
            {
                return _balances.TryGetValue(currencyId, out long value) ? value : 0;
            }

            public void Save(string currencyId, long balance)
            {
                _balances[currencyId] = balance;
            }
        }

        private static GameSession CreateSession(ICurrencyWallet currencies)
        {
            return new GameSession(TestZones.CreateRules(),
                new FakeWheelFactory((zone, kind) => Wheels.WithBomb(2, 0, "loot")),
                new ScriptedRandom(), currencies: currencies, revivePolicy: new RevivePolicy("gold", 100, 25, TEST_MAX_REVIVES));
        }

        private static void Bomb(GameSession session)
        {
            session.BeginSpin();
            session.ResolveSpin(0);
        }

        [TestCase(1, 100)]
        [TestCase(2, 125)]
        [TestCase(30, 825)]
        public void PriceIncreasesByFailedZone(int zone, long expected)
        {
            Assert.AreEqual(expected, new RevivePolicy("gold", 100, 25, TEST_MAX_REVIVES).GetCost(zone));
        }

        [Test]
        public void InvalidPricingIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new RevivePolicy("", 100, 25, TEST_MAX_REVIVES));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RevivePolicy("gold", 0, 25, TEST_MAX_REVIVES));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RevivePolicy("gold", 100, 0, TEST_MAX_REVIVES));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RevivePolicy("gold", 100, 25, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RevivePolicy("gold", 100, 25, TEST_MAX_REVIVES).GetCost(0));
            Assert.Throws<OverflowException>(() => new RevivePolicy("gold", long.MaxValue, 1, TEST_MAX_REVIVES).GetCost(2));
        }

        [Test]
        public void ReviveRestoresAllLootAtFailedZoneWithoutBankingIt()
        {
            var wallet = new CurrencyWallet(new Store(), new EventBus());
            wallet.Credit("gold", 1000);
            var session = CreateSession(wallet);
            session.Wallet.Add(new RewardGrant("gold", 17));
            session.Wallet.Add(new RewardGrant("cash", 23));
            session.JumpToZone(4);
            Bomb(session);
            Assert.IsTrue(session.Wallet.IsEmpty);
            Assert.IsTrue(session.TryRevive());
            Assert.AreEqual(4, session.Zone);
            Assert.AreEqual(17, session.Wallet.Totals["gold"]);
            Assert.AreEqual(23, session.Wallet.Totals["cash"]);
            Assert.AreEqual(825, wallet.GetBalance("gold"));
            Assert.AreEqual(0, wallet.GetBalance("cash"));
            Assert.AreEqual(0, session.BankedTotal);
            Assert.IsTrue(session.IsSpinAllowed);
            Assert.IsFalse(session.TryRevive());
        }

        [Test]
        public void InsufficientFundsKeepsOfferAndRecoveryForLaterAttempt()
        {
            var wallet = new CurrencyWallet(new Store(), new EventBus());
            var session = CreateSession(wallet);
            session.Wallet.Add(new RewardGrant("loot", 42));
            Bomb(session);
            ZoneWheel wheel = session.CurrentWheel;
            Assert.IsTrue(session.IsReviveAllowed);
            Assert.IsFalse(session.TryRevive());
            Assert.AreSame(wheel, session.CurrentWheel);
            Assert.AreEqual(4, session.RevivesRemaining);
            Assert.IsTrue(session.IsRestartAllowed);
            Assert.IsTrue(session.Wallet.IsEmpty);
            Assert.AreEqual(0, wallet.GetBalance("gold"));
            wallet.Credit("gold", 100);
            Assert.IsTrue(session.TryRevive());
            Assert.AreEqual(42, session.Wallet.AccumulatedAmount);
        }

        [Test]
        public void FourPaidRevivesAreAllowedAndFifthIsRejectedWithoutSpending()
        {
            var wallet = new CurrencyWallet(new Store(), new EventBus());
            wallet.Credit("gold", 1000);
            var session = CreateSession(wallet);
            session.Wallet.Add(new RewardGrant("loot", 42));
            for (int i = 0; i < 4; i++)
            {
                Bomb(session);
                Assert.IsTrue(session.TryRevive());
                Assert.AreEqual(42, session.Wallet.AccumulatedAmount);
                Assert.AreEqual(1, session.Wallet.Count);
                Assert.AreEqual(3 - i, session.RevivesRemaining);
            }
            Bomb(session);
            Assert.IsFalse(session.IsReviveAllowed);
            Assert.IsFalse(session.TryRevive());
            Assert.IsTrue(session.IsRestartAllowed);
            Assert.IsTrue(session.Wallet.IsEmpty);
            Assert.AreEqual(600, wallet.GetBalance("gold"));
            session.Restart();
            Assert.AreEqual(4, session.RevivesRemaining);
            Bomb(session);
            Assert.IsTrue(session.TryRevive());
        }

        [Test]
        public void FreeRestartDiscardsPreviousRecovery()
        {
            var wallet = new CurrencyWallet(new Store(), new EventBus());
            wallet.Credit("gold", 100);
            var session = CreateSession(wallet);
            session.Wallet.Add(new RewardGrant("loot", 42));
            Bomb(session);
            session.Restart();
            Assert.AreEqual(100, wallet.GetBalance("gold"));
            Bomb(session);
            Assert.IsTrue(session.TryRevive());
            Assert.IsTrue(session.Wallet.IsEmpty);
        }

        [Test]
        public void ReviveCannotSpendWhileReadyOrSpinning()
        {
            var wallet = new CurrencyWallet(new Store(), new EventBus());
            wallet.Credit("gold", 100);
            var session = CreateSession(wallet);
            Assert.IsFalse(session.IsReviveAllowed);
            Assert.IsFalse(session.TryRevive());
            session.BeginSpin();
            Assert.IsFalse(session.TryRevive());
            Assert.AreEqual(100, wallet.GetBalance("gold"));
        }
    }
}
