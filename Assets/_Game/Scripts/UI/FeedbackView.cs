using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Blocking loss panel: bomb card, recoverable-reward preview, paid revive and free restart.
    /// A dim overlay leaves rounded spotlights over the death card and the reward preview.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FeedbackView : MonoBehaviour, ILossPanelInput
    {
        #region Configuration and Runtime

        private static readonly int DIM_SIZE_ID = Shader.PropertyToID("_DimSize");
        private static readonly int CUTOUT_A_ID = Shader.PropertyToID("_CutoutA");
        private static readonly int CUTOUT_B_ID = Shader.PropertyToID("_CutoutB");
        /// <summary>Converts a rect size into the half extents the cutout shader expects.</summary>
        private const float HALF_EXTENT = 0.5f;
        private const int SINGLE_REVIVE = 1;

        [SerializeField] private CanvasGroup _group;

        [Header("Death card")]
        [SerializeField] private RectTransform _deathScreen;
        [SerializeField] private Image _deathIcon;
        [SerializeField] private UIButton _restartButton;
        [SerializeField] private UIButton _reviveButton;
        [SerializeField] private TMP_Text _reviveLabel;
        [SerializeField] private TMP_Text _reviveStatus;

        [Header("Recoverable rewards")]
        [SerializeField] private GameObject _recoverableRewardsPanel;
        [SerializeField] private RewardsListView _recoverableRewardsList;

        [Header("Death spotlight")]
        [Tooltip("Full-screen dim with rounded cut-outs (uses the UI Dim Cutout shader).")]
        [SerializeField] private Image _dimOverlay;
        [Tooltip("Red damage vignette shown with the death screen.")]
        [SerializeField] private Image _damageGlow;
        [Tooltip("Rects the dim leaves clear (the rewards pane and the death card).")]
        [SerializeField] private RectTransform _spotlightA;
        [SerializeField] private RectTransform _spotlightB;
        [Tooltip("Shrinks each cut-out so it stays inside the opaque part of a framed panel, in canvas units.")]
        [SerializeField, Min(0f)] private float _spotlightInset = 8f;

        [Header("Copy")]
        [SerializeField] private string _reviveLimitReachedText = "Revive limit reached — restart for free";
        [SerializeField] private string _singleReviveRemainingText = "1 revive remaining";
        [Tooltip("{0} is the number of revives left.")]
        [SerializeField] private string _revivesRemainingFormat = "{0} revives remaining";
        [Tooltip("{0} is the remaining-revives text.")]
        [SerializeField] private string _insufficientFundsFormat = "Not enough gold — {0}";

        [SerializeField] private PanelAnimation _animation;
        [SerializeField] private CanvasGroup[] _underlyingHud = new CanvasGroup[0];

        private Tween _fade;
        private Material _dimMaterial;
        private ModalHudVisibility _hudVisibility;
        private int _revivesRemaining;

        #endregion

        #region Events

        public event Action RestartRequested;
        public event Action ReviveRequested;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _hudVisibility = new ModalHudVisibility(_underlyingHud);
            _group.alpha = _animation.HiddenAlpha;
            SetBlocking(false);
        }

        private void OnEnable()
        {
            _restartButton.Clicked += OnRestartClicked;
            _reviveButton.Clicked += OnReviveClicked;
        }

        private void OnDisable()
        {
            _restartButton.Clicked -= OnRestartClicked;
            _reviveButton.Clicked -= OnReviveClicked;
            HideImmediate();
        }

        private void OnDestroy()
        {
            KillFade();
            if (_dimMaterial)
            {
                Destroy(_dimMaterial);
            }
        }

        /// <summary>Keeps the spotlight cut-outs aligned when the screen size or orientation changes.</summary>
        private void OnRectTransformDimensionsChange()
        {
            if (_dimOverlay && _dimOverlay.gameObject.activeInHierarchy)
            {
                ApplySpotlight();
            }
        }

        private void OnRestartClicked()
        {
            Hide();
            if (RestartRequested != null)
            {
                RestartRequested.Invoke();
            }
        }

        private void OnReviveClicked()
        {
            if (ReviveRequested != null)
            {
                ReviveRequested.Invoke();
            }
        }

        #endregion

        #region Loss Panel

        /// <summary>Blocking loss message that requires the player to restart or revive.</summary>
        public void ShowLoss(Sprite bombIcon)
        {
            _hudVisibility.Hide();
            SetLossWidgetsActive(true);
            _deathIcon.sprite = bombIcon;
            _deathIcon.enabled = bombIcon;
            ApplySpotlight();
            SetBlocking(true);
            FadeTo(_animation.VisibleAlpha);
        }

        public void SetRecoverableRewards(IReadOnlyList<RewardRowData> rows)
        {
            _recoverableRewardsList.SetRows(rows);
        }

        public void SetReviveOffer(long cost, int remaining)
        {
            _revivesRemaining = remaining;
            _reviveButton.gameObject.SetActive(true);
            _reviveButton.IsInteractable = remaining > 0;
            _reviveLabel.text = RewardAmountFormatter.Format(cost);
            _reviveStatus.text = remaining > 0 ? GetRemainingRevivesText() : _reviveLimitReachedText;
        }

        public void ShowInsufficientFunds()
        {
            _reviveStatus.text = string.Format(_insufficientFundsFormat, GetRemainingRevivesText());
        }

        private string GetRemainingRevivesText()
        {
            return _revivesRemaining == SINGLE_REVIVE
                ? _singleReviveRemainingText
                : string.Format(_revivesRemainingFormat, _revivesRemaining);
        }

        public void Hide()
        {
            KillFade();
            _fade = _group.DOFade(_animation.HiddenAlpha, _animation.FadeDuration)
                .SetEase(_animation.FadeEase).SetUpdate(true).SetLink(gameObject)
                .OnComplete(FinishHide);
        }

        private void FinishHide()
        {
            _fade = null;
            SetBlocking(false);
            SetLossWidgetsActive(false);
            _hudVisibility.Restore();
        }

        public void HideImmediate()
        {
            KillFade();
            SetBlocking(false);
            SetLossWidgetsActive(false);
            _hudVisibility.Restore();
            _group.alpha = _animation.HiddenAlpha;
        }

        #endregion

        #region Presentation Helpers

        private void SetLossWidgetsActive(bool isActive)
        {
            _recoverableRewardsPanel.SetActive(isActive);
            _deathScreen.gameObject.SetActive(isActive);
            _dimOverlay.gameObject.SetActive(isActive);
            // Toggle the graphic, not its GameObject, so the generated vignette is not rebuilt each loss.
            _damageGlow.enabled = isActive;
        }

        /// <summary>Punches rounded cut-outs for the rewards pane and the death card into the dim.</summary>
        private void ApplySpotlight()
        {
            if (!_dimMaterial)
            {
                _dimMaterial = new Material(_dimOverlay.material);
                _dimOverlay.material = _dimMaterial;
            }

            RectTransform dimRect = (RectTransform)_dimOverlay.transform;
            Vector2 size = dimRect.rect.size;
            _dimMaterial.SetVector(DIM_SIZE_ID, new Vector4(size.x, size.y, 0f, 0f));
            _dimMaterial.SetVector(CUTOUT_A_ID, GetCutout(_spotlightA, dimRect, _spotlightInset));
            _dimMaterial.SetVector(CUTOUT_B_ID, GetCutout(_spotlightB, dimRect, _spotlightInset));
        }

        private static Vector4 GetCutout(RectTransform target, RectTransform dimRect, float inset)
        {
            // Use the rect centre, not the pivot, so panes with edge pivots are spotlit where they are.
            Vector3 local = dimRect.InverseTransformPoint(target.TransformPoint(target.rect.center));
            Vector2 halfSize = target.rect.size * HALF_EXTENT;
            return new Vector4(local.x, local.y, Mathf.Max(0f, halfSize.x - inset), Mathf.Max(0f, halfSize.y - inset));
        }

        private void SetBlocking(bool isBlocking)
        {
            _group.blocksRaycasts = isBlocking;
            _group.interactable = isBlocking;
        }

        private void FadeTo(float target)
        {
            KillFade();
            _fade = _group.DOFade(target, _animation.FadeDuration).SetEase(_animation.FadeEase).SetUpdate(true).SetLink(gameObject);
        }

        private void KillFade()
        {
            if (_fade != null)
            {
                _fade.Kill(false);
            }
            _fade = null;
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_group) _group = GetComponent<CanvasGroup>();
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (!_deathScreen && child.name == "ui_image_feedback_death_screen") _deathScreen = (RectTransform)child;
                if (!_deathIcon && child.name == "ui_image_feedback_death_value") _deathIcon = child.GetComponent<Image>();
                if (!_restartButton && child.name == "ui_button_feedback_action") _restartButton = child.GetComponent<UIButton>();
                if (!_dimOverlay && child.name == "ui_image_feedback_dim") _dimOverlay = child.GetComponent<Image>();
                if (!_damageGlow && child.name == "ui_image_feedback_damage_glow") _damageGlow = child.GetComponent<Image>();
            }
            if (!_recoverableRewardsPanel && _recoverableRewardsList) _recoverableRewardsPanel = _recoverableRewardsList.transform.parent.gameObject;
            if (!_group || !_deathScreen || !_deathIcon || !_restartButton || !_reviveButton ||
                !_reviveLabel || !_reviveStatus || !_recoverableRewardsPanel || !_recoverableRewardsList || !_dimOverlay ||
                !_damageGlow || !_spotlightA || !_spotlightB || !_animation)
            {
                Debug.LogError("FeedbackView has missing serialized references.", this);
            }
        }

        #endregion
#endif
    }
}
