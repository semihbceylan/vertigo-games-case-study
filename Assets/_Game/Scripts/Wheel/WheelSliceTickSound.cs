using UnityEngine;
using UnityEngine.Serialization;

namespace Vertigo.WheelOfFortune.Wheel
{
    /// <summary>
    /// Ratchet tick: plays a click every time a slice boundary passes under the pointer while the
    /// wheel is spinning. Pure observer — never affects the outcome.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WheelSliceTickSound : MonoBehaviour
    {
        #region Configuration and Runtime

        [SerializeField, FormerlySerializedAs("rotationTarget")] private RectTransform _rotationTarget;
        [SerializeField, FormerlySerializedAs("wheelLayout")] private WheelLayout _wheelLayout;
        [SerializeField, FormerlySerializedAs("wheelSpinner")] private WheelSpinner _wheelSpinner;
        [SerializeField, FormerlySerializedAs("audioSource")] private AudioSource _audioSource;
        [SerializeField, FormerlySerializedAs("clickClip")] private AudioClip _clickClip;
        [SerializeField, Range(0f, 1f), FormerlySerializedAs("volume")] private float _volume = 0.8f;
        [Tooltip("Minimum rotation since the last tick, in degrees, so a boundary hovering under the pointer cannot repeat.")]
        [SerializeField, Min(0f)] private float _minimumTickTravel = 2f;

        private float _unwrappedAngle;
        private float _lastRawAngle;
        private int _lastTickStep;
        private float _lastTickUnwrappedAngle;
        private bool _isTracking;

        /// <summary>How many boundary crossings have been detected (useful for testing).</summary>
        public int TickCount { get; private set; }

        #endregion

        #region Tracking

        private void OnEnable()
        {
            _isTracking = false;
        }

        private void Update()
        {
            if (_wheelLayout.SliceCount <= 0)
            {
                return;
            }
            float step = WheelLayout.FULL_CIRCLE_DEGREES / _wheelLayout.SliceCount;
            float raw = _rotationTarget.localEulerAngles.z;

            if (!_isTracking)
            {
                _lastRawAngle = raw;
                _unwrappedAngle = raw;
                _lastTickStep = GetStep(_unwrappedAngle, step);
                _lastTickUnwrappedAngle = _unwrappedAngle;
                _isTracking = true;
                return;
            }

            // Continuous angle: immune to 0/360 wrap jumps, so each genuine pass ticks once.
            _unwrappedAngle += Mathf.DeltaAngle(_lastRawAngle, raw);
            _lastRawAngle = raw;
            int stepIndex = GetStep(_unwrappedAngle, step);

            if (!_wheelSpinner.IsSpinning)
            {
                _lastTickStep = stepIndex;
                _lastTickUnwrappedAngle = _unwrappedAngle;
                return;
            }
            if (stepIndex != _lastTickStep && Mathf.Abs(_unwrappedAngle - _lastTickUnwrappedAngle) >= _minimumTickTravel)
            {
                PlayTick();
                _lastTickStep = stepIndex;
                _lastTickUnwrappedAngle = _unwrappedAngle;
            }
        }

        private int GetStep(float angle, float step)
        {
            return Mathf.FloorToInt((angle - _wheelLayout.StartAngle) / step);
        }

        private void PlayTick()
        {
            TickCount++;
            _audioSource.PlayOneShot(_clickClip, _volume);
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_wheelLayout) _wheelLayout = GetComponentInChildren<WheelLayout>(true);
            if (!_wheelSpinner) _wheelSpinner = GetComponent<WheelSpinner>();
            if (!_audioSource) _audioSource = GetComponent<AudioSource>();
            if (!_rotationTarget || !_wheelLayout || !_wheelSpinner || !_audioSource || !_clickClip)
            {
                Debug.LogError("WheelSliceTickSound requires serialized rotation, layout, spinner, audio source and clip.", this);
            }
        }

        #endregion
#endif
    }
}
