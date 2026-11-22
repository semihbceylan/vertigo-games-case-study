using System;
using System.Collections.Generic;
using UnityEngine;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>Authored run bounds and the ordered zone rule assets.</summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Zone Configuration", fileName = "Zones_")]
    public sealed class ZoneConfiguration : ScriptableObject
    {
        #region Serialized Values

        [SerializeField, Min(1)] private int _startingZone = 1;
        [Tooltip("The run completes after this zone. Zero means endless.")]
        [SerializeField, Min(0)] private int _finalZone = 60;
        [SerializeField] private ZoneRuleDefinition[] _zoneRules = new ZoneRuleDefinition[0];

        #endregion

        #region Accessors

        public int StartingZone
        {
            get { return _startingZone; }
        }

        public int FinalZone
        {
            get { return _finalZone; }
        }

        public IReadOnlyList<ZoneRuleDefinition> RuleDefinitions
        {
            get { return _zoneRules; }
        }

        public IEnumerable<WheelDefinition> AllWheels
        {
            get
            {
                foreach (ZoneRuleDefinition rule in _zoneRules)
                {
                    yield return rule.Wheel;
                }
            }
        }

        /// <summary>Adapts the assets into the Unity-independent gameplay rules.</summary>
        public ZoneRules ToRules()
        {
            var rules = new ZoneRule[_zoneRules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                if (!_zoneRules[i])
                {
                    throw new InvalidOperationException("Missing zone rule asset.");
                }
                rules[i] = _zoneRules[i].ToRule();
            }
            return new ZoneRules(rules);
        }

        public WheelDefinition GetWheel(ZoneKind kind)
        {
            foreach (ZoneRuleDefinition rule in _zoneRules)
            {
                if (rule.Id == kind.Id)
                {
                    return rule.Wheel;
                }
            }
            throw new InvalidOperationException("Zone rule identity is not configured: " + kind.Id);
        }

        #endregion

#if UNITY_EDITOR
        #region Validation

        private void OnValidate()
        {
            if (_finalZone != ZoneRules.ENDLESS_FINAL_ZONE && _finalZone < _startingZone)
            {
                Debug.LogError("Final zone must not be below the starting zone.", this);
            }
            foreach (ZoneRuleDefinition rule in _zoneRules)
            {
                if (!rule || !rule.Wheel)
                {
                    Debug.LogError("Every zone rule entry needs a rule asset with a wheel.", this);
                }
            }
        }

        #endregion
#endif
    }
}
