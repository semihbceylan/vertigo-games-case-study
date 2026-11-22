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
    public sealed class JackpotShowcaseView : MonoBehaviour, IRewardShowcase
    {
        #region Configuration and Runtime

        /// <summary>DOTween loop count meaning "repeat until killed".</summary>
        private const int INFINITE_LOOPS = -1;

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button _dismissButton;
        [SerializeField] private Image _flash;
        [SerializeField] private Image _bigIcon;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _amountLabel;
        [SerializeField] private ShowcaseAnimation _animation;
        [SerializeField] private CanvasGroup[] _underlyingHud = new CanvasGroup[0];
        [Tooltip("{0} is the formatted reward amount.")]
        [SerializeField] private string _amountFormat = "x{0}";

        private Tween _fade;
        private Tween _pop;
        private Tween _spin;
        private ModalHudVisibility _hudVisibility;
        private bool _isShowing;

        public event Action Dismissed;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _hudVisibility = new ModalHudVisibility(_underlyingHud);
            ApplyHiddenImmediate();
        }

        private void OnEnable()
        {
            _dismissButton.onClick.AddListener(OnDismissClicked);
        }

        private void OnDisable()
        {
            _dismissButton.onClick.RemoveListener(OnDismissClicked);
            HideImmediate();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        #endregion

        #region Showcase

        public void Show(Sprite icon, string label, long amount)
        {
            KillTweens();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _hudVisibility.Hide();
            _bigIcon.sprite = icon;
            _bigIcon.enabled = icon;
            _titleLabel.text = label;
            _amountLabel.text = string.Format(_amountFormat, RewardAmountFormatter.Format(amount));
            _isShowing = true;
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
                .SetLoops(INFINITE_LOOPS, LoopType.Incremental).SetEase(_animation.SpinEase).SetUpdate(true).SetLink(gameObject);
        }

        public void Hide()
        {
            if (!_isShowing)
            {
                return;
            }
            _isShowing = false;
            KillTweens();
            _dismissButton.interactable = false;
            // Keep the modal raycast barrier and HUD isolation until the fade has finished.
            _fade = _group.DOFade(_animation.HiddenAlpha, _animation.FadeDuration)
                .SetEase(_animation.FadeEase).SetUpdate(true).SetLink(gameObject).OnComplete(OnDismissComplete);
        }

        /// <summary>Cancellation never raises Dismissed or advances the session.</summary>
        public void HideImmediate()
        {
            _isShowing = false;
            KillTweens();
            ApplyHiddenImmediate();
            _hudVisibility.Restore();
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
            if (Dismissed != null)
            {
                Dismissed.Invoke();
            }
        }

        private void KillTweens()
        {
            KillTween(_fade);
            KillTween(_pop);
            KillTween(_spin);
            _fade = null;
            _pop = null;
            _spin = null;
        }

        private static void KillTween(Tween tween)
        {
            if (tween != null)
            {
                tween.Kill(false);
            }
        }

        private void ApplyHiddenImmediate()
        {
            _group.alpha = _animation.HiddenAlpha;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_group) _group = GetComponent<CanvasGroup>();
            if (!_group || !_dismissButton || !_flash || !_bigIcon || !_titleLabel || !_amountLabel || !_animation)
            {
                Debug.LogError("Showcase requires authored widget references and an animation profile.", this);
            }
        }

        #endregion
#endif
    }
}
