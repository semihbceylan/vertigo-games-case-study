using DG.Tweening;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    [CreateAssetMenu(menuName = "Wheel of Fortune/Animation/Wheel Spin", fileName = "Animation_WheelSpin")]
    public sealed class WheelSpinAnimation : ScriptableObject
    {
        #region Serialized Data

        [SerializeField, Min(0.1f)] private float _duration = 4.2f;
        [SerializeField, Min(0)] private int _extraTurns = 4;
        [SerializeField] private Ease _spinEase = Ease.OutCubic;
        [SerializeField] private Ease _settleEase = Ease.Linear;
        [SerializeField, Range(0f, 1f)] private float _snapAmount = 0.45f;
        [SerializeField, Range(0f, 0.49f)] private float _sliceOffsetLimit = 0.35f;
        [SerializeField, Min(0f)] private float _minimumOffset = 1.5f;
        [SerializeField] private Vector2 _offsetRange = new Vector2(1.5f, 7f);
        [SerializeField] private Vector2 _stiffnessRange = new Vector2(22f, 12f);
        [SerializeField] private Vector2 _dampingRange = new Vector2(0.55f, 0.18f);
        [SerializeField] private Vector2 _settleDurationRange = new Vector2(0.7f, 1.4f);
        [SerializeField, Min(1f)] private float _trajectoryClampMultiplier = 1.6f;
        [SerializeField, Min(0f)] private float _trajectoryClampPadding = 0.5f;

        #endregion

        #region Read API

        public float Duration { get { return _duration; } }
        public int ExtraTurns { get { return _extraTurns; } }
        public Ease SpinEase { get { return _spinEase; } }
        public Ease SettleEase { get { return _settleEase; } }
        public float MinimumOffset { get { return _minimumOffset; } }
        public float Stiffness { get { return Evaluate(_stiffnessRange); } }
        public float Damping { get { return Evaluate(_dampingRange); } }
        public float SettleDuration { get { return Evaluate(_settleDurationRange); } }
        public float TrajectoryClampMultiplier { get { return _trajectoryClampMultiplier; } }
        public float TrajectoryClampPadding { get { return _trajectoryClampPadding; } }

        public float GetOffsetLimit(float sliceAngle)
        {
            return Mathf.Min(Evaluate(_offsetRange), sliceAngle * _sliceOffsetLimit);
        }

        private float Evaluate(Vector2 range)
        {
            return Mathf.Lerp(range.x, range.y, _snapAmount);
        }

        #endregion
    }
}
