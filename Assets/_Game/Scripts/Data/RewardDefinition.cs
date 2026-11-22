using UnityEngine;
using UnityEngine.Serialization;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>
    /// Editor-configurable description of a single reward that can appear on a wheel slice.
    /// The ID identifies content only; behaviour comes from the explicit fields below.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Reward", fileName = "Reward_")]
    public sealed class RewardDefinition : ScriptableObject
    {
        #region Serialized Values

        private const int NO_MAXIMUM_ZONE = 0;

        [SerializeField, FormerlySerializedAs("rewardId")] private string _rewardId = "reward";
        [SerializeField, FormerlySerializedAs("displayName")] private string _displayName = "Reward";
        [SerializeField, FormerlySerializedAs("icon")] private Sprite _icon;
        [SerializeField, Min(0), FormerlySerializedAs("amount")] private long _amount = 1;
        [Tooltip("Display grouping; controls where this reward sorts in the rewards list and results summary.")]
        [SerializeField] private RewardCategoryDefinition _category;

        [Header("Jackpot showcase")]
        [Tooltip("When enabled, this reward pops up fullscreen until the player taps to dismiss.")]
        [SerializeField, FormerlySerializedAs("showcaseBig")] private bool _isShowcased;

        [Header("Progression and presentation")]
        [SerializeField, Min(1), FormerlySerializedAs("minimumZone")] private int _minimumZone = 1;
        [Tooltip("Zero means no upper limit.")]
        [SerializeField, Min(0), FormerlySerializedAs("maximumZone")] private int _maximumZone;
        [SerializeField, FormerlySerializedAs("isAmountScaled")] private bool _isAmountScaled = true;
        [SerializeField, Min(0.01f), FormerlySerializedAs("iconScaleMultiplier")] private float _iconScaleMultiplier = 1f;
        [Tooltip("Optional persistent currency credited when this reward is successfully banked.")]
        [SerializeField] private CurrencyDefinition _currency;

        #endregion

        #region Accessors

        public string RewardId
        {
            get { return _rewardId; }
        }

        public string DisplayName
        {
            get { return _displayName; }
        }

        public Sprite Icon
        {
            get { return _icon; }
        }

        public long Amount
        {
            get { return _amount; }
        }

        public RewardCategoryDefinition Category
        {
            get { return _category; }
        }

        public bool ShowcaseBig
        {
            get { return _isShowcased; }
        }

        public bool IsAmountScaled
        {
            get { return _isAmountScaled; }
        }

        public float IconScaleMultiplier
        {
            get { return _iconScaleMultiplier; }
        }

        public CurrencyDefinition Currency
        {
            get { return _currency; }
        }

        public bool IsAvailableAt(int zone)
        {
            return zone >= _minimumZone && (_maximumZone == NO_MAXIMUM_ZONE || zone <= _maximumZone);
        }

        #endregion

#if UNITY_EDITOR
        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_rewardId) || string.IsNullOrWhiteSpace(_displayName) || !_icon || !_category)
            {
                Debug.LogError("Reward requires an ID, display name, icon and category: " + name, this);
            }
            if (_maximumZone != NO_MAXIMUM_ZONE && _maximumZone < _minimumZone)
            {
                Debug.LogError("Reward maximum zone must not be below its minimum zone: " + name, this);
            }
        }

        #endregion
#endif
    }
}
