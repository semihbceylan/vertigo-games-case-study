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

            if (wheel != null && wheel.UseExplicitSlices && wheel.Slices != null && wheel.Slices.Length > 0)
                return BuildExplicit(wheel, zone, kind);

            int sliceCount = wheel != null ? wheel.SliceCount : 8;
            var pool = BuildPool(wheel, zone);
            if (kind == ZoneKind.Super)
                pool = FilterSuperPool(pool, zone);
            return _builder.Build(sliceCount, kind, pool, random);
        }

        /// <summary>Builds a wheel directly from the authored slice list, scaling amounts per zone.</summary>
        private static ZoneWheel BuildExplicit(WheelDefinition wheel, int zone, ZoneKind kind)
        {
            var definitions = wheel.Slices;
            var slices = new WheelSlice[definitions.Length];
            double scale = 1d + wheel.AmountScalePerZone * (zone - 1);
            bool bombFree = ZoneClassifier.IsBombFree(kind);

            for (int i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];

                if (definition.isBomb && !bombFree)
                {
                    slices[i] = WheelSlice.Bomb;
                    continue;
                }

                long baseAmount = definition.amount > 0
                    ? definition.amount
                    : (definition.reward != null ? definition.reward.Amount : 0);

                // Skins are a single item; everything else keeps the per-zone scaling.
                long amount = IsSkin(definition.reward) ? 1 : (long)Math.Round(baseAmount * scale);
                if (amount < 1) amount = 1;

                string rewardId = definition.reward != null ? definition.reward.RewardId : "empty";
                slices[i] = new WheelSlice(false, new RewardGrant(rewardId, amount));
            }

            return new ZoneWheel(slices);
        }

        /// <summary>Builds the weighted pool for a zone, scaling reward amounts with progression.</summary>
        public static List<RewardPoolEntry> BuildPool(WheelDefinition wheel, int zone)
        {
            var pool = new List<RewardPoolEntry>();

            if (wheel != null && wheel.RewardPool != null)
            {
                var entries = wheel.RewardPool.Entries;
                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry.reward == null) continue;

                    double scale = 1d + wheel.AmountScalePerZone * (zone - 1);
                    // Skins are a single item; everything else keeps the per-zone scaling.
                    long amount = IsSkin(entry.reward) ? 1 : (long)Math.Round(entry.reward.Amount * scale);
                    if (amount < 1) amount = 1;

                    pool.Add(new RewardPoolEntry(entry.reward.RewardId, amount, entry.weight));
                }
            }

            if (pool.Count == 0)
                pool.Add(new RewardPoolEntry("empty", 1, 1));

            return pool;
        }

        /// <summary>
        /// Skins — weapon renders, melee weapons and helmets — are one-off cosmetics, so they always
        /// grant a single item and skip the per-zone amount scaling. Everything else (currency,
        /// chests, points and consumables) keeps the scaling algorithm.
        /// </summary>
        private static bool IsSkin(RewardDefinition reward)
        {
            if (reward == null) return false;
            return reward.Category == RewardCategory.WeaponRender
                || reward.Category == RewardCategory.Helmet;
        }

        private static readonly HashSet<string> SuperRenderIds = new HashSet<string>
        {
            "t1_shotgun", "t2_mle", "t2_rifle", "t3_shotgun", "t3_smg", "t3_sniper"
        };

        private const string SuperMeleeFirst = "bayonet_vice"; // zone 30
        private const string SuperMeleeSecond = "bayonet_time"; // zone 60

        /// <summary>
        /// Restricts super wheels to renders + the zone's melee weapon.
        /// Zone 30 uses Vice, zone 60+ uses Time. Falls back to the unfiltered pool
        /// when nothing matches (e.g. custom test wheels) so we never return empty.
        /// </summary>
        private static List<RewardPoolEntry> FilterSuperPool(List<RewardPoolEntry> pool, int zone)
        {
            string melee = zone >= 60 ? SuperMeleeSecond : SuperMeleeFirst;
            var filtered = new List<RewardPoolEntry>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                string id = pool[i].RewardId;
                if (SuperRenderIds.Contains(id) || id == melee)
                    filtered.Add(pool[i]);
            }

            return filtered.Count > 0 ? filtered : pool;
        }
    }
}
