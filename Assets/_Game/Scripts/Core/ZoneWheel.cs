using System;
using System.Collections.Generic;

namespace Vertigo.WheelOfFortune.Core
{
    /// <summary>Outcome stored on a single wheel slice.</summary>
    public readonly struct WheelSlice
    {
        public readonly bool IsBomb;
        public readonly RewardGrant Reward;

        public WheelSlice(bool isBomb, RewardGrant reward)
        {
            IsBomb = isBomb;
            Reward = reward;
        }

        public static WheelSlice Bomb => new WheelSlice(true, default(RewardGrant));
    }

    /// <summary>Immutable slice layout for one zone.</summary>
    public sealed class ZoneWheel
    {
        private readonly WheelSlice[] _slices;

        public ZoneWheel(WheelSlice[] slices)
        {
            _slices = slices ?? throw new ArgumentNullException(nameof(slices));
        }

        public int SliceCount => _slices.Length;

        public WheelSlice this[int index] => _slices[index];

        public int BombCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _slices.Length; i++)
                {
                    if (_slices[i].IsBomb) count++;
                }
                return count;
            }
        }
    }

    /// <summary>A weighted reward pool entry used to fill non-bomb slices.</summary>
    public readonly struct RewardPoolEntry
    {
        public readonly string RewardId;
        public readonly long Amount;
        public readonly double Weight;

        public RewardPoolEntry(string rewardId, long amount, double weight)
        {
            RewardId = rewardId ?? string.Empty;
            Amount = amount < 0 ? 0 : amount;
            Weight = weight < 0 ? 0 : weight;
        }
    }

    /// <summary>Creates the wheel for a given zone. Implemented by the Unity configuration layer.</summary>
    public interface IZoneWheelFactory
    {
        ZoneWheel CreateWheel(int zone, ZoneKind kind, IRandomSource random);
    }

    /// <summary>
    /// Builds a <see cref="ZoneWheel"/>: Normal zones get exactly one bomb, Safe/Super get none.
    /// Rewards are drawn from the weighted pool.
    /// </summary>
    public sealed class ZoneWheelBuilder
    {
        public ZoneWheel Build(int sliceCount, ZoneKind kind, IReadOnlyList<RewardPoolEntry> rewardPool, IRandomSource random)
        {
            if (sliceCount < 1)
                throw new ArgumentOutOfRangeException(nameof(sliceCount), "A wheel needs at least one slice.");

            var slices = new WheelSlice[sliceCount];

            int bombIndex = ZoneClassifier.IsBombFree(kind) ? -1 : random.NextInt(0, sliceCount);

            for (int i = 0; i < sliceCount; i++)
            {
                slices[i] = i == bombIndex
                    ? WheelSlice.Bomb
                    : new WheelSlice(false, PickReward(rewardPool, random));
            }

            return new ZoneWheel(slices);
        }

        private static RewardGrant PickReward(IReadOnlyList<RewardPoolEntry> pool, IRandomSource random)
        {
            if (pool == null || pool.Count == 0)
                return new RewardGrant("empty", 0);

            double total = 0d;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Weight > 0d) total += pool[i].Weight;
            }

            if (total <= 0d)
                return new RewardGrant(pool[0].RewardId, pool[0].Amount);

            double roll = random.NextDouble() * total;
            double accumulated = 0d;
            for (int i = 0; i < pool.Count; i++)
            {
                accumulated += pool[i].Weight;
                if (roll <= accumulated)
                    return new RewardGrant(pool[i].RewardId, pool[i].Amount);
            }

            var last = pool[pool.Count - 1];
            return new RewardGrant(last.RewardId, last.Amount);
        }
    }
}
