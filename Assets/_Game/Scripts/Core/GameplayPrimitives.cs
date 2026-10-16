using System;

namespace Vertigo.WheelOfFortune.Core
{
    /// <summary>Classification of a zone based on progression rules.</summary>
    public enum ZoneKind
    {
        Normal = 0,
        Safe = 1,
        Super = 2
    }

    /// <summary>A single reward that has been collected.</summary>
    public readonly struct RewardGrant
    {
        public readonly string RewardId;
        public readonly long Amount;

        public RewardGrant(string rewardId, long amount)
        {
            RewardId = rewardId ?? string.Empty;
            Amount = amount < 0 ? 0 : amount;
        }
    }

    /// <summary>Rules describing how often Safe and Super zones occur.</summary>
    public readonly struct ZoneRules
    {
        public readonly int SafeInterval;
        public readonly int SuperInterval;

        public ZoneRules(int safeInterval, int superInterval)
        {
            SafeInterval = safeInterval < 1 ? 1 : safeInterval;
            SuperInterval = superInterval < 1 ? 1 : superInterval;
        }
    }

    /// <summary>Injected randomness so gameplay logic stays deterministic and testable.</summary>
    public interface IRandomSource
    {
        int NextInt(int minInclusive, int maxExclusive);
        double NextDouble();
    }

    /// <summary>Default <see cref="IRandomSource"/> backed by <see cref="System.Random"/>.</summary>
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource() : this(Environment.TickCount) { }

        public SystemRandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return _random.Next(minInclusive, maxExclusive);
        }

        public double NextDouble() => _random.NextDouble();
    }
}
