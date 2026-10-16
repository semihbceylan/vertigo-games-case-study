using System;
using DG.Tweening;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Wheel
{
    /// <summary>
    /// Spins the wheel in two DOTween phases. First a normal decelerating spin that stops at a
    /// small randomized offset from the winning slice, then a magnetic spring-damper snap that
    /// pulls the wheel exactly onto the slice with decaying oscillation. The outcome is fixed
    /// before the spin and the snap is clamped inside the winning slice, so the reward can
    /// never change. A single Snap Amount slider drives the whole settle feel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WheelSpinner : MonoBehaviour
    {
        [SerializeField] private RectTransform rotationTarget;

        [Header("Spin")]
        [Tooltip("How long the fast spin lasts.")]
        [SerializeField, Min(0.1f)] private float spinDuration = 4.2f;
        [Tooltip("Full rotations before stopping.")]
        [SerializeField, Min(0)] private int extraTurns = 4;

        [Header("Snap")]
        [Tooltip("Magnetic settle feel. 0 = stops dead on the slice, 1 = loose, wobbly settle.")]
        [SerializeField, Range(0f, 1f)] private float snapAmount = 0.45f;

        private const Ease MainEase = Ease.OutCubic;

        private Tween _tween;

        public bool IsSpinning { get; private set; }
        public float SpinDuration => spinDuration;
        public event Action SpinCompleted;

        public RectTransform RotationTarget => rotationTarget;

        public void SetRotationTarget(RectTransform target) => rotationTarget = target;

        /// <summary>
        /// Spins so the slice at <paramref name="sliceIndex"/> stops under the top pointer.
        /// Ignores the call if a spin is already running.
        /// </summary>
        public void SpinToSlice(int sliceIndex, int sliceCount, float startAngle, Action onComplete = null)
        {
            if (IsSpinning) return;

            if (rotationTarget == null || sliceCount <= 0)
            {
                onComplete?.Invoke();
                return;
            }

            IsSpinning = true;

            float angleStep = 360f / sliceCount;
            float sliceAngle = startAngle + sliceIndex * angleStep;

            float currentZ = rotationTarget.localEulerAngles.z;
            float delta = Mathf.Repeat(sliceAngle - currentZ, 360f);
            float exactTarget = currentZ + extraTurns * 360f + delta;

            SnapSettings snap = SnapSettings.FromAmount(snapAmount, angleStep);
            float offset = snap.RandomizeOffset();
            float phaseOneEnd = exactTarget + offset;

            _tween = rotationTarget
                .DOLocalRotate(new Vector3(0f, 0f, phaseOneEnd), spinDuration, RotateMode.FastBeyond360)
                .SetEase(MainEase)
                .OnKill(() => IsSpinning = false)
                .OnComplete(() => StartSnap(exactTarget, offset, snap, onComplete));
        }

        public void KillSpin()
        {
            if (_tween != null && _tween.IsActive()) _tween.Kill(false);
            _tween = null;
            IsSpinning = false;
        }

        private void StartSnap(float exactTarget, float offset, SnapSettings snap, Action onComplete)
        {
            float[] samples = BuildSnapSamples(offset, snap.Stiffness, snap.Damping, snap.SettleDuration);

            _tween = DOVirtual.Float(0f, 1f, snap.SettleDuration, t =>
                {
                    if (rotationTarget == null) return;
                    rotationTarget.localEulerAngles = new Vector3(0f, 0f, exactTarget + Sample(samples, t));
                })
                .SetEase(Ease.Linear)
                .OnKill(() => IsSpinning = false)
                .OnComplete(() =>
                {
                    if (rotationTarget != null)
                        rotationTarget.localEulerAngles = new Vector3(0f, 0f, exactTarget);

                    IsSpinning = false;
                    _tween = null;
                    onComplete?.Invoke();
                    SpinCompleted?.Invoke();
                });
        }

        /// <summary>Maps the 0-1 feel slider to real spring parameters.</summary>
        private readonly struct SnapSettings
        {
            public readonly float Stiffness;
            public readonly float Damping;
            public readonly float SettleDuration;
            private readonly float _offsetRange;

            private SnapSettings(float offsetRange, float stiffness, float damping, float settleDuration)
            {
                _offsetRange = offsetRange;
                Stiffness = stiffness;
                Damping = damping;
                SettleDuration = settleDuration;
            }

            public static SnapSettings FromAmount(float amount, float angleStep)
            {
                float t = Mathf.Clamp01(amount);
                // Never allow the offset to leave the winning slice.
                float safe = angleStep * 0.35f;
                return new SnapSettings(
                    Mathf.Min(Mathf.Lerp(1.5f, 7f, t), safe),
                    Mathf.Lerp(22f, 12f, t),
                    Mathf.Lerp(0.55f, 0.18f, t),
                    Mathf.Lerp(0.7f, 1.4f, t));
            }

            public float RandomizeOffset()
            {
                float offset = UnityEngine.Random.Range(-_offsetRange, _offsetRange);
                const float minMagnitude = 1.5f;
                float magnitude = Mathf.Min(minMagnitude, _offsetRange);
                if (magnitude > 0f && Mathf.Abs(offset) < magnitude)
                    offset = (UnityEngine.Random.value < 0.5f ? -1f : 1f) * magnitude;
                return offset;
            }
        }

        /// <summary>
        /// Integrates an underdamped angular spring (semi-implicit Euler, 240 Hz) from the given
        /// offset to zero and returns the trajectory in degrees. The last sample is exactly zero.
        /// </summary>
        private static float[] BuildSnapSamples(float offsetDegrees, float stiffness, float dampingRatio, float duration)
        {
            const int stepsPerSecond = 240;
            int steps = Mathf.Max(2, Mathf.CeilToInt(duration * stepsPerSecond));
            float dt = duration / steps;

            float[] samples = new float[steps + 1];
            float omega = Mathf.Max(0.01f, stiffness);
            float x = offsetDegrees * Mathf.Deg2Rad;
            float v = 0f;

            // Safety clamp so a misconfigured spring can never cause a large reverse rotation.
            float clamp = Mathf.Abs(offsetDegrees) * 1.6f + 0.5f;

            for (int i = 0; i <= steps; i++)
            {
                samples[i] = Mathf.Clamp(x * Mathf.Rad2Deg, -clamp, clamp);

                float acceleration = -omega * omega * x - 2f * dampingRatio * omega * v;
                v += acceleration * dt;
                x += v * dt;
            }

            samples[steps] = 0f;
            return samples;
        }

        private static float Sample(float[] samples, float t)
        {
            float f = Mathf.Clamp01(t) * (samples.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(f), samples.Length - 2);
            float u = f - i;
            return samples[i] + (samples[i + 1] - samples[i]) * u;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rotationTarget == null)
            {
                var t = transform.Find("ui_wheel_rotation");
                if (t != null) rotationTarget = (RectTransform)t;
            }
        }
#endif
    }
}
