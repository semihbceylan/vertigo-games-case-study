using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    public enum WheelTier
    {
        Bronze,
        Silver,
        Golden
    }

    /// <summary>Explicit content authored for a single wheel slice.</summary>
    [System.Serializable]
    public struct WheelSliceDefinition
    {
        [Tooltip("Tick to make this slice the bomb (ignored on safe/super wheels, which never contain bombs).")]
        public bool isBomb;
        [Tooltip("Reward granted when this slice is hit.")]
        public RewardDefinition reward;
        [Tooltip("Amount for this slice. 0 uses the reward's own default amount.")]
        [Min(0)] public long amount;
    }

    /// <summary>
    /// Editor-configurable wheel art + slice configuration. Layout (radius / start angle /
    /// icon scale) is authored on the scene's WheelLayout, while slice count and rewards live here.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Wheel", fileName = "Wheel_")]
    public sealed class WheelDefinition : ScriptableObject
    {
        [SerializeField] private WheelTier tier = WheelTier.Bronze;

        [Header("Art")]
        [SerializeField] private Sprite baseSprite;
        [SerializeField] private Sprite indicatorSprite;
        [SerializeField] private Sprite bombSprite;

        [Header("Slices")]
        [SerializeField, Min(2)] private int sliceCount = 8;
        [Tooltip("Weighted pool used to fill non-bomb slices when 'Use Explicit Slices' is off. " +
                 "Use the pool for randomised wheels (e.g. the dynamic zone-filtered super wheel).")]
        [SerializeField] private RewardPool rewardPool;

        [Tooltip("When enabled, the wheel is built from the explicit slice list below instead of random weighted pool draws.")]
        [SerializeField] private bool useExplicitSlices = false;
        [Tooltip("Exact slice-by-slice contents. Only used when 'Use Explicit Slices' is enabled.")]
        [SerializeField] private WheelSliceDefinition[] slices = new WheelSliceDefinition[0];

        [Tooltip("Reward amounts grow by this fraction for every zone above the first.")]
        [SerializeField, Min(0f)] private float amountScalePerZone = 0.1f;

        public WheelTier Tier => tier;
        public Sprite BaseSprite => baseSprite;
        public Sprite IndicatorSprite => indicatorSprite;
        public Sprite BombSprite => bombSprite;
        public int SliceCount => sliceCount;
        public RewardPool RewardPool => rewardPool;
        public bool UseExplicitSlices => useExplicitSlices;
        public WheelSliceDefinition[] Slices => slices;
        public float AmountScalePerZone => amountScalePerZone;
    }
}
