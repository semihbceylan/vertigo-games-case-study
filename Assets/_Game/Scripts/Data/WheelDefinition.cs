using System;
using UnityEngine;
using UnityEngine.Serialization;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>A reward pool used for a contiguous band of zones.</summary>
    [Serializable]
    public sealed class RewardPoolBand
    {
        [SerializeField, Min(1)] private int _minimumZone = 1;
        [Tooltip("Zero means the band never ends.")]
        [SerializeField, Min(0)] private int _maximumZone;
        [SerializeField] private RewardPool _pool;

        public RewardPool Pool
        {
            get { return _pool; }
        }

        public bool Matches(int zone)
        {
            return zone >= _minimumZone && (_maximumZone == ZoneRule.UNBOUNDED_MAXIMUM_ZONE || zone <= _maximumZone);
        }
    }

    /// <summary>
    /// Wheel tier art and slice content. Bomb placement is decided in code by the wheel builder,
    /// never by authored slice flags. Layout lives on the scene's WheelLayout.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Wheel", fileName = "Wheel_")]
    public sealed class WheelDefinition : ScriptableObject
    {
        #region Serialized Values

        [Header("Art")]
        [SerializeField, FormerlySerializedAs("baseSprite")] private Sprite _baseSprite;
        [SerializeField, FormerlySerializedAs("indicatorSprite")] private Sprite _indicatorSprite;
        [SerializeField, FormerlySerializedAs("bombSprite")] private Sprite _bombSprite;
        [Tooltip("Bomb icon size relative to the slot.")]
        [SerializeField, Min(0.01f)] private float _bombIconScale = 1.5f;

        [Header("Slices")]
        [SerializeField, Min(2), FormerlySerializedAs("sliceCount")] private int _sliceCount = 8;
        [Tooltip("Weighted pool used when no progression bands are configured.")]
        [SerializeField, FormerlySerializedAs("rewardPool")] private RewardPool _rewardPool;
        [Tooltip("Optional progression bands. Exactly one band must match each playable zone when configured.")]
        [SerializeField] private RewardPoolBand[] _rewardPoolBands = new RewardPoolBand[0];
        [Tooltip("Reward amounts grow by this fraction for every zone above the first.")]
        [SerializeField, Min(0f), FormerlySerializedAs("amountScalePerZone")] private float _amountScalePerZone = 0.1f;

        #endregion

        #region Accessors

        public Sprite BaseSprite
        {
            get { return _baseSprite; }
        }

        public Sprite IndicatorSprite
        {
            get { return _indicatorSprite; }
        }

        public Sprite BombSprite
        {
            get { return _bombSprite; }
        }

        public float BombIconScale
        {
            get { return _bombIconScale; }
        }

        public int SliceCount
        {
            get { return _sliceCount; }
        }

        public RewardPool RewardPool
        {
            get { return _rewardPool; }
        }

        public float AmountScalePerZone
        {
            get { return _amountScalePerZone; }
        }

        public RewardPoolBand[] RewardPoolBands
        {
            get { return _rewardPoolBands; }
        }

        public RewardPool GetRewardPool(int zone)
        {
            if (_rewardPoolBands.Length == 0)
            {
                return _rewardPool;
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
                    throw new InvalidOperationException("Reward progression bands overlap.");
                }
                hasMatch = true;
                selected = _rewardPoolBands[i].Pool;
            }
            if (!selected)
            {
                throw new InvalidOperationException("No reward progression pool is configured for this zone.");
            }
            return selected;
        }

        #endregion

#if UNITY_EDITOR
        #region Validation

        private void OnValidate()
        {
            if (!_baseSprite || !_indicatorSprite || !_bombSprite)
            {
                Debug.LogError("Wheel requires base, indicator and bomb sprites: " + name, this);
            }
            if (!_rewardPool && _rewardPoolBands.Length == 0)
            {
                Debug.LogError("Wheel requires a reward pool or progression bands: " + name, this);
            }
        }

        #endregion
#endif
    }
}
