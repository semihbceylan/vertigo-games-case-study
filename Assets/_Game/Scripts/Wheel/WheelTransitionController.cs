using System;
using DG.Tweening;
using UnityEngine;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.Wheel
{
    public interface IWheelTransition
    {
        void Play(Action<WheelDiscView> populateIncoming, Tween zoneBar, Action onComplete);
        void Cancel();
    }

    /// <summary>Owns wheel movement, geometry and one reusable incoming disc, not game flow.</summary>
    public sealed class WheelTransitionController : MonoBehaviour, IWheelTransition
    {
        [SerializeField] private WheelDiscView _live;
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private WheelTransitionAnimation _animation;

        private WheelDiscView _incoming;
        private Sequence _sequence;
        private Action _onComplete;
        private Vector2 _home;

        public WheelTransitionAnimation Animation { get { return _animation; } }

        private void Awake()
        {
            if (!_live || !_canvasRect || !_animation)
            {
                throw new InvalidOperationException("Wheel transition requires a disc, canvas and animation profile.");
            }
            _home = _live.Rect.anchoredPosition;
            _incoming = Instantiate(_live, _live.transform.parent);
            _incoming.name = "ui_panel_wheel_incoming";
            _incoming.gameObject.SetActive(false);
        }

        public void Play(Action<WheelDiscView> populateIncoming, Tween zoneBar, Action onComplete)
        {
            Cancel();
            _onComplete = onComplete;
            populateIncoming(_incoming);
            RectTransform parent = (RectTransform)_live.Rect.parent;
            Vector3 edge = _canvasRect.TransformPoint(new Vector3(_canvasRect.rect.width * 0.5f, 0f, 0f));
            float distance = Mathf.Abs(parent.InverseTransformPoint(edge).x - _home.x) + parent.rect.width * 0.5f;
            _incoming.Rect.anchoredPosition = _home + Vector2.right * distance;
            _incoming.Rect.localRotation = Quaternion.identity;
            _incoming.gameObject.SetActive(true);
            _sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            _sequence.AppendInterval(_animation.HoldDuration);
            _sequence.Append(_live.Rect.DOAnchorPosX(_home.x - distance, _animation.SlideDuration).SetEase(_animation.SlideEase));
            _sequence.Join(_incoming.Rect.DOAnchorPosX(_home.x, _animation.SlideDuration).SetEase(_animation.SlideEase));
            if (zoneBar != null) _sequence.Join(zoneBar);
            _sequence.OnComplete(Complete);
        }

        public void Cancel()
        {
            _sequence?.Kill(false);
            _sequence = null;
            _onComplete = null;
            if (_live && _incoming) _live.Rect.anchoredPosition = _home;
            if (_incoming) _incoming.gameObject.SetActive(false);
        }

        private void Complete()
        {
            _sequence = null;
            _live.Rect.anchoredPosition = _home;
            _incoming.gameObject.SetActive(false);
            Action completion = _onComplete;
            _onComplete = null;
            completion?.Invoke();
        }

        private void OnDisable() { Cancel(); }

        private void OnDestroy()
        {
            Cancel();
            if (_incoming) Destroy(_incoming.gameObject);
        }
    }
}
