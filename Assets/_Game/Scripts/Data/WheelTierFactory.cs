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
        private readonly ZoneConfiguration _zones;
        private readonly ZoneWheelBuilder _builder = new ZoneWheelBuilder();

        public WheelTierFactory(ZoneConfiguration zones)
        {
            _zones = zones ?? throw new ArgumentNullException(nameof(zones));
        }

        public ZoneWheel CreateWheel(int zone, ZoneKind kind, IRandomSource random)
        {
            WheelDefinition wheel = _zones.GetWheel(kind);

            if (!wheel)
            {
                throw new InvalidOperationException("A wheel definition is required for each zone.");
            }

            int sliceCount = wheel.SliceCount;
            var pool = BuildPool(wheel, zone);
            return _builder.Build(sliceCount, kind, pool, random);
        }

        /// <summary>Builds the weighted pool for a zone, scaling reward amounts with progression.</summary>
        public static List<RewardPoolEntry> BuildPool(WheelDefinition wheel, int zone)
        {
            var pool = new List<RewardPoolEntry>();

            RewardPool selectedPool = wheel ? wheel.GetRewardPool(zone) : null;
            if (selectedPool)
            {
                var entries = selectedPool.Entries;
                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (!entry.reward || !entry.reward.IsAvailableAt(zone)) continue;

                    double scale = 1d + wheel.AmountScalePerZone * (zone - 1);
                    // Skins are a single item; everything else keeps the per-zone scaling.
                    long amount = entry.reward.IsAmountScaled
                        ? (long)Math.Round(entry.reward.Amount * scale)
                        : entry.reward.Amount;
                    if (amount < 1) amount = 1;

                    pool.Add(new RewardPoolEntry(entry.reward.RewardId, amount, entry.weight));
                }
            }

            if (pool.Count == 0)
            {
                throw new InvalidOperationException("The wheel reward pool must contain valid rewards.");
            }

            return pool;
        }

    }
}
