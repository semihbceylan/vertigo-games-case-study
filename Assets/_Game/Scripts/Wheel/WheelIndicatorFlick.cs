using DG.Tweening;
using UnityEngine;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.Wheel
{
    /// <summary>
    /// Kicks the pointer aside on every slice tick and springs it back. One reusable sequence is
    /// restarted per tick, so a fast spin allocates nothing. Purely cosmetic; never affects flow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WheelIndicatorFlick : MonoBehaviour
    {
        #region Configuration and Runtime

        [SerializeField] private RectTransform _indicator;
        [SerializeField] private WheelSliceTickSound _ticker;
        [SerializeField] private IndicatorFlickAnimation _animation;

        private Sequence _sequence;
        private Vector3 _restRotation;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _restRotation = _indicator.localEulerAngles;
        }

        private void OnEnable()
        {
            _ticker.SliceTicked += OnSliceTicked;
        }

        private void OnDisable()
        {
            _ticker.SliceTicked -= OnSliceTicked;
            KillAndRest();
        }

        private void OnDestroy()
        {
            KillAndRest();
        }

        #endregion

        #region Flick

        private void OnSliceTicked()
        {
            if (_sequence == null)
            {
                Vector3 flicked = _restRotation + Vector3.forward * _animation.FlickAngle;
                _sequence = DOTween.Sequence().SetAutoKill(false).SetUpdate(true).SetLink(gameObject);
                _sequence.Append(_indicator.DOLocalRotate(flicked, _animation.FlickDuration).SetEase(_animation.FlickEase));
                _sequence.Append(_indicator.DOLocalRotate(_restRotation, _animation.ReturnDuration).SetEase(_animation.ReturnEase));
                return;
            }
            _sequence.Restart();
        }

        private void KillAndRest()
        {
            if (_sequence != null)
            {
                _sequence.Kill(false);
            }
            _sequence = null;
            if (_indicator)
            {
                _indicator.localEulerAngles = _restRotation;
            }
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_ticker) _ticker = GetComponent<WheelSliceTickSound>();
            if (!_indicator || !_ticker || !_animation)
            {
                Debug.LogError("WheelIndicatorFlick requires serialized indicator, ticker and animation profile.", this);
            }
        }

        #endregion
#endif
    }
}
