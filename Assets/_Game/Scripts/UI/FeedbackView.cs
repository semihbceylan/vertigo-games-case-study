using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Central feedback widget. Blocking banners (bomb / death screen) use a tall, gradient-framed
    /// panel with the title on top, a large image in the middle and the buttons stacked at the
    /// bottom. Non-blocking reward toasts fade away on their own.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FeedbackView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;

        [Header("Death screen (blocking banner)")]
        [SerializeField] private RectTransform deathScreen;
        [SerializeField] private TMP_Text deathTitle;
        [SerializeField] private Image deathIcon;
        [SerializeField] private UIButton actionButton;
        [SerializeField] private TMP_Text _actionLabel;

        [Header("Reward toast")]
        [SerializeField] private Image toastBackground;
        [SerializeField] private Image toastIcon;
        [SerializeField] private TMP_Text toastMessage;

        [Header("Death spotlight")]
        [Tooltip("Full-screen dim with rounded cut-outs (uses the UI Dim Cutout shader).")]
        [SerializeField] private Image dimOverlay;
        [Tooltip("Red damage vignette shown with the death screen.")]
        [SerializeField] private GameObject damageGlow;
        [Tooltip("Rects the dim leaves clear (e.g. the rewards pane and the death card).")]
        [SerializeField] private RectTransform spotlightA;
        [SerializeField] private RectTransform spotlightB;

        [SerializeField] private PanelAnimation _animation;

        public event Action ActionRequested;

        private Tween _fade;
        private Sequence _toastSequence;
        private Material _dimMaterial;

        private void Awake()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }

        }

        private void OnEnable()
        {
            if (actionButton) actionButton.Clicked += OnActionClicked;
        }

        private void OnDisable()
        {
            if (actionButton) actionButton.Clicked -= OnActionClicked;
            HideImmediate();
        }

        private void OnDestroy()
        {
            HideImmediate();
            if (_dimMaterial)
            {
                Destroy(_dimMaterial);
            }
        }

        private void OnActionClicked()
        {
            Hide();
            ActionRequested?.Invoke();
        }

        /// <summary>Blocking message that requires the player to press the action button.</summary>
        public void ShowBanner(string text, Color color, Sprite iconSprite, string actionLabel)
        {
            KillToast();
            SetRoots(banner: true);

            if (deathTitle != null)
            {
                deathTitle.text = text;
                deathTitle.color = color;
            }

            if (deathIcon != null)
            {
                deathIcon.sprite = iconSprite;
                deathIcon.enabled = iconSprite != null;
            }

            bool hasAction = !string.IsNullOrEmpty(actionLabel);
            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(hasAction);
                if (hasAction)
                {
                    if (_actionLabel) _actionLabel.text = actionLabel;
                }
            }

            SetBlocking(true);
            FadeTo(_animation.VisibleAlpha);
        }

        /// <summary>Non-blocking toast that fades away by itself.</summary>
        public void ShowToast(string text, Color color, Sprite iconSprite)
        {
            KillToast();
            SetRoots(banner: false);

            if (toastMessage != null)
            {
                toastMessage.text = text;
                toastMessage.color = color;
            }

            if (toastIcon != null)
            {
                toastIcon.sprite = iconSprite;
                toastIcon.enabled = iconSprite != null;
            }

            SetBlocking(false);
            FadeTo(_animation.VisibleAlpha);

            _toastSequence = DOTween.Sequence();
            _toastSequence.AppendInterval(_animation.HoldDuration);
            _toastSequence.AppendCallback(HideToast);
            _toastSequence.SetUpdate(true);
            _toastSequence.SetLink(gameObject);
        }

        private void HideToast()
        {
            FadeTo(_animation.HiddenAlpha);
        }

        public void Hide()
        {
            KillToast();

            // Unblock input immediately even if the fade itself stalls.
            SetBlocking(false);
            FadeTo(_animation.HiddenAlpha);
        }

        public void HideImmediate()
        {
            KillToast();
            _fade?.Kill(false);
            _fade = null;
            SetBlocking(false);
            if (group) group.alpha = _animation ? _animation.HiddenAlpha : 0f;
        }

        private void KillToast()
        {
            if (_toastSequence != null && _toastSequence.IsActive()) _toastSequence.Kill(false);
            _toastSequence = null;
        }

        private void SetRoots(bool banner)
        {
            if (deathScreen != null) deathScreen.gameObject.SetActive(banner);
            if (toastBackground != null) toastBackground.gameObject.SetActive(!banner);
            if (dimOverlay != null) dimOverlay.gameObject.SetActive(banner);
            if (damageGlow != null) damageGlow.SetActive(banner);

            if (banner) ApplySpotlight();
        }

        /// <summary>Punches rounded cut-outs for the rewards pane and the death card into the dim.</summary>
        private void ApplySpotlight()
        {
            if (dimOverlay == null) return;

            if (_dimMaterial == null)
            {
                _dimMaterial = new Material(dimOverlay.material);
                dimOverlay.material = _dimMaterial;
            }

            var dimRect = (RectTransform)dimOverlay.transform;
            Vector2 size = dimRect.rect.size;
            _dimMaterial.SetVector("_DimSize", new Vector4(size.x, size.y, 0f, 0f));
            _dimMaterial.SetVector("_CutoutA", CutoutFor(spotlightA, dimRect));
            _dimMaterial.SetVector("_CutoutB", CutoutFor(spotlightB, dimRect));
        }

        private static Vector4 CutoutFor(RectTransform target, RectTransform dimRect)
        {
            if (target == null) return new Vector4(0f, -99999f, 0f, 0f);

            Vector3 local = dimRect.InverseTransformPoint(target.position);
            Vector2 size = target.rect.size;
            return new Vector4(local.x, local.y, size.x * 0.5f, size.y * 0.5f);
        }

        private void SetBlocking(bool blocking)
        {
            if (group == null) return;
            group.blocksRaycasts = blocking;
            group.interactable = blocking;
        }

        private void FadeTo(float target)
        {
            if (group == null)
            {
                gameObject.SetActive(target > 0f);
                return;
            }

            _fade?.Kill();
            _fade = group.DOFade(target, _animation.FadeDuration).SetEase(_animation.FadeEase).SetUpdate(true).SetLink(gameObject);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!group) group = GetComponent<CanvasGroup>();
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (!deathScreen && child.name == "ui_image_feedback_death_screen") deathScreen = (RectTransform)child;
                if (!deathTitle && child.name == "ui_text_feedback_title_value") deathTitle = child.GetComponent<TMP_Text>();
                if (!deathIcon && child.name == "ui_image_feedback_death_value") deathIcon = child.GetComponent<Image>();
                if (!actionButton && child.name == "ui_button_feedback_action") actionButton = child.GetComponent<UIButton>();
                if (!toastBackground && child.name == "ui_image_feedback_toast") toastBackground = child.GetComponent<Image>();
                if (!toastIcon && child.name == "ui_image_feedback_toast_icon_value") toastIcon = child.GetComponent<Image>();
                if (!toastMessage && child.name == "ui_text_feedback_toast_message_value") toastMessage = child.GetComponent<TMP_Text>();
                if (!dimOverlay && child.name == "ui_image_feedback_dim") dimOverlay = child.GetComponent<Image>();
                if (!damageGlow && child.name == "ui_image_feedback_damage_glow") damageGlow = child.gameObject;
            }
            if (!_actionLabel && actionButton) _actionLabel = actionButton.GetComponentInChildren<TMP_Text>(true);
            if (spotlightB == null && deathScreen != null) spotlightB = deathScreen;
        }
#endif
    }
}
