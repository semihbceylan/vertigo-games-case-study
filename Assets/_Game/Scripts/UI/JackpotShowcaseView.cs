using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Full-screen jackpot showcase. Shows a rare reward (weapon Render / melee) big in the
    /// centre of the screen with a dim background. Stays up until the player taps anywhere.
    /// Builds its own widgets in code so no prefab or scene setup is required beyond adding
    /// the component to a full-canvas rect (it should be the last sibling to render on top).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JackpotShowcaseView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image dim;
        [SerializeField] private Button dismissButton;
        [SerializeField] private Sprite flashSprite;
        [SerializeField] private Image bigIcon;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private TMP_Text hintLabel;

        [Header("Presentation")]
        [SerializeField] private float flashSize = 900f;
        [SerializeField] private float flashSpinDuration = 14f;
        [SerializeField] private float iconSize = 780f;
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private float popDuration = 0.35f;
        [SerializeField] private float titleFontSize = 64f;
        [SerializeField] private float amountFontSize = 52f;

        [Header("Text")]
        [SerializeField] private string hintText = "TAP ANYWHERE TO CONTINUE";

        /// <summary>Raised once when the player dismisses a visible showcase.</summary>
        public event Action Dismissed;

        public bool IsShowing { get; private set; }

        private Tween _fade;
        private Tween _pop;
        private Tween _spin;
        private Image _flash;

        private void Awake()
        {
            EnsureBuilt();

            if (dismissButton != null)
                dismissButton.onClick.AddListener(OnDismissClicked);

            ApplyHiddenImmediate();
        }

        private void OnDestroy()
        {
            if (dismissButton != null)
                dismissButton.onClick.RemoveListener(OnDismissClicked);
            _fade?.Kill();
            _pop?.Kill();
            _spin?.Kill();
        }

        /// <summary>Shows the reward big. Clicking anywhere dismisses it.</summary>
        public void Show(Sprite icon, string label, long amount)
        {
            EnsureBuilt();
            transform.SetAsLastSibling();

            if (bigIcon != null)
            {
                bigIcon.sprite = icon;
                bigIcon.enabled = icon != null;
                bigIcon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            }

            if (titleLabel != null) titleLabel.text = string.IsNullOrEmpty(label) ? "JACKPOT!" : label;
            if (amountLabel != null) amountLabel.text = "x" + amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            if (hintLabel != null) hintLabel.text = hintText;

            IsShowing = true;
            gameObject.SetActive(true);

            if (group != null)
            {
                group.blocksRaycasts = true;
                group.interactable = true;
                _fade?.Kill();
                _fade = group.DOFade(1f, fadeDuration).SetUpdate(true);
            }

            if (bigIcon != null)
            {
                _pop?.Kill();
                bigIcon.rectTransform.localScale = Vector3.one * 0.5f;
                _pop = bigIcon.rectTransform.DOScale(Vector3.one, popDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }

            StartFlashSpin();
        }

        /// <summary>Player-driven dismiss. Fires <see cref="Dismissed"/> once per <see cref="Show"/>.</summary>
        public void Hide()
        {
            if (!IsShowing) return;
            IsShowing = false;

            if (group != null)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
                _fade?.Kill();
                _fade = group.DOFade(0f, fadeDuration).SetUpdate(true);
            }

            Dismissed?.Invoke();
        }

        /// <summary>Silent hide for restarts/revives. Does not fire <see cref="Dismissed"/>.</summary>
        public void HideImmediate()
        {
            IsShowing = false;
            _fade?.Kill();
            _pop?.Kill();
            _spin?.Kill();
            ApplyHiddenImmediate();
        }

        private void OnDismissClicked() => Hide();

        private void ApplyHiddenImmediate()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        private void EnsureBuilt()
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            if (group == null)
            {
                group = GetComponent<CanvasGroup>();
                if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            }

            if (dim == null)
            {
                dim = FindDeep<Image>("ui_showcase_dim");
                if (dim == null) dim = CreateFullStretch("ui_showcase_dim", new Color(0f, 0f, 0f, 0.82f));
                else StretchFull(dim.rectTransform);
            }

            if (dismissButton == null)
            {
                var btnTransform = FindDeep<Transform>("ui_button_showcase_dismiss");
                if (btnTransform != null) dismissButton = btnTransform.GetComponent<Button>();
                if (dismissButton == null)
                {
                    var btnGo = new GameObject("ui_button_showcase_dismiss", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnGo.layer = gameObject.layer;
                    btnGo.transform.SetParent(transform, false);
                    StretchFull((RectTransform)btnGo.transform);
                    // Transparent but raycastable so any tap dismisses.
                    var bg = btnGo.GetComponent<Image>();
                    bg.color = new Color(0f, 0f, 0f, 0f);
                    dismissButton = btnGo.GetComponent<Button>();
                    dismissButton.transition = Selectable.Transition.None;
                }
            }

            // Keep the tap catcher behind the content.
            dismissButton.transform.SetSiblingIndex(dim.transform.GetSiblingIndex() + 1);

            if (_flash == null)
            {
                _flash = FindDeep<Image>("ui_image_showcase_flash");
                if (_flash == null)
                {
                    var flashGo = new GameObject("ui_image_showcase_flash", typeof(RectTransform), typeof(Image));
                    flashGo.layer = gameObject.layer;
                    flashGo.transform.SetParent(transform, false);
                    _flash = flashGo.GetComponent<Image>();
                    _flash.raycastTarget = false;
                    _flash.preserveAspect = true;
                    var fr = _flash.rectTransform;
                    fr.anchorMin = new Vector2(0.5f, 0.5f);
                    fr.anchorMax = new Vector2(0.5f, 0.5f);
                    fr.pivot = new Vector2(0.5f, 0.5f);
                    fr.sizeDelta = new Vector2(flashSize, flashSize);
                    fr.anchoredPosition = new Vector2(0f, 60f);
                }
            }

            if (bigIcon == null)
            {
                bigIcon = FindDeep<Image>("ui_image_showcase_icon_value");
                if (bigIcon == null)
                {
                    var iconGo = new GameObject("ui_image_showcase_icon_value", typeof(RectTransform), typeof(Image));
                    iconGo.layer = gameObject.layer;
                    iconGo.transform.SetParent(transform, false);
                    bigIcon = iconGo.GetComponent<Image>();
                    bigIcon.raycastTarget = false;
                    bigIcon.preserveAspect = true;
                    var r = bigIcon.rectTransform;
                    r.anchorMin = new Vector2(0.5f, 0.5f);
                    r.anchorMax = new Vector2(0.5f, 0.5f);
                    r.pivot = new Vector2(0.5f, 0.5f);
                    r.sizeDelta = new Vector2(iconSize, iconSize);
                    r.anchoredPosition = new Vector2(0f, 60f);
                }
            }

            if (titleLabel == null)
            {
                titleLabel = FindDeep<TMP_Text>("ui_text_showcase_title_value");
                if (titleLabel == null)
                    titleLabel = CreateLabel("ui_text_showcase_title_value", titleFontSize, new Vector2(0f, -260f), 1000f, 90f);
            }

            if (amountLabel == null)
            {
                amountLabel = FindDeep<TMP_Text>("ui_text_showcase_amount_value");
                if (amountLabel == null)
                    amountLabel = CreateLabel("ui_text_showcase_amount_value", amountFontSize, new Vector2(0f, -340f), 1000f, 80f);
            }

            if (hintLabel == null)
            {
                hintLabel = FindDeep<TMP_Text>("ui_text_showcase_hint_value");
                if (hintLabel == null)
                {
                    hintLabel = CreateLabel("ui_text_showcase_hint_value", 30f, new Vector2(0f, -440f), 1000f, 50f);
                    hintLabel.alpha = 0.75f;
                }
            }

            // Content renders above the tap catcher.
            if (_flash != null)
            {
                _flash.sprite = flashSprite;
                _flash.enabled = flashSprite != null;
                if (flashSprite != null)
                {
                    _flash.rectTransform.sizeDelta = new Vector2(flashSize, flashSize);
                    _flash.transform.SetSiblingIndex(dismissButton.transform.GetSiblingIndex() + 1);
                }
            }
            if (bigIcon != null) bigIcon.transform.SetAsLastSibling();
            if (titleLabel != null) titleLabel.transform.SetAsLastSibling();
            if (amountLabel != null) amountLabel.transform.SetAsLastSibling();
            if (hintLabel != null) hintLabel.transform.SetAsLastSibling();
        }

        /// <summary>Restarts the slow star-flash rotation behind the icon.</summary>
        private void StartFlashSpin()
        {
            _spin?.Kill();
            if (_flash == null || flashSprite == null || flashSpinDuration <= 0f) return;
            _flash.rectTransform.localRotation = Quaternion.identity;
            _spin = _flash.rectTransform.DORotate(new Vector3(0f, 0f, -360f), flashSpinDuration, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental)
                .SetEase(Ease.Linear)
                .SetUpdate(true);
        }

        private Image CreateFullStretch(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            StretchFull(rect);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text CreateLabel(string name, float fontSize, Vector2 anchoredPos, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = anchoredPos;
            return text;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (dim == null) dim = FindDeep<Image>("ui_showcase_dim");
            if (bigIcon == null) bigIcon = FindDeep<Image>("ui_image_showcase_icon_value");
            if (titleLabel == null) titleLabel = FindDeep<TMP_Text>("ui_text_showcase_title_value");
            if (amountLabel == null) amountLabel = FindDeep<TMP_Text>("ui_text_showcase_amount_value");
            if (hintLabel == null) hintLabel = FindDeep<TMP_Text>("ui_text_showcase_hint_value");
            var btn = FindDeep<Transform>("ui_button_showcase_dismiss");
            if (btn != null && dismissButton == null) dismissButton = btn.GetComponent<Button>();
            if (_flash == null) _flash = FindDeep<Image>("ui_image_showcase_flash");
            if (flashSprite == null && !Application.isPlaying)
            {
                var auto = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/star_flash_alpha.png");
                if (auto != null) flashSprite = auto;
            }
        }
#endif

        private T FindDeep<T>(string name) where T : Component
        {
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == name) return all[i].GetComponent<T>();
            }
            return null;
        }
    }
}
