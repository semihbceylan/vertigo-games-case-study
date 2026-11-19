using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>Authored modal reward showcase. Completion is emitted only after dismissal finishes.</summary>
    [DisallowMultipleComponent]
    public sealed class JackpotShowcaseView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button _dismissButton;
        [SerializeField] private Image _flash;
        [SerializeField] private Image _bigIcon;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _amountLabel;
        [SerializeField] private TMP_Text _hintLabel;
        [SerializeField] private ShowcaseAnimation _animation;
        [SerializeField] private CanvasGroup[] _underlyingHud = new CanvasGroup[0];
        [SerializeField] private string _hintText = "TAP ANYWHERE TO CONTINUE";
        [SerializeField] private string _fallbackTitle = "JACKPOT!";

        public event Action Dismissed;
        public bool IsShowing { get; private set; }

        private Tween _fade;
        private Tween _pop;
        private Tween _spin;
        private ModalHudVisibility _hudVisibility;

        private void Awake()
        {
            _hudVisibility = new ModalHudVisibility(_underlyingHud);
            ApplyHiddenImmediate();
        }

        private void OnEnable()
        {
            if (_dismissButton) _dismissButton.onClick.AddListener(OnDismissClicked);
        }

        private void OnDisable()
        {
            if (_dismissButton) _dismissButton.onClick.RemoveListener(OnDismissClicked);
            HideImmediate();
        }

        private void OnDestroy()
        {
            HideImmediate();
        }

        public void Show(Sprite icon, string label, long amount)
        {
            KillTweens();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _hudVisibility.Hide();
            _bigIcon.sprite = icon;
            _bigIcon.enabled = icon;
            _titleLabel.text = string.IsNullOrEmpty(label) ? _fallbackTitle : label;
            _amountLabel.text = "x" + RewardAmountFormatter.Format(amount);
            _hintLabel.text = _hintText;
            IsShowing = true;
            _group.blocksRaycasts = true;
            _group.interactable = true;
            _dismissButton.interactable = true;
            _group.alpha = _animation.HiddenAlpha;
            _fade = _group.DOFade(_animation.VisibleAlpha, _animation.FadeDuration)
                .SetEase(_animation.FadeEase).SetUpdate(true).SetLink(gameObject);
            _bigIcon.rectTransform.localScale = Vector3.one * _animation.InitialScale;
            _pop = _bigIcon.rectTransform.DOScale(Vector3.one * _animation.TargetScale, _animation.PopDuration)
                .SetEase(_animation.PopEase).SetUpdate(true).SetLink(gameObject);
            _flash.rectTransform.localRotation = Quaternion.identity;
            _spin = _flash.rectTransform.DORotate(_animation.SpinRotation, _animation.SpinDuration, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental).SetEase(_animation.SpinEase).SetUpdate(true).SetLink(gameObject);
        }

        public void Hide()
        {
            if (!IsShowing) return;
            IsShowing = false;
            KillTweens();
            _dismissButton.interactable = false;
            // Keep the modal raycast barrier and HUD isolation until the fade has finished.
            _fade = _group.DOFade(_animation.HiddenAlpha, _animation.FadeDuration)
                .SetEase(_animation.FadeEase).SetUpdate(true).SetLink(gameObject).OnComplete(OnDismissComplete);
        }

        /// <summary>Cancellation never raises Dismissed or advances the session.</summary>
        public void HideImmediate()
        {
            IsShowing = false;
            KillTweens();
            ApplyHiddenImmediate();
            _hudVisibility?.Restore();
        }

        private void OnDismissClicked()
        {
            Hide();
        }

        private void OnDismissComplete()
        {
            _fade = null;
            ApplyHiddenImmediate();
            _hudVisibility.Restore();
            Dismissed?.Invoke();
        }

        private void KillTweens()
        {
            _fade?.Kill(false);
            _pop?.Kill(false);
            _spin?.Kill(false);
            _fade = null;
            _pop = null;
            _spin = null;
        }

        private void ApplyHiddenImmediate()
        {
            if (!_group) return;
            _group.alpha = _animation ? _animation.HiddenAlpha : 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_group) _group = GetComponent<CanvasGroup>();
            if (gameObject.activeInHierarchy && (!_group || !_dismissButton || !_flash || !_bigIcon ||
                !_titleLabel || !_amountLabel || !_hintLabel || !_animation))
            {
                Debug.LogError("Showcase requires authored widget references and an animation profile.", this);
            }
        }
#endif
    }
}
