using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.Wheel
{
    public interface IWheelSpinPresentation
    {
        void SpinToSlice(int sliceIndex, int sliceCount, float startAngle, Action onComplete = null);
        void CancelSpin();
    }

    /// <summary>Animates a predetermined result using an authored spin/spring profile.</summary>
    [DisallowMultipleComponent]
    public sealed class WheelSpinner : MonoBehaviour, IWheelSpinPresentation
    {
        #region Configuration and Runtime

        private const float FULL_CIRCLE_DEGREES = 360f;
        private const int SPRING_SAMPLES_PER_SECOND = 240;
        private const int MINIMUM_SPRING_STEPS = 2;
        private const float MINIMUM_STIFFNESS = 0.01f;
        private const float SPRING_DAMPING_FACTOR = 2f;
        private const float SIGN_PROBABILITY = 0.5f;

        [FormerlySerializedAs("rotationTarget")]
        [SerializeField] private RectTransform _rotationTarget;
        [SerializeField] private WheelSpinAnimation _animation;

        private Tween _tween;
        private Action _onComplete;
        private float _exactTarget;
        private float[] _samples;

        public bool IsSpinning { get; private set; }
        public float SpinDuration { get { return _animation.Duration; } }
        public RectTransform RotationTarget { get { return _rotationTarget; } }
        public event Action SpinCompleted;

        public void SetRotationTarget(RectTransform target)
        {
            _rotationTarget = target;
        }

        #endregion

        #region Playback and Lifecycle

        public void SpinToSlice(int sliceIndex, int sliceCount, float startAngle, Action onComplete = null)
        {
            if (IsSpinning)
            {
                return;
            }
            if (!_rotationTarget || !_animation)
            {
                throw new InvalidOperationException("WheelSpinner requires a serialized target and animation profile.");
            }
            if (sliceCount <= 0 || sliceIndex < 0 || sliceIndex >= sliceCount)
            {
                throw new ArgumentOutOfRangeException(nameof(sliceIndex));
            }

            IsSpinning = true;
            _onComplete = onComplete;
            float angleStep = FULL_CIRCLE_DEGREES / sliceCount;
            float sliceAngle = startAngle + sliceIndex * angleStep;
            float current = _rotationTarget.localEulerAngles.z;
            float delta = Mathf.Repeat(sliceAngle - current, FULL_CIRCLE_DEGREES);
            _exactTarget = current + _animation.ExtraTurns * FULL_CIRCLE_DEGREES + delta;

            float offsetLimit = _animation.GetOffsetLimit(angleStep);
            float offset = UnityEngine.Random.Range(-offsetLimit, offsetLimit);
            float minimum = Mathf.Min(_animation.MinimumOffset, offsetLimit);
            if (minimum > 0f && Mathf.Abs(offset) < minimum)
            {
                offset = (UnityEngine.Random.value < SIGN_PROBABILITY ? -1f : 1f) * minimum;
            }
            _samples = BuildSnapSamples(offset);

            _tween = _rotationTarget
                .DOLocalRotate(new Vector3(0f, 0f, _exactTarget + offset), _animation.Duration, RotateMode.FastBeyond360)
                .SetEase(_animation.SpinEase)
                .SetLink(gameObject)
                .OnComplete(StartSnap);
        }

        private void StartSnap()
        {
            _tween = DOVirtual.Float(0f, 1f, _animation.SettleDuration, ApplySnap)
                .SetEase(_animation.SettleEase)
                .SetLink(gameObject)
                .OnComplete(CompleteSpin);
        }

        private void ApplySnap(float progress)
        {
            if (_rotationTarget)
            {
                _rotationTarget.localEulerAngles = new Vector3(0f, 0f, _exactTarget + Sample(_samples, progress));
            }
        }

        private void CompleteSpin()
        {
            if (_rotationTarget)
            {
                _rotationTarget.localEulerAngles = new Vector3(0f, 0f, _exactTarget);
            }
            IsSpinning = false;
            _tween = null;
            Action completion = _onComplete;
            _onComplete = null;
            completion?.Invoke();
            SpinCompleted?.Invoke();
        }

        public void CancelSpin()
        {
            _tween?.Kill(false);
            _tween = null;
            _onComplete = null;
            IsSpinning = false;
        }

        private void OnDisable()
        {
            CancelSpin();
        }

        private void OnDestroy()
        {
            CancelSpin();
        }

        #endregion

        #region Spring Integration

        private float[] BuildSnapSamples(float offsetDegrees)
        {
            float duration = _animation.SettleDuration;
            int steps = Mathf.Max(MINIMUM_SPRING_STEPS, Mathf.CeilToInt(duration * SPRING_SAMPLES_PER_SECOND));
            float dt = duration / steps;
            float[] samples = new float[steps + 1];
            float omega = Mathf.Max(MINIMUM_STIFFNESS, _animation.Stiffness);
            float position = offsetDegrees * Mathf.Deg2Rad;
            float velocity = 0f;
            float clamp = Mathf.Abs(offsetDegrees) * _animation.TrajectoryClampMultiplier + _animation.TrajectoryClampPadding;
            for (int i = 0; i <= steps; i++)
            {
                samples[i] = Mathf.Clamp(position * Mathf.Rad2Deg, -clamp, clamp);
                float acceleration = -omega * omega * position - SPRING_DAMPING_FACTOR * _animation.Damping * omega * velocity;
                velocity += acceleration * dt;
                position += velocity * dt;
            }
            samples[steps] = 0f;
            return samples;
        }

        private static float Sample(float[] samples, float progress)
        {
            float index = Mathf.Clamp01(progress) * (samples.Length - 1);
            int lower = Mathf.Min(Mathf.FloorToInt(index), samples.Length - 2);
            return Mathf.Lerp(samples[lower], samples[lower + 1], index - lower);
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_rotationTarget)
            {
                Transform target = transform.Find("ui_panel_wheel_rotation");
                if (target)
                {
                    _rotationTarget = (RectTransform)target;
                }
            }
        }
#endif
    }
}
