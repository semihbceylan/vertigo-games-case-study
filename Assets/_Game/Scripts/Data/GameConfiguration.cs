using System;
using System.Collections.Generic;
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

        [Header("Economy")]
        [Tooltip("Every persistent currency; each one's starting balance is granted to new players.")]
        [SerializeField] private CurrencyDefinition[] _currencies = new CurrencyDefinition[0];

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

        public CurrencyDefinition ReviveCurrency
        {
            get { return _reviveCurrency; }
        }

        /// <summary>Starting balances keyed by currency ID, for the persistent currency wallet.</summary>
        public Dictionary<string, long> CreateStartingBalances()
        {
            var balances = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (CurrencyDefinition currency in _currencies)
            {
                if (!currency)
                {
                    throw new InvalidOperationException("Currency list contains an empty entry.");
                }
                balances.Add(currency.CurrencyId, currency.StartingBalance);
            }
            return balances;
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
            if (Array.IndexOf(_currencies, _reviveCurrency) < 0)
            {
                Debug.LogError("The revive currency must be listed in the game's currencies.", this);
            }
        }

        #endregion
#endif
    }
}
