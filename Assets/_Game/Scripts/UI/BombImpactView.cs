using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.Wheel;

namespace Vertigo.WheelOfFortune.UI
{
    public interface IBombImpact
    {
        /// <summary>Plays the hit and reports when the loss panel should open.</summary>
        void Play(int bombSliceIndex, Action onPanelDue);
        /// <summary>Restores every animated widget and drops the pending callback.</summary>
        void Cancel();
    }

    /// <summary>
    /// Bomb hit presentation: shakes the wheel, flashes the screen and grows the bomb icon. It owns
    /// no flow; cancellation never invokes the hand-off callback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BombImpactView : MonoBehaviour, IBombImpact
    {
        #region Configuration and Runtime

        private const int NO_SLICE = -1;

        [SerializeField] private RectTransform _shakeTarget;
        [SerializeField] private Image _flash;
        [SerializeField] private WheelLayout _wheelLayout;
        [SerializeField] private BombImpactAnimation _animation;

        private Sequence _sequence;
        private Action _onPanelDue;
        private Vector2 _shakeHome;
        private RectTransform _bombIcon;
        private Vector3 _bombIconHome;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (!_shakeTarget || !_flash || !_wheelLayout || !_animation)
            {
                throw new InvalidOperationException("BombImpactView requires serialized shake target, flash, wheel layout and animation profile.");
            }
            _shakeHome = _shakeTarget.anchoredPosition;
            HideFlash();
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void OnDestroy()
        {
            Cancel();
        }

        #endregion

        #region Playback

        public void Play(int bombSliceIndex, Action onPanelDue)
        {
            Cancel();
            _onPanelDue = onPanelDue;
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.Insert(0f, _shakeTarget.DOShakeAnchorPos(_animation.ShakeDuration, _animation.ShakeStrength,
                _animation.ShakeVibrato, _animation.ShakeRandomness, false, true));

            Color flashColor = _animation.FlashColor;
            flashColor.a = _animation.FlashHiddenAlpha;
            _flash.color = flashColor;
            _flash.enabled = true;
            _sequence.Insert(0f, _flash.DOFade(_animation.FlashPeakAlpha, _animation.FlashInDuration).SetEase(_animation.FlashInEase));
            _sequence.Insert(_animation.FlashInDuration,
                _flash.DOFade(_animation.FlashHiddenAlpha, _animation.FlashOutDuration).SetEase(_animation.FlashOutEase));

            if (bombSliceIndex > NO_SLICE && bombSliceIndex < _wheelLayout.SliceCount)
            {
                _bombIcon = _wheelLayout.GetIcon(bombSliceIndex).rectTransform;
                _bombIconHome = _bombIcon.localScale;
                // Grow in the canvas plane only; the layout keeps icon depth scale at its authored value.
                Vector3 grown = new Vector3(_bombIconHome.x * _animation.IconScale, _bombIconHome.y * _animation.IconScale, _bombIconHome.z);
                _sequence.Insert(0f, _bombIcon.DOScale(grown, _animation.IconDuration).SetEase(_animation.IconEase));
            }

            _sequence.InsertCallback(_animation.PanelDelay, HandOff);
            _sequence.OnComplete(Settle);
        }

        public void Cancel()
        {
            _onPanelDue = null;
            if (_sequence != null)
            {
                _sequence.Kill(false);
            }
            _sequence = null;
            Settle();
            if (_bombIcon)
            {
                _bombIcon.localScale = _bombIconHome;
            }
            _bombIcon = null;
        }

        private void HandOff()
        {
            Action handOff = _onPanelDue;
            _onPanelDue = null;
            if (handOff != null)
            {
                handOff();
            }
        }

        /// <summary>Puts the shaken wheel home and hides the flash; the grown bomb stays until cancelled.</summary>
        private void Settle()
        {
            if (_shakeTarget)
            {
                _shakeTarget.anchoredPosition = _shakeHome;
            }
            HideFlash();
        }

        private void HideFlash()
        {
            if (!_flash)
            {
                return;
            }
            Color hidden = _animation.FlashColor;
            hidden.a = _animation.FlashHiddenAlpha;
            _flash.color = hidden;
            _flash.enabled = false;
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_shakeTarget || !_flash || !_wheelLayout || !_animation)
            {
                Debug.LogError("BombImpactView requires serialized shake target, flash, wheel layout and animation profile.", this);
            }
        }

        #endregion
#endif
    }
}
