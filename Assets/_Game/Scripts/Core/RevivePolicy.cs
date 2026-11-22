using System;

namespace Vertigo.WheelOfFortune.Core
{
    public sealed class RevivePolicy
    {
        public const int DEFAULT_MAX_REVIVES = 4;
        public int MaxRevives { get; }
        public string CurrencyId { get; }
        private readonly long _baseCost;
        private readonly long _costPerZone;

        public RevivePolicy(string currencyId, long baseCost, long costPerZone, int maxRevives = DEFAULT_MAX_REVIVES)
        {
            if (string.IsNullOrWhiteSpace(currencyId)) throw new ArgumentException("Currency is required.", nameof(currencyId));
            if (baseCost <= 0) throw new ArgumentOutOfRangeException(nameof(baseCost));
            if (costPerZone <= 0) throw new ArgumentOutOfRangeException(nameof(costPerZone));
            if (maxRevives < 1) throw new ArgumentOutOfRangeException(nameof(maxRevives));
            MaxRevives = maxRevives;
            CurrencyId = currencyId;
            _baseCost = baseCost;
            _costPerZone = costPerZone;
        }

        public long GetCost(int zone)
        {
            if (zone < 1) throw new ArgumentOutOfRangeException(nameof(zone));
            return checked(_baseCost + (zone - 1L) * _costPerZone);
        }
    }
}
