using System.Collections.Generic;
using NUnit.Framework;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class RunCurrencySettlementTests
    {
        private sealed class Store : ICurrencyStore
        {
            private readonly Dictionary<string, long> _balances = new Dictionary<string, long>();
            public long Load(string id)
            {
                long balance;
                return _balances.TryGetValue(id, out balance) ? balance : 0;
            }
            public void Save(string id, long balance)
            {
                _balances[id] = balance;
            }
        }

        private sealed class Resolver : IRewardCurrencyResolver
        {
            public string ResolveCurrencyId(string id)
            {
                if (id == "coins") return "gold";
                if (id == "gems") return "premium";
                return null;
            }
        }

        [Test]
        public void SuccessfulCompletionDepositsConfiguredCurrenciesWithoutChangingRunLoot()
        {
            var events = new EventBus();
            var rewards = new RewardWallet();
            var wallet = new CurrencyWallet(new Store(), events);
            var settlement = new RunCurrencySettlement(events, rewards, wallet, new Resolver());
            settlement.Subscribe();
            rewards.Add(new RewardGrant("coins", 100));
            rewards.Add(new RewardGrant("gems", 5));
            rewards.Add(new RewardGrant("equipment", 1));
            events.Publish(new RunCompleted(106));
            Assert.AreEqual(100, wallet.GetBalance("gold"));
            Assert.AreEqual(5, wallet.GetBalance("premium"));
            Assert.AreEqual(106, rewards.AccumulatedAmount);
        }

        [Test]
        public void RepeatedSubscriptionsDoNotDuplicatePayoutAndUnsubscribeStopsSettlement()
        {
            var events = new EventBus();
            var rewards = new RewardWallet();
            rewards.Add(new RewardGrant("coins", 10));
            var wallet = new CurrencyWallet(new Store(), events);
            var settlement = new RunCurrencySettlement(events, rewards, wallet, new Resolver());
            for (int i = 0; i < 3; i++)
            {
                settlement.Subscribe();
                settlement.Subscribe();
                events.Publish(new RunCompleted(10));
                Assert.AreEqual((i + 1) * 10, wallet.GetBalance("gold"));
                settlement.Unsubscribe();
                events.Publish(new RunCompleted(10));
                Assert.AreEqual((i + 1) * 10, wallet.GetBalance("gold"));
            }
        }

        [Test]
        public void BombAndRewardNotificationsNeverDepositAtRiskRewards()
        {
            var events = new EventBus();
            var rewards = new RewardWallet();
            var wallet = new CurrencyWallet(new Store(), events);
            var settlement = new RunCurrencySettlement(events, rewards, wallet, new Resolver());
            settlement.Subscribe();
            rewards.Add(new RewardGrant("coins", 100));
            events.Publish(new RewardCollected(new RewardGrant("coins", 100)));
            rewards.Clear();
            events.Publish(new BombHit(1));
            Assert.AreEqual(0, wallet.GetBalance("gold"));
        }
    }
}
