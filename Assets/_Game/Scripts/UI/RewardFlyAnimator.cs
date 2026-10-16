using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Spawns short-lived reward icon copies that pop out of a source rect and fly to a target rect
    /// (e.g. a wheel slot to the rewards pane), then invokes a callback on arrival.
    /// Expects to live on a full-canvas overlay rect rendered above the game.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RewardFlyAnimator : MonoBehaviour
    {
        [SerializeField] private RectTransform flyLayer;
        [SerializeField] private float iconSize = 120f;
        [SerializeField] private float popDuration = 0.16f;
        [SerializeField] private float flyDuration = 0.55f;
        [SerializeField] private float popScale = 1.4f;
        [SerializeField] private float startScale = 0.6f;
        [SerializeField] private float endScale = 0.7f;
        [SerializeField] private Ease flyEase = Ease.InQuad;

        private Canvas _canvas;

        private Canvas OwnerCanvas
        {
            get
            {
                if (_canvas == null && flyLayer != null)
                    _canvas = flyLayer.GetComponentInParent<Canvas>();
                return _canvas;
            }
        }

        private Camera EventCamera
        {
            get
            {
                var canvas = OwnerCanvas;
                return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;
            }
        }

        /// <summary>Flies <paramref name="icon"/> from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public void Fly(Sprite icon, RectTransform from, RectTransform to, Action onArrive = null)
        {
            if (flyLayer == null || icon == null || from == null)
            {
                onArrive?.Invoke();
                return;
            }

            Vector2 start = WorldToLayer(from.position);
            Vector2 end = to != null ? WorldToLayer(to.position) : start;
            FlyLocal(icon, start, end, onArrive);
        }

        /// <summary>Flies <paramref name="icon"/> from <paramref name="from"/> to a world position.</summary>
        public void FlyToPosition(Sprite icon, RectTransform from, Vector3 worldTarget, Action onArrive = null)
        {
            if (flyLayer == null || icon == null || from == null)
            {
                onArrive?.Invoke();
                return;
            }

            FlyLocal(icon, WorldToLayer(from.position), WorldToLayer(worldTarget), onArrive);
        }

        private void FlyLocal(Sprite icon, Vector2 start, Vector2 end, Action onArrive)
        {
            var go = new GameObject("ui_image_reward_fly", typeof(RectTransform), typeof(Image));
            go.layer = flyLayer.gameObject.layer;
            go.transform.SetParent(flyLayer, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(iconSize, iconSize);
            rect.anchoredPosition = start;
            rect.localScale = Vector3.one * startScale;

            var image = go.GetComponent<Image>();
            image.sprite = icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.maskable = false;

            var seq = DOTween.Sequence();
            seq.SetUpdate(true);
            seq.Append(rect.DOScale(popScale, popDuration).SetEase(Ease.OutBack));
            seq.Append(rect.DOAnchorPos(end, flyDuration).SetEase(flyEase));
            seq.Join(rect.DOScale(endScale, flyDuration).SetEase(Ease.InQuad));
            seq.OnComplete(() =>
            {
                if (go != null) Destroy(go);
                onArrive?.Invoke();
            });
        }

        private Vector2 WorldToLayer(Vector3 world)
        {
            Camera cam = EventCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(flyLayer, screen, cam, out Vector2 local);
            return local;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (flyLayer == null) flyLayer = (RectTransform)transform;
        }
#endif
    }
}
