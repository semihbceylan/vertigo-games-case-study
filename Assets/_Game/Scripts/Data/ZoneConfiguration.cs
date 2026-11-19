using System;
using System.Collections.Generic;
using UnityEngine;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Data
{
    [CreateAssetMenu(menuName = "Wheel of Fortune/Zone Configuration", fileName = "Zones_")]
    public sealed class ZoneConfiguration : ScriptableObject
    {
        [SerializeField, Min(1)] private int startingZone = 1;
        [SerializeField, Min(0)] private int finalZone = 60;
        [SerializeField] private ZoneRuleDefinition[] _zoneRules = new ZoneRuleDefinition[0];

        public int StartingZone { get { return startingZone; } }
        public int FinalZone { get { return finalZone; } }

        public ZoneRules ToRules()
        {
            var rules = new ZoneRule[_zoneRules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                if (!_zoneRules[i]) throw new InvalidOperationException("Missing zone rule asset.");
                rules[i] = _zoneRules[i].ToRule();
            }
            return new ZoneRules(rules);
        }

        public ZoneRuleDefinition Resolve(int zone)
        {
            if (zone < 1) throw new ArgumentOutOfRangeException(nameof(zone));
            ZoneRuleDefinition selected = null;
            foreach (ZoneRuleDefinition rule in _zoneRules)
            {
                if (!rule) throw new InvalidOperationException("Missing zone rule asset.");
                if (!rule.Matches(zone)) continue;
                if (!selected || rule.Priority > selected.Priority) selected = rule;
            }
            if (!selected) throw new InvalidOperationException("No zone rule matches zone " + zone);
            foreach (ZoneRuleDefinition rule in _zoneRules)
            {
                if (rule != selected && rule.Priority == selected.Priority && rule.Matches(zone))
                    throw new InvalidOperationException("Ambiguous zone rules at zone " + zone);
            }
            return selected;
        }

        public ZoneRuleDefinition GetDefinition(ZoneKind kind)
        {
            foreach (ZoneRuleDefinition rule in _zoneRules)
            {
                if (rule.Id == kind.Id) return rule;
            }
            throw new InvalidOperationException("Zone rule identity is not configured: " + kind.Id);
        }

        public WheelDefinition GetWheelForZone(int zone) { return Resolve(zone).Wheel; }
        public WheelDefinition GetWheel(ZoneKind kind) { return GetDefinition(kind).Wheel; }

        public int FindNextZone(int currentZone, ZoneRuleDefinition rule)
        {
            ZoneRule match = rule.ToRule();
            foreach (ZoneRuleDefinition other in _zoneRules)
            {
                ZoneRule blocker = other.ToRule();
                if (blocker.Priority > match.Priority && match.Interval % blocker.Interval == 0 &&
                    blocker.MinimumZone <= match.MinimumZone &&
                    (blocker.MaximumZone == 0 || (match.MaximumZone > 0 && blocker.MaximumZone >= match.MaximumZone)))
                {
                    return 0;
                }
            }
            long candidate = ((long)currentZone / match.Interval + 1) * match.Interval;
            long limit = finalZone > 0 ? finalZone : int.MaxValue;
            if (match.MaximumZone > 0) limit = Math.Min(limit, match.MaximumZone);
            candidate = Math.Max(candidate, ((long)match.MinimumZone + match.Interval - 1) / match.Interval * match.Interval);
            for (; candidate <= limit; candidate += match.Interval)
            {
                if (Resolve((int)candidate) == rule) return (int)candidate;
            }
            return 0;
        }

        public IEnumerable<WheelDefinition> AllWheels
        {
            get
            {
                foreach (ZoneRuleDefinition rule in _zoneRules)
                {
                    if (rule && rule.Wheel) yield return rule.Wheel;
                }
            }
        }

    }
}
