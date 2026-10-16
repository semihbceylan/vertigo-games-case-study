using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [Tooltip("Optional line shown between the title and the image, above the buttons (e.g. the revive hint).")]
        [SerializeField] private TMP_Text deathCaption;
        [SerializeField] private string deathCaptionText = "Revive yourself to keep your rewards.";
        [SerializeField] private UIButton actionButton;
        [SerializeField] private UIButton reviveButton;

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

        [SerializeField] private float fadeDuration = 0.25f;

        public event Action ActionRequested;
        public event Action ReviveRequested;

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

            if (actionButton != null)
                actionButton.Clicked += OnActionClicked;

            if (reviveButton != null)
                reviveButton.Clicked += OnReviveClicked;
        }

        private void OnDestroy()
        {
            if (actionButton != null)
                actionButton.Clicked -= OnActionClicked;

            if (reviveButton != null)
                reviveButton.Clicked -= OnReviveClicked;
        }

        private void OnActionClicked()
        {
            Hide();
            ActionRequested?.Invoke();
        }

        private void OnReviveClicked()
        {
            Hide();
            ReviveRequested?.Invoke();
        }

        /// <summary>Blocking message that requires the player to press the action button.</summary>
        public void ShowBanner(string text, Color color, Sprite iconSprite, string actionLabel, bool showRevive = false)
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
                    var label = actionButton.GetComponentInChildren<TMP_Text>();
                    if (label != null) label.text = actionLabel;
                }
            }

            if (reviveButton != null)
                reviveButton.gameObject.SetActive(showRevive);

            // The caption only makes sense on the bomb/death screen (it explains the revive option).
            if (deathCaption != null)
            {
                deathCaption.text = deathCaptionText;
                deathCaption.gameObject.SetActive(showRevive);
            }

            SetBlocking(true);
            FadeTo(1f);
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
            FadeTo(1f);

            _toastSequence = DOTween.Sequence();
            _toastSequence.AppendInterval(1.1f);
            _toastSequence.AppendCallback(() => FadeTo(0f));
            _toastSequence.SetUpdate(true);
        }

        public void Hide()
        {
            KillToast();

            // Unblock input immediately even if the fade itself stalls.
            SetBlocking(false);
            FadeTo(0f);
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
            _fade = group.DOFade(target, fadeDuration).SetUpdate(true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (deathScreen == null) deathScreen = FindDeep<RectTransform>("ui_image_feedback_death_screen");
            if (deathTitle == null) deathTitle = FindDeep<TMP_Text>("ui_text_feedback_title_value");
            if (deathIcon == null) deathIcon = FindDeep<Image>("ui_image_feedback_death_value");
            if (deathCaption == null) deathCaption = FindDeep<TMP_Text>("ui_text_feedback_death_caption_value");
            if (actionButton == null) actionButton = FindDeep<UIButton>("ui_button_feedback_action");
            if (reviveButton == null) reviveButton = FindDeep<UIButton>("ui_button_feedback_revive");
            if (toastBackground == null) toastBackground = FindDeep<Image>("ui_image_feedback_toast");
            if (toastIcon == null) toastIcon = FindDeep<Image>("ui_image_feedback_toast_icon_value");
            if (toastMessage == null) toastMessage = FindDeep<TMP_Text>("ui_text_feedback_toast_message_value");
            if (dimOverlay == null) dimOverlay = FindDeep<Image>("ui_image_feedback_dim");
            if (damageGlow == null)
            {
                var glow = FindDeep<Transform>("ui_image_feedback_damage_glow");
                if (glow != null) damageGlow = glow.gameObject;
            }
            if (spotlightB == null && deathScreen != null) spotlightB = deathScreen;
        }

        private T FindDeep<T>(string name) where T : Component
        {
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == name) return all[i].GetComponent<T>();
            }
            return null;
        }
#endif
    }
}
