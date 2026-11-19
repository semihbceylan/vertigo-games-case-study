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

        public ZoneConfiguration Zones { get { return zones; } }
        public bool DeterministicRandom { get { return deterministicRandom; } }
        public int RandomSeed { get { return randomSeed; } }
    }
}
