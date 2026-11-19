using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    [System.Serializable]
    public sealed class RewardPoolBand
    {
        [SerializeField, Min(1)] private int _minimumZone = 1;
        [SerializeField, Min(0)] private int _maximumZone;
        [SerializeField] private RewardPool _pool;

        public RewardPool Pool
        {
            get { return _pool; }
        }

        public bool Matches(int zone)
        {
            return zone >= _minimumZone && (_maximumZone == 0 || zone <= _maximumZone);
        }
    }

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

        [Tooltip("Optional progression bands. Exactly one band must match each playable zone when configured.")]
        [SerializeField] private RewardPoolBand[] _rewardPoolBands = new RewardPoolBand[0];

        [Tooltip("Reward amounts grow by this fraction for every zone above the first.")]
        [SerializeField, Min(0f)] private float amountScalePerZone = 0.1f;

        public Sprite BaseSprite => baseSprite;
        public Sprite IndicatorSprite => indicatorSprite;
        public Sprite BombSprite => bombSprite;
        public int SliceCount => sliceCount;
        public RewardPool RewardPool => rewardPool;
        public float AmountScalePerZone => amountScalePerZone;

        public RewardPoolBand[] RewardPoolBands
        {
            get { return _rewardPoolBands; }
        }

        public RewardPool GetRewardPool(int zone)
        {
            if (_rewardPoolBands.Length == 0)
            {
                return rewardPool;
            }

            RewardPool selected = null;
            bool hasMatch = false;
            for (int i = 0; i < _rewardPoolBands.Length; i++)
            {
                if (!_rewardPoolBands[i].Matches(zone))
                {
                    continue;
                }
                if (hasMatch)
                {
                    throw new System.InvalidOperationException("Reward progression bands overlap.");
                }
                hasMatch = true;
                selected = _rewardPoolBands[i].Pool;
            }
            if (!selected)
            {
                throw new System.InvalidOperationException("No reward progression pool is configured for this zone.");
            }
            return selected;
        }
    }
}
