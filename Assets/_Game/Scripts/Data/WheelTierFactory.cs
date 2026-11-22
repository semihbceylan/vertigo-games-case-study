using System;
using System.Collections.Generic;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>
    /// Bridges the ScriptableObject configuration to the pure gameplay core by producing a
    /// <see cref="ZoneWheel"/> for a given zone. Keeps <see cref="GameSession"/> framework-free.
    /// </summary>
    public sealed class WheelTierFactory : IZoneWheelFactory
    {
        #region Configuration

        /// <summary>Zone one shows authored amounts unchanged.</summary>
        private const double BASE_AMOUNT_SCALE = 1d;
        /// <summary>Rounding never reduces a reward to nothing.</summary>
        private const long MINIMUM_AMOUNT = 1;

        private readonly ZoneConfiguration _zones;
        private readonly ZoneWheelBuilder _builder = new ZoneWheelBuilder();
        /// <summary>Reused for every wheel; the builder only reads it while building, so no per-zone list is allocated.</summary>
        private readonly List<RewardPoolEntry> _pool = new List<RewardPoolEntry>();

        public WheelTierFactory(ZoneConfiguration zones)
        {
            _zones = zones ?? throw new ArgumentNullException(nameof(zones));
        }

        #endregion

        #region Wheel Construction

        public ZoneWheel CreateWheel(int zone, ZoneKind kind, IRandomSource random)
        {
            WheelDefinition wheel = _zones.GetWheel(kind);
            if (!wheel)
            {
                throw new InvalidOperationException("A wheel definition is required for each zone.");
            }
            FillPool(wheel, zone);
            return _builder.Build(wheel.SliceCount, kind, _pool, random);
        }

        /// <summary>Refills the reusable weighted pool for a zone, scaling reward amounts with progression.</summary>
        private void FillPool(WheelDefinition wheel, int zone)
        {
            _pool.Clear();
            RewardPool selectedPool = wheel.GetRewardPool(zone);
            if (selectedPool)
            {
                double scale = BASE_AMOUNT_SCALE + wheel.AmountScalePerZone * (zone - ZoneRules.FIRST_ZONE);
                RewardPoolItem[] entries = selectedPool.Entries;
                for (int i = 0; i < entries.Length; i++)
                {
                    RewardDefinition reward = entries[i].Reward;
                    if (!reward)
                    {
                        throw new InvalidOperationException("Reward pool '" + selectedPool.name + "' contains an empty entry.");
                    }
                    if (!reward.IsAvailableAt(zone))
                    {
                        continue;
                    }
                    long amount = reward.IsAmountScaled ? (long)Math.Round(reward.Amount * scale) : reward.Amount;
                    _pool.Add(new RewardPoolEntry(reward.RewardId, Math.Max(MINIMUM_AMOUNT, amount), entries[i].Weight));
                }
            }

            if (_pool.Count == 0)
            {
                throw new InvalidOperationException("The wheel reward pool must contain valid rewards.");
            }
        }

        #endregion
    }
}
