using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>
    /// Editor-configurable wheel art + slice configuration. Layout (radius / start angle /
    /// icon scale) is authored on the scene's WheelLayout, while slice count and rewards live here.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Wheel", fileName = "Wheel_")]
    public sealed class WheelDefinition : ScriptableObject
    {
        [Header("Art")]
        [SerializeField] private Sprite baseSprite;
        [SerializeField] private Sprite indicatorSprite;
        [SerializeField] private Sprite bombSprite;

        [Header("Slices")]
        [SerializeField, Min(2)] private int sliceCount = 8;
        [Tooltip("Weighted pool used to fill non-bomb slices. Bomb placement is controlled by the builder.")]
        [SerializeField] private RewardPool rewardPool;

        [Tooltip("Reward amounts grow by this fraction for every zone above the first.")]
        [SerializeField, Min(0f)] private float amountScalePerZone = 0.1f;

        public Sprite BaseSprite => baseSprite;
        public Sprite IndicatorSprite => indicatorSprite;
        public Sprite BombSprite => bombSprite;
        public int SliceCount => sliceCount;
        public RewardPool RewardPool => rewardPool;
        public float AmountScalePerZone => amountScalePerZone;
    }
}
