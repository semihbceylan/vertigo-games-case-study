using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>
    /// Top-level, editor-configurable entry point that ties zone progression and wheel tiers together.
    /// Runtime state is never stored here.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Game Configuration", fileName = "GameConfig")]
    public sealed class GameConfiguration : ScriptableObject
    {
        [SerializeField] private ZoneConfiguration zones;
        [SerializeField] private bool deterministicRandom;
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private CurrencyDefinition _reviveCurrency;
        [SerializeField, Min(1)] private int _reviveBaseCost = 100;
        [SerializeField, Min(1)] private int _reviveCostPerZone = 25;
        [SerializeField, Min(1)] private int _maxRevivesPerRun = Vertigo.WheelOfFortune.Core.RevivePolicy.DEFAULT_MAX_REVIVES;

        public Vertigo.WheelOfFortune.Core.RevivePolicy CreateRevivePolicy()
        {
            if (!_reviveCurrency) throw new System.InvalidOperationException("Revive currency is required.");
            return new Vertigo.WheelOfFortune.Core.RevivePolicy(_reviveCurrency.CurrencyId, _reviveBaseCost, _reviveCostPerZone, _maxRevivesPerRun);
        }

        public ZoneConfiguration Zones { get { return zones; } }
        public bool DeterministicRandom { get { return deterministicRandom; } }
        public int RandomSeed { get { return randomSeed; } }
    }
}
