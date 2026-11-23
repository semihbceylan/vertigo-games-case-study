using UnityEngine;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>
    /// One zone type: which zones it matches, its priority, wheel, bomb and leave policy, and the
    /// zone-strip visuals. Gameplay and UI both resolve zones through these assets.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Zone Rule", fileName = "ZoneRule_")]
    public sealed class ZoneRuleDefinition : ScriptableObject
    {
        #region Serialized Values

        [SerializeField] private string _id;
        [SerializeField, Min(1)] private int _interval = 1;
        [SerializeField, Min(1)] private int _minimumZone = 1;
        [Tooltip("Zero means the rule never stops matching.")]
        [SerializeField, Min(0)] private int _maximumZone;
        [Tooltip("Higher priority wins when several rules match the same zone.")]
        [SerializeField] private int _priority;
        [SerializeField] private bool _isBombFree;
        [SerializeField] private bool _isLeaveAllowed;
        [SerializeField] private WheelDefinition _wheel;
        [SerializeField] private Sprite _currentBackground;
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _currentTextColor = Color.black;
        [Tooltip("Optional icon on this zone type's map card, e.g. its headline reward.")]
        [SerializeField] private Sprite _mapIcon;

        #endregion

        #region Accessors

        public string Id
        {
            get { return _id; }
        }

        public WheelDefinition Wheel
        {
            get { return _wheel; }
        }

        public Sprite CurrentBackground
        {
            get { return _currentBackground; }
        }

        public Color TextColor
        {
            get { return _textColor; }
        }

        public Color CurrentTextColor
        {
            get { return _currentTextColor; }
        }

        public Sprite MapIcon
        {
            get { return _mapIcon; }
        }

        public ZoneRule ToRule()
        {
            return new ZoneRule(new ZoneKind(_id, _isBombFree, _isLeaveAllowed),
                _interval, _minimumZone, _maximumZone, _priority);
        }

        #endregion

#if UNITY_EDITOR
        #region Validation

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id) || !_wheel || !_currentBackground)
            {
                Debug.LogError("Zone rule requires an ID, wheel and current-zone background: " + name, this);
            }
            if (_maximumZone != ZoneRule.UNBOUNDED_MAXIMUM_ZONE && _maximumZone < _minimumZone)
            {
                Debug.LogError("Zone rule maximum zone must not be below its minimum zone: " + name, this);
            }
        }

        #endregion
#endif
    }
}
