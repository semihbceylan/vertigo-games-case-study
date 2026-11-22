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

        public static WheelSlice Bomb { get { return new WheelSlice(true, default(RewardGrant)); } }
    }

    /// <summary>Immutable slice layout for one zone.</summary>
    public sealed class ZoneWheel
    {
        #region Slices

        private readonly WheelSlice[] _slices;

        public ZoneWheel(WheelSlice[] slices)
        {
            _slices = slices ?? throw new ArgumentNullException(nameof(slices));
        }

        public int SliceCount { get { return _slices.Length; } }

        public WheelSlice this[int index] { get { return _slices[index]; } }

        public int BombCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _slices.Length; i++)
                {
                    if (_slices[i].IsBomb)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        #endregion
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
        #region Building

        private const int NO_BOMB = -1;
        private const int MINIMUM_SLICES = 1;
        private const int FIRST_SLICE = 0;

        public ZoneWheel Build(int sliceCount, ZoneKind kind, IReadOnlyList<RewardPoolEntry> rewardPool, IRandomSource random)
        {
            if (sliceCount < MINIMUM_SLICES)
            {
                throw new ArgumentOutOfRangeException(nameof(sliceCount), "A wheel needs at least one slice.");
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }
            if (rewardPool == null || rewardPool.Count == 0)
            {
                throw new ArgumentException("A wheel needs a nonempty reward pool.", nameof(rewardPool));
            }
            double totalWeight = 0d;
            for (int i = 0; i < rewardPool.Count; i++)
            {
                var entry = rewardPool[i];
                if (string.IsNullOrWhiteSpace(entry.RewardId) || entry.Amount <= 0 ||
                    double.IsNaN(entry.Weight) || double.IsInfinity(entry.Weight) || entry.Weight < 0d)
                {
                    throw new ArgumentException("Reward entries require an ID, positive amount and finite nonnegative weight.", nameof(rewardPool));
                }
                totalWeight += entry.Weight;
            }
            if (totalWeight <= 0d || double.IsInfinity(totalWeight))
            {
                throw new ArgumentException("Reward pool total weight must be positive and finite.", nameof(rewardPool));
            }

            var slices = new WheelSlice[sliceCount];

            // The bomb slot is chosen in code for every normal wheel; authored data never places it.
            int bombIndex = kind.IsBombFree ? NO_BOMB : random.NextInt(FIRST_SLICE, sliceCount);

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
            double total = 0d;
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Weight > 0d)
                {
                    total += pool[i].Weight;
                }
            }

            double roll = random.NextDouble() * total;
            double accumulated = 0d;
            for (int i = 0; i < pool.Count; i++)
            {
                accumulated += pool[i].Weight;
                if (pool[i].Weight > 0d && roll < accumulated)
                {
                    return new RewardGrant(pool[i].RewardId, pool[i].Amount);
                }
            }

            RewardPoolEntry last = pool[pool.Count - 1];
            return new RewardGrant(last.RewardId, last.Amount);
        }

        #endregion
    }
}
