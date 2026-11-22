using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    public interface IRewardFlight
    {
        void Fly(Sprite icon, RectTransform from, RectTransform to, Action onArrive);
        void CancelAll();
    }

    /// <summary>Owns a prewarmed icon pool. Cancellation never invokes gameplay completion.</summary>
    [DisallowMultipleComponent]
    public sealed class RewardFlyAnimator : MonoBehaviour, IRewardFlight
    {
        #region Configuration

        [SerializeField] private RectTransform _flyLayer;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RewardFlyAnimation _animation;
        [SerializeField] private Image _iconPrefab;
        [SerializeField, Min(1)] private int _poolCapacity = 4;

        private readonly List<Flight> _flights = new List<Flight>();

        private sealed class Flight
        {
            private readonly RectTransform _rect;
            private readonly Image _image;
            private Sequence _sequence;
            private Action _onArrive;

            public bool IsActive { get; private set; }

            public Flight(RectTransform rect, Image image)
            {
                _rect = rect;
                _image = image;
                _rect.gameObject.SetActive(false);
            }

            public void Play(Sprite icon, Vector2 start, Vector2 end, RewardFlyAnimation animation, GameObject owner, Action onArrive)
            {
                IsActive = true;
                _onArrive = onArrive;
                _image.sprite = icon;
                _rect.sizeDelta = Vector2.one * animation.IconSize;
                _rect.anchoredPosition = start;
                _rect.localScale = Vector3.one * animation.StartScale;
                _rect.gameObject.SetActive(true);
                _sequence = DOTween.Sequence().SetUpdate(true).SetLink(owner);
                _sequence.Append(_rect.DOScale(animation.PopScale, animation.PopDuration).SetEase(animation.PopEase));
                _sequence.Append(_rect.DOAnchorPos(end, animation.FlyDuration).SetEase(animation.FlyEase));
                _sequence.Join(_rect.DOScale(animation.EndScale, animation.FlyDuration).SetEase(animation.ScaleEase));
                _sequence.OnComplete(Complete);
            }

            public void Cancel()
            {
                if (_sequence != null)
                {
                    _sequence.Kill(false);
                }
                _sequence = null;
                _onArrive = null;
                Release();
            }

            private void Complete()
            {
                _sequence = null;
                Action completion = _onArrive;
                _onArrive = null;
                Release();
                if (completion != null)
                {
                    completion();
                }
            }

            private void Release()
            {
                IsActive = false;
                if (_image)
                {
                    _image.sprite = null;
                }
                if (_rect)
                {
                    _rect.gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region Pool Lifecycle

        private void Awake()
        {
            if (!_flyLayer || !_canvas || !_animation || !_iconPrefab)
            {
                throw new InvalidOperationException("RewardFlyAnimator requires serialized layer, canvas, icon prefab and animation profile.");
            }
            for (int i = 0; i < _poolCapacity; i++)
            {
                Image image = Instantiate(_iconPrefab, _flyLayer, false);
                _flights.Add(new Flight(image.rectTransform, image));
            }
        }

        private void OnDisable()
        {
            CancelAll();
        }

        private void OnDestroy()
        {
            CancelAll();
        }

        public void CancelAll()
        {
            foreach (Flight flight in _flights)
            {
                flight.Cancel();
            }
        }

        #endregion

        #region Playback

        public void Fly(Sprite icon, RectTransform from, RectTransform to, Action onArrive)
        {
            FlyLocal(icon, WorldToLayer(from.position), WorldToLayer(to.position), onArrive);
        }

        private void FlyLocal(Sprite icon, Vector2 start, Vector2 end, Action onArrive)
        {
            foreach (Flight flight in _flights)
            {
                if (!flight.IsActive)
                {
                    flight.Play(icon, start, end, _animation, gameObject, onArrive);
                    return;
                }
            }
            // Bounded cosmetic pool: skip the visual rather than allocating or blocking gameplay.
            if (onArrive != null)
            {
                onArrive();
            }
        }

        private Vector2 WorldToLayer(Vector3 world)
        {
            Camera camera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_flyLayer, screen, camera, out Vector2 local);
            return local;
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_flyLayer)
            {
                _flyLayer = (RectTransform)transform;
            }
            if (!_canvas)
            {
                _canvas = GetComponentInParent<Canvas>();
            }
        }

        #endregion
#endif
    }
}
