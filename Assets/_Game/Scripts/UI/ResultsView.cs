using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>Scroll direction of the results list.</summary>
    public enum ResultsLayout
    {
        Vertical,
        Horizontal
    }

    /// <summary>
    /// End-of-run summary screen. Lists every banked reward as a large icon with its total count
    /// underneath inside a scrollable, mask-based viewport (no scrollbar). Fades in/out with DOTween.
    /// Tiles are pooled and built in code (or from an optional prefab) so the list stays dynamic.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultsView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private UIButton restartButton;
        [Tooltip("Optional tile prefab. When set, tiles are cloned from it instead of built in code.")]
        [SerializeField] private GameObject tilePrefab;

        [Header("Layout")]
        [SerializeField] private ResultsLayout layout = ResultsLayout.Horizontal;
        [Tooltip("Row height in vertical layout.")]
        [SerializeField] private float tileHeight = 250f;
        [Tooltip("Tile width in horizontal layout.")]
        [SerializeField] private float tileWidth = 220f;
        [SerializeField] private float iconSize = 150f;
        [SerializeField] private float iconTopPadding = 16f;
        [SerializeField] private float countGap = 8f;
        [SerializeField] private float countWidth = 360f;
        [SerializeField] private float countHeight = 66f;
        [SerializeField] private float countFontSize = 46f;
        [SerializeField] private float fadeDuration = 0.3f;

        [Header("Text")]
        [SerializeField] private string title = "YOU GOT:";

        /// <summary>Raised when the player presses the restart button on the summary.</summary>
        public event Action RestartRequested;

        private readonly List<TileWidgets> _tiles = new List<TileWidgets>();
        private Tween _fade;

        private sealed class TileWidgets
        {
            public GameObject Root;
            public Image Icon;
            public TMP_Text Count;
        }

        private bool IsHorizontal => layout == ResultsLayout.Horizontal;

        private void Awake()
        {
            ApplyHiddenState();
            if (restartButton != null) restartButton.Clicked += OnRestartClicked;
        }

        private void OnDestroy()
        {
            if (restartButton != null) restartButton.Clicked -= OnRestartClicked;
        }

        private void OnRestartClicked()
        {
            Hide();
            RestartRequested?.Invoke();
        }

        /// <summary>Populates the summary with the banked rewards and fades the screen in.</summary>
        public void Show(IReadOnlyList<RewardRowData> rewards)
        {
            if (titleLabel != null) titleLabel.text = title;

            ApplyScrollConfiguration();
            BuildTiles(rewards);

            if (scroll != null)
            {
                if (IsHorizontal) scroll.horizontalNormalizedPosition = 0f;
                else scroll.verticalNormalizedPosition = 1f;
            }

            SetVisible(true);
        }

        public void Hide() => SetVisible(false);

        private void ApplyHiddenState()
        {
            if (group == null) return;

            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void SetVisible(bool visible)
        {
            if (group == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            group.blocksRaycasts = visible;
            group.interactable = visible;

            _fade?.Kill();
            _fade = group.DOFade(visible ? 1f : 0f, fadeDuration).SetUpdate(true);
        }

        /// <summary>Points the ScrollRect and content rect at the configured axis.</summary>
        private void ApplyScrollConfiguration()
        {
            bool horizontal = IsHorizontal;

            if (scroll != null)
            {
                scroll.horizontal = horizontal;
                scroll.vertical = !horizontal;
            }

            if (content == null) return;

            if (horizontal)
            {
                content.anchorMin = new Vector2(0f, 0f);
                content.anchorMax = new Vector2(0f, 1f);
                content.pivot = new Vector2(0f, 0.5f);
            }
            else
            {
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(0.5f, 1f);
            }

            content.anchoredPosition = Vector2.zero;
        }

        private void BuildTiles(IReadOnlyList<RewardRowData> rewards)
        {
            int count = rewards?.Count ?? 0;
            EnsureTileCount(count);

            bool horizontal = IsHorizontal;

            for (int i = 0; i < count; i++)
            {
                var tile = _tiles[i];
                var data = rewards[i];

                var rect = (RectTransform)tile.Root.transform;
                rect.anchoredPosition = horizontal
                    ? new Vector2(i * tileWidth, 0f)
                    : new Vector2(0f, -(i * tileHeight));

                if (tile.Icon != null)
                {
                    tile.Icon.sprite = data.Icon;
                    tile.Icon.enabled = data.Icon != null;
                }

                if (tile.Count != null) tile.Count.text = data.Amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

                tile.Root.SetActive(true);
            }

            for (int i = count; i < _tiles.Count; i++)
                _tiles[i].Root.SetActive(false);

            // Grow the content along the scroll axis so the list can overflow the viewport.
            if (content != null)
            {
                content.sizeDelta = horizontal
                    ? new Vector2(count * tileWidth, content.sizeDelta.y)
                    : new Vector2(content.sizeDelta.x, count * tileHeight);
            }
        }

        private void EnsureTileCount(int count)
        {
            var parent = content != null ? content : (RectTransform)transform;
            while (_tiles.Count < count)
                _tiles.Add(CreateTile(parent, _tiles.Count));
        }

        private TileWidgets CreateTile(RectTransform parent, int index)
        {
            if (tilePrefab != null)
                return CreateTileFromPrefab(parent, index);

            bool horizontal = IsHorizontal;

            var rootGo = new GameObject("ui_result_reward_" + (index + 1) + "_value", typeof(RectTransform));
            rootGo.layer = gameObject.layer;
            rootGo.transform.SetParent(parent, false);

            var rootRect = (RectTransform)rootGo.transform;
            if (horizontal)
            {
                rootRect.anchorMin = new Vector2(0f, 0f);
                rootRect.anchorMax = new Vector2(0f, 1f);
                rootRect.pivot = new Vector2(0f, 0.5f);
                rootRect.sizeDelta = new Vector2(tileWidth, 0f);
            }
            else
            {
                rootRect.anchorMin = new Vector2(0f, 1f);
                rootRect.anchorMax = new Vector2(1f, 1f);
                rootRect.pivot = new Vector2(0.5f, 1f);
                rootRect.sizeDelta = new Vector2(0f, tileHeight);
            }

            // Vertical layout stacks from the top; horizontal centres the icon/count pair vertically.
            float iconY = horizontal ? (countHeight + countGap) * 0.5f : -iconTopPadding;
            float countY = horizontal
                ? -(iconSize + countGap) * 0.5f
                : -(iconTopPadding + iconSize + countGap);

            var iconGo = new GameObject("ui_image_result_reward_icon", typeof(RectTransform), typeof(Image));
            iconGo.layer = gameObject.layer;
            iconGo.transform.SetParent(rootGo.transform, false);

            var icon = iconGo.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.maskable = true;
            icon.preserveAspect = true;

            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = new Vector2(0.5f, horizontal ? 0.5f : 1f);
            iconRect.anchorMax = new Vector2(0.5f, horizontal ? 0.5f : 1f);
            iconRect.pivot = new Vector2(0.5f, horizontal ? 0.5f : 1f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            iconRect.anchoredPosition = new Vector2(0f, iconY);

            var countGo = new GameObject("ui_text_result_reward_count_value", typeof(RectTransform));
            countGo.layer = gameObject.layer;
            countGo.transform.SetParent(rootGo.transform, false);

            var count = countGo.AddComponent<TextMeshProUGUI>();
            count.text = "0";
            count.fontSize = countFontSize;
            count.fontStyle = FontStyles.Bold;
            count.alignment = TextAlignmentOptions.Center;
            count.color = Color.white;
            count.raycastTarget = false;
            count.textWrappingMode = TextWrappingModes.NoWrap;
            count.overflowMode = TextOverflowModes.Overflow;

            var countRect = (RectTransform)countGo.transform;
            countRect.anchorMin = new Vector2(0.5f, horizontal ? 0.5f : 1f);
            countRect.anchorMax = new Vector2(0.5f, horizontal ? 0.5f : 1f);
            countRect.pivot = new Vector2(0.5f, horizontal ? 0.5f : 1f);
            countRect.sizeDelta = new Vector2(horizontal ? Mathf.Max(countWidth, tileWidth) : countWidth, countHeight);
            countRect.anchoredPosition = new Vector2(0f, countY);

            return new TileWidgets
            {
                Root = rootGo,
                Icon = icon,
                Count = count
            };
        }

        private TileWidgets CreateTileFromPrefab(RectTransform parent, int index)
        {
            var instance = Instantiate(tilePrefab, parent, false);
            instance.name = "ui_result_reward_" + (index + 1) + "_value";
            instance.layer = gameObject.layer;

            return new TileWidgets
            {
                Root = instance,
                Icon = instance.GetComponentInChildren<Image>(true),
                Count = instance.GetComponentInChildren<TMP_Text>(true)
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (content == null) content = FindDeep<RectTransform>("ui_results_content");
            if (scroll == null) scroll = FindDeep<ScrollRect>("ui_results_scroll");
            if (titleLabel == null) titleLabel = FindDeep<TMP_Text>("ui_text_results_title_value");
            if (restartButton == null) restartButton = FindDeep<UIButton>("ui_button_results_restart");

            if (!Application.isPlaying) ApplyScrollConfiguration();
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
