using UnityEngine;

namespace Vertigo.WheelOfFortune.Wheel
{
    /// <summary>
    /// Ratchet tick: plays a click every time a slice boundary passes under the pointer
    /// while the wheel is spinning. Pure observer — never affects the outcome.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WheelSliceTickSound : MonoBehaviour
    {
        [SerializeField] private RectTransform rotationTarget;
        [SerializeField] private WheelLayout wheelLayout;
        [SerializeField] private WheelSpinner wheelSpinner;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

        private float _unwrappedAngle;
        private float _lastRawAngle;
        private int _lastTickStep;
        private float _lastTickUnwrappedAngle;
        private bool _initialized;

        /// <summary>How many boundary crossings have been detected (useful for testing).</summary>
        public int TickCount { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            EnsureAudioSource();
        }

        private void OnEnable()
        {
            _initialized = false;
        }

        private void Update()
        {
            if (rotationTarget == null || wheelLayout == null || wheelLayout.SliceCount <= 0)
                return;

            float step = 360f / wheelLayout.SliceCount;
            float raw = rotationTarget.localEulerAngles.z;

            if (!_initialized)
            {
                _lastRawAngle = raw;
                _unwrappedAngle = raw;
                _lastTickStep = Mathf.FloorToInt((raw - wheelLayout.StartAngle) / step);
                _lastTickUnwrappedAngle = _unwrappedAngle;
                _initialized = true;
                return;
            }

            // Continuous angle: immune to 0/360 wrap jumps, so each genuine pass ticks once.
            _unwrappedAngle += Mathf.DeltaAngle(_lastRawAngle, raw);
            _lastRawAngle = raw;

            int stepIndex = Mathf.FloorToInt((_unwrappedAngle - wheelLayout.StartAngle) / step);

            if (wheelSpinner != null && !wheelSpinner.IsSpinning)
            {
                _lastTickStep = stepIndex;
                _lastTickUnwrappedAngle = _unwrappedAngle;
                return;
            }

            // Travel guard: ignore sub-degree jitter so a hovered boundary can't machine-gun.
            if (stepIndex != _lastTickStep && Mathf.Abs(_unwrappedAngle - _lastTickUnwrappedAngle) >= 2f)
            {
                PlayTick();
                _lastTickStep = stepIndex;
                _lastTickUnwrappedAngle = _unwrappedAngle;
            }
        }

        private void PlayTick()
        {
            TickCount++;
            if (audioSource != null && clickClip != null)
                audioSource.PlayOneShot(clickClip, volume);
        }

        private void ResolveReferences()
        {
            if (rotationTarget == null)
            {
                var t = transform.Find("ui_wheel_rotation");
                if (t != null) rotationTarget = (RectTransform)t;
            }

            if (wheelLayout == null) wheelLayout = GetComponentInChildren<WheelLayout>(true);
            if (wheelSpinner == null) wheelSpinner = GetComponent<WheelSpinner>();
            if (wheelSpinner == null) wheelSpinner = GetComponentInChildren<WheelSpinner>(true);
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.loop = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
