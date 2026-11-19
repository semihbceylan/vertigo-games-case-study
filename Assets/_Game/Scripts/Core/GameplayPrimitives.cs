using System;

namespace Vertigo.WheelOfFortune.Core
{
    /// <summary>Data-defined zone identity and gameplay policy; not a fixed enumeration.</summary>
    public sealed class ZoneKind
    {
        public string Id { get; }
        public bool IsBombFree { get; }
        public bool IsLeaveAllowed { get; }

        public ZoneKind(string id, bool isBombFree, bool isLeaveAllowed)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Zone identity is required.", nameof(id));
            Id = id;
            IsBombFree = isBombFree;
            IsLeaveAllowed = isLeaveAllowed;
        }
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

    public sealed class ZoneRule
    {
        public ZoneKind Kind { get; }
        public int Interval { get; }
        public int MinimumZone { get; }
        public int MaximumZone { get; }
        public int Priority { get; }

        public ZoneRule(ZoneKind kind, int interval, int minimumZone, int maximumZone, int priority)
        {
            Kind = kind ?? throw new ArgumentNullException(nameof(kind));
            if (interval < 1 || minimumZone < 1 || (maximumZone != 0 && maximumZone < minimumZone))
                throw new ArgumentOutOfRangeException(nameof(interval), "Invalid zone rule bounds.");
            Interval = interval;
            MinimumZone = minimumZone;
            MaximumZone = maximumZone;
            Priority = priority;
        }

        public bool Matches(int zone)
        {
            return zone >= MinimumZone && (MaximumZone == 0 || zone <= MaximumZone) && zone % Interval == 0;
        }
    }

    /// <summary>Immutable rule snapshot shared by gameplay and content resolution.</summary>
    public sealed class ZoneRules
    {
        private readonly ZoneRule[] _rules;

        public ZoneRules(params ZoneRule[] rules)
        {
            if (rules == null || rules.Length == 0) throw new ArgumentException("Zone rules are required.", nameof(rules));
            _rules = (ZoneRule[])rules.Clone();
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (ZoneRule rule in _rules)
            {
                if (rule == null || !ids.Add(rule.Kind.Id)) throw new ArgumentException("Zone rule identities must be unique.", nameof(rules));
            }
        }

        public ZoneKind Resolve(int zone)
        {
            if (zone < 1) throw new ArgumentOutOfRangeException(nameof(zone));
            ZoneRule selected = null;
            foreach (ZoneRule rule in _rules)
            {
                if (!rule.Matches(zone)) continue;
                if (selected == null || rule.Priority > selected.Priority) selected = rule;
            }
            if (selected == null) throw new InvalidOperationException("No zone rule matches zone " + zone);
            foreach (ZoneRule rule in _rules)
            {
                if (rule != selected && rule.Priority == selected.Priority && rule.Matches(zone))
                    throw new InvalidOperationException("Zone rules have ambiguous priorities at zone " + zone);
            }
            return selected.Kind;
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
