using DG.Tweening;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    [CreateAssetMenu(menuName = "Wheel of Fortune/Animation/Reward List", fileName = "Animation_RewardList")]
    public sealed class RewardListAnimation : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _shiftDuration = 0.3f;
        [SerializeField] private Ease _shiftEase = Ease.OutCubic;
        [SerializeField, Min(0f)] private float _appearDuration = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _appearScale = 0.94f;
        [SerializeField] private Ease _appearEase = Ease.OutCubic;
        [SerializeField, Range(0f, 1f)] private float _hiddenAlpha;
        [SerializeField, Range(0f, 1f)] private float _visibleAlpha = 1f;
        [SerializeField, Min(0f)] private float _punchScale = 0.2f;
        [SerializeField, Min(0.01f)] private float _punchDuration = 0.3f;
        [SerializeField, Min(1)] private int _punchVibrato = 8;
        [SerializeField, Range(0f, 1f)] private float _punchElasticity = 0.6f;

        public float ShiftDuration { get { return _shiftDuration; } }
        public Ease ShiftEase { get { return _shiftEase; } }
        public float AppearDuration { get { return _appearDuration; } }
        public float AppearScale { get { return _appearScale; } }
        public Ease AppearEase { get { return _appearEase; } }
        public float HiddenAlpha { get { return _hiddenAlpha; } }
        public float VisibleAlpha { get { return _visibleAlpha; } }
        public float PunchScale { get { return _punchScale; } }
        public float PunchDuration { get { return _punchDuration; } }
        public int PunchVibrato { get { return _punchVibrato; } }
        public float PunchElasticity { get { return _punchElasticity; } }
    }
}
