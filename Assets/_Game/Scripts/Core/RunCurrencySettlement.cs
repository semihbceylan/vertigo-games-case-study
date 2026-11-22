using System;
using System.Collections.Generic;

namespace Vertigo.WheelOfFortune.Core
{
    public interface IRewardCurrencyResolver
    {
        string ResolveCurrencyId(string rewardId);
    }

    /// <summary>Session-scoped successful-run settlement. Does not control session progression.</summary>
    public sealed class RunCurrencySettlement
    {
        #region Construction

        private readonly IEventBus _events;
        private readonly RewardWallet _rewards;
        private readonly ICurrencyWallet _currencies;
        private readonly IRewardCurrencyResolver _resolver;

        public RunCurrencySettlement(IEventBus events, RewardWallet rewards, ICurrencyWallet currencies,
            IRewardCurrencyResolver resolver)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
            _currencies = currencies ?? throw new ArgumentNullException(nameof(currencies));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        #endregion

        #region Subscription

        /// <summary>The event bus ignores duplicate handlers, so repeated enable cycles are safe.</summary>
        public void Subscribe()
        {
            _events.Subscribe<RunCompleted>(OnRunCompleted);
        }

        public void Unsubscribe()
        {
            _events.Unsubscribe<RunCompleted>(OnRunCompleted);
        }

        private void OnRunCompleted(RunCompleted message)
        {
            foreach (KeyValuePair<string, long> pair in _rewards.Totals)
            {
                string currencyId = _resolver.ResolveCurrencyId(pair.Key);
                if (!string.IsNullOrEmpty(currencyId))
                {
                    _currencies.Credit(currencyId, pair.Value);
                }
            }
        }

        #endregion
    }
}
