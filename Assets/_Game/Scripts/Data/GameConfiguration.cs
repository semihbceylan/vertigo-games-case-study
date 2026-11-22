using System;
using UnityEngine;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>
    /// Top-level, editor-configurable entry point that ties zone progression, randomness and the
    /// revive economy together. Runtime state is never stored here.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Game Configuration", fileName = "GameConfig")]
    public sealed class GameConfiguration : ScriptableObject
    {
        #region Serialized Values

        [SerializeField] private ZoneConfiguration _zones;
        [Tooltip("Use a fixed seed so runs (and the editor preview) are reproducible.")]
        [SerializeField] private bool _isDeterministicRandom;
        [SerializeField] private int _randomSeed = 12345;

        [Header("Revive")]
        [SerializeField] private CurrencyDefinition _reviveCurrency;
        [SerializeField, Min(1)] private int _reviveBaseCost = 100;
        [SerializeField, Min(1)] private int _reviveCostPerZone = 25;
        [SerializeField, Min(1)] private int _maxRevivesPerRun = 4;

        #endregion

        #region Accessors

        public ZoneConfiguration Zones
        {
            get { return _zones; }
        }

        public bool IsDeterministicRandom
        {
            get { return _isDeterministicRandom; }
        }

        public int RandomSeed
        {
            get { return _randomSeed; }
        }

        public RevivePolicy CreateRevivePolicy()
        {
            if (!_reviveCurrency)
            {
                throw new InvalidOperationException("Revive currency is required.");
            }
            return new RevivePolicy(_reviveCurrency.CurrencyId, _reviveBaseCost, _reviveCostPerZone, _maxRevivesPerRun);
        }

        #endregion

#if UNITY_EDITOR
        #region Validation

        private void OnValidate()
        {
            if (!_zones || !_reviveCurrency)
            {
                Debug.LogError("Game configuration requires zones and a revive currency.", this);
            }
        }

        #endregion
#endif
    }
}
