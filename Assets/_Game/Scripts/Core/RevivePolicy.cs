using System;

namespace Vertigo.WheelOfFortune.Core
{
    /// <summary>Price and per-run limit of reviving after a bomb.</summary>
    public sealed class RevivePolicy
    {
        #region Configuration


        private readonly long _baseCost;
        private readonly long _costPerZone;

        public RevivePolicy(string currencyId, long baseCost, long costPerZone, int maxRevives)
        {
            if (string.IsNullOrWhiteSpace(currencyId))
            {
                throw new ArgumentException("Currency is required.", nameof(currencyId));
            }
            if (baseCost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseCost));
            }
            if (costPerZone <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(costPerZone));
            }
            if (maxRevives < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRevives));
            }
            CurrencyId = currencyId;
            MaxRevives = maxRevives;
            _baseCost = baseCost;
            _costPerZone = costPerZone;
        }

        #endregion

        #region Pricing

        public string CurrencyId { get; }
        public int MaxRevives { get; }

        /// <summary>Zone one costs the base price; each later zone adds the per-zone price.</summary>
        public long GetCost(int zone)
        {
            if (zone < ZoneRules.FIRST_ZONE)
            {
                throw new ArgumentOutOfRangeException(nameof(zone));
            }
            return checked(_baseCost + (zone - (long)ZoneRules.FIRST_ZONE) * _costPerZone);
        }

        #endregion
    }
}
