using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// End-of-run summary. Lists every banked reward as a pooled tile inside a horizontally
    /// scrolling, masked viewport and fades in/out with its panel animation profile.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultsView : MonoBehaviour, IResultsInput
    {
        #region Configuration and Runtime

        private const float SCROLL_START = 0f;
        /// <summary>Half of the free space goes on each side of a centred row.</summary>
        private const float CENTRE_OFFSET = 0.5f;
        private const string TILE_NAME_PREFIX = "ui_panel_result_reward_";

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _content;
        [SerializeField] private ScrollRect _scroll;
        [SerializeField] private UIButton _restartButton;
        [SerializeField] private CanvasGroup[] _underlyingHud = new CanvasGroup[0];
        [SerializeField] private ResultTileView _tilePrefab;
        [SerializeField, Min(1)] private int _poolCapacity = 32;
        [SerializeField] private PanelAnimation _animation;

        private readonly List<ResultTileView> _tiles = new List<ResultTileView>();
        private Tween _fade;
        private ModalHudVisibility _hudVisibility;
        private float _tileWidth;

        public event Action RestartRequested;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _hudVisibility = new ModalHudVisibility(_underlyingHud);
            _tileWidth = ((RectTransform)_tilePrefab.transform).rect.width;
            for (int i = 0; i < _poolCapacity; i++)
            {
                ResultTileView tile = Instantiate(_tilePrefab, _content, false);
                tile.name = TILE_NAME_PREFIX + i;
                tile.gameObject.SetActive(false);
                _tiles.Add(tile);
            }
            ApplyHiddenState();
        }

        private void OnEnable()
        {
            _restartButton.Clicked += OnRestartClicked;
        }

        private void OnDisable()
        {
            _restartButton.Clicked -= OnRestartClicked;
            HideImmediate();
        }

        private void OnDestroy()
        {
            KillFade();
        }

        private void OnRestartClicked()
        {
            Hide();
            if (RestartRequested != null)
            {
                RestartRequested.Invoke();
            }
        }

        #endregion

        #region Visibility

        /// <summary>Populates the summary with the banked rewards and fades the screen in.</summary>
        public void Show(IReadOnlyList<RewardRowData> rewards)
        {
            BuildTiles(rewards);
            if (_scroll.horizontal)
            {
                _scroll.horizontalNormalizedPosition = SCROLL_START;
            }
            _hudVisibility.Hide();
            _group.blocksRaycasts = true;
            _group.interactable = true;
            FadeTo(_animation.VisibleAlpha);
        }

        public void Hide()
        {
            _group.interactable = false;
            FadeTo(_animation.HiddenAlpha);
            _fade.OnComplete(OnHideComplete);
        }

        public void HideImmediate()
        {
            KillFade();
            ApplyHiddenState();
            _hudVisibility.Restore();
        }

        private void OnHideComplete()
        {
            _fade = null;
            ApplyHiddenState();
            _hudVisibility.Restore();
        }

        private void ApplyHiddenState()
        {
            _group.alpha = _animation.HiddenAlpha;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void FadeTo(float alpha)
        {
            KillFade();
            _fade = _group.DOFade(alpha, _animation.FadeDuration).SetEase(_animation.FadeEase).SetUpdate(true).SetLink(gameObject);
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

        #region Tiles

        private void BuildTiles(IReadOnlyList<RewardRowData> rewards)
        {
            int count = rewards.Count;
            if (count > _tiles.Count)
            {
                throw new InvalidOperationException("Results tile pool exhausted; configure capacity for the reward catalogue.");
            }

            for (int i = 0; i < count; i++)
            {
                ResultTileView tile = _tiles[i];
                RewardRowData data = rewards[i];
                tile.Rect.anchoredPosition = new Vector2(i * _tileWidth, 0f);
                tile.Icon.sprite = data.Icon;
                tile.Icon.enabled = data.Icon;
                tile.Count.text = RewardAmountFormatter.Format(data.Amount);
                tile.gameObject.SetActive(true);
            }
            for (int i = count; i < _tiles.Count; i++)
            {
                _tiles[i].gameObject.SetActive(false);
            }

            // Grow the content so longer runs scroll; a row that fits is centred and does not scroll.
            float rowWidth = count * _tileWidth;
            float viewportWidth = _scroll.viewport.rect.width;
            bool isScrollable = rowWidth > viewportWidth;
            _content.sizeDelta = new Vector2(rowWidth, _content.sizeDelta.y);
            _content.anchoredPosition = new Vector2(isScrollable ? 0f : (viewportWidth - rowWidth) * CENTRE_OFFSET, 0f);
            _scroll.horizontal = isScrollable;
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_group) _group = GetComponent<CanvasGroup>();
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (!_content && child.name == "ui_layout_results_content") _content = (RectTransform)child;
                if (!_scroll && child.name == "ui_scroll_results") _scroll = child.GetComponent<ScrollRect>();
                if (!_restartButton && child.name == "ui_button_results_restart") _restartButton = child.GetComponent<UIButton>();
            }
            if (!_group || !_content || !_scroll || !_restartButton || !_tilePrefab || !_animation)
            {
                Debug.LogError("ResultsView has missing serialized references.", this);
            }
        }

        #endregion
#endif
    }
}
