using UnityEngine;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Data
{
    [CreateAssetMenu(menuName = "Wheel of Fortune/Zone Rule", fileName = "ZoneRule_")]
    public sealed class ZoneRuleDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField, Min(1)] private int _interval = 1;
        [SerializeField, Min(1)] private int _minimumZone = 1;
        [SerializeField, Min(0)] private int _maximumZone;
        [SerializeField] private int _priority;
        [SerializeField] private bool _isBombFree;
        [SerializeField] private bool _isLeaveAllowed;
        [SerializeField] private WheelDefinition _wheel;
        [SerializeField] private Sprite _currentBackground;
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _currentTextColor = Color.black;

        public string Id { get { return _id; } }
        public int Priority { get { return _priority; } }
        public WheelDefinition Wheel { get { return _wheel; } }
        public Sprite CurrentBackground { get { return _currentBackground; } }
        public Color TextColor { get { return _textColor; } }
        public Color CurrentTextColor { get { return _currentTextColor; } }

        public ZoneRule ToRule()
        {
            return new ZoneRule(new ZoneKind(_id, _isBombFree, _isLeaveAllowed),
                _interval, _minimumZone, _maximumZone, _priority);
        }

        public bool Matches(int zone)
        {
            return _interval > 0 && zone >= _minimumZone &&
                (_maximumZone == 0 || zone <= _maximumZone) && zone % _interval == 0;
        }
    }
}
