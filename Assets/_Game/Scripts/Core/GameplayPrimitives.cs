using System;
using System.Collections.Generic;

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

    /// <summary>One configured zone policy: which zones it matches and how strongly.</summary>
    public sealed class ZoneRule
    {
        /// <summary>A maximum zone of zero means the rule never stops matching.</summary>
        public const int UNBOUNDED_MAXIMUM_ZONE = 0;

        public ZoneKind Kind { get; }
        public int Interval { get; }
        public int MinimumZone { get; }
        public int MaximumZone { get; }
        public int Priority { get; }

        public ZoneRule(ZoneKind kind, int interval, int minimumZone, int maximumZone, int priority)
        {
            Kind = kind ?? throw new ArgumentNullException(nameof(kind));
            if (interval < 1 || minimumZone < 1 || (maximumZone != UNBOUNDED_MAXIMUM_ZONE && maximumZone < minimumZone))
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "Invalid zone rule bounds.");
            }
            Interval = interval;
            MinimumZone = minimumZone;
            MaximumZone = maximumZone;
            Priority = priority;
        }

        public bool IsUnbounded
        {
            get { return MaximumZone == UNBOUNDED_MAXIMUM_ZONE; }
        }

        public bool Matches(int zone)
        {
            return zone >= MinimumZone && (IsUnbounded || zone <= MaximumZone) && zone % Interval == 0;
        }

        /// <summary>True when <paramref name="other"/> wins every zone this rule could match.</summary>
        public bool IsShadowedBy(ZoneRule other)
        {
            return other.Priority > Priority && Interval % other.Interval == 0 &&
                other.MinimumZone <= MinimumZone &&
                (other.IsUnbounded || (!IsUnbounded && other.MaximumZone >= MaximumZone));
        }
    }

    /// <summary>Immutable rule snapshot shared by gameplay and presentation resolution.</summary>
    public sealed class ZoneRules
    {
        /// <summary>Returned by <see cref="FindNextZone"/> when the rule never matches again.</summary>
        public const int NO_ZONE = 0;
        /// <summary>A final zone of zero means the run is endless.</summary>
        public const int ENDLESS_FINAL_ZONE = 0;
        private const int FIRST_ZONE = 1;

        private readonly ZoneRule[] _rules;

        public ZoneRules(params ZoneRule[] rules)
        {
            if (rules == null || rules.Length == 0)
            {
                throw new ArgumentException("Zone rules are required.", nameof(rules));
            }
            _rules = (ZoneRule[])rules.Clone();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (ZoneRule rule in _rules)
            {
                if (rule == null || !ids.Add(rule.Kind.Id))
                {
                    throw new ArgumentException("Zone rule identities must be unique.", nameof(rules));
                }
            }
        }

        public ZoneKind Resolve(int zone)
        {
            return ResolveRule(zone).Kind;
        }

        /// <summary>
        /// The first zone after <paramref name="currentZone"/> resolved to <paramref name="kindId"/>,
        /// or <see cref="NO_ZONE"/> when it never wins again before <paramref name="finalZone"/>.
        /// </summary>
        public int FindNextZone(int currentZone, string kindId, int finalZone)
        {
            ZoneRule match = FindRule(kindId);
            foreach (ZoneRule other in _rules)
            {
                if (match.IsShadowedBy(other))
                {
                    return NO_ZONE;
                }
            }

            long limit = finalZone > ENDLESS_FINAL_ZONE ? finalZone : int.MaxValue;
            if (!match.IsUnbounded)
            {
                limit = Math.Min(limit, match.MaximumZone);
            }
            long firstMatch = ((long)match.MinimumZone + match.Interval - 1) / match.Interval * match.Interval;
            long candidate = Math.Max(((long)currentZone / match.Interval + 1) * match.Interval, firstMatch);
            for (; candidate <= limit; candidate += match.Interval)
            {
                if (ResolveRule((int)candidate) == match)
                {
                    return (int)candidate;
                }
            }
            return NO_ZONE;
        }

        private ZoneRule FindRule(string kindId)
        {
            foreach (ZoneRule rule in _rules)
            {
                if (string.Equals(rule.Kind.Id, kindId, StringComparison.Ordinal))
                {
                    return rule;
                }
            }
            throw new ArgumentException("Zone rule identity is not configured: " + kindId, nameof(kindId));
        }

        private ZoneRule ResolveRule(int zone)
        {
            if (zone < FIRST_ZONE)
            {
                throw new ArgumentOutOfRangeException(nameof(zone));
            }
            ZoneRule selected = null;
            foreach (ZoneRule rule in _rules)
            {
                if (rule.Matches(zone) && (selected == null || rule.Priority > selected.Priority))
                {
                    selected = rule;
                }
            }
            if (selected == null)
            {
                throw new InvalidOperationException("No zone rule matches zone " + zone);
            }
            foreach (ZoneRule rule in _rules)
            {
                if (rule != selected && rule.Priority == selected.Priority && rule.Matches(zone))
                {
                    throw new InvalidOperationException("Zone rules have ambiguous priorities at zone " + zone);
                }
            }
            return selected;
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

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
