using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>View model for a single accumulated reward row.</summary>
    public readonly struct RewardRowData
    {
        public readonly string Id;
        public readonly Sprite Icon;
        public readonly string Label;
        public readonly long Amount;

        public RewardRowData(string id, Sprite icon, string label, long amount)
        {
            Id = id;
            Icon = icon;
            Label = label;
            Amount = amount;
        }

        public RewardRowData(Sprite icon, string label, long amount) : this(null, icon, label, amount) { }
    }

    /// <summary>
    /// Builds and recycles reward rows at runtime so the accumulated rewards panel stays dynamic.
    /// Rows are created in code (no prefab dependency) and pooled. Each row is keyed by reward id,
    /// so when a new reward is inserted the rows below animate into their new slots.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RewardsListView : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text emptyLabel;
        [Tooltip("Optional row prefab. When set, rows are cloned from it instead of built in code.")]
        [SerializeField] private GameObject rowPrefab;
        [SerializeField] private float rowHeight = 72f;
        [SerializeField] private float iconSize = 56f;

        [Header("Insert animation")]
        [Tooltip("How long rows below a newly added reward take to slide into their new slots.")]
        [SerializeField, Min(0f)] private float shiftDuration = 0.3f;
        [SerializeField] private Ease shiftEase = Ease.OutCubic;
        [Tooltip("How long a newly added row takes to fade/scale in.")]
        [SerializeField, Min(0f)] private float appearDuration = 0.25f;
        [Tooltip("Scale a new row starts from as it pops in.")]
        [SerializeField, Range(0.5f, 1f)] private float appearScaleFrom = 0.94f;

        private readonly Dictionary<string, RowWidgets> _active = new Dictionary<string, RowWidgets>();
        private readonly List<RowWidgets> _pool = new List<RowWidgets>();
        private readonly List<string> _retire = new List<string>();

        /// <summary>The visible list rect, used as a fallback target for reward fly animations.</summary>
        public RectTransform ListRect => (RectTransform)transform;

        /// <summary>True when an active row for <paramref name="id"/> exists; returns its icon rect.</summary>
        public bool TryGetRowIcon(string id, out RectTransform icon)
        {
            icon = null;
            if (string.IsNullOrEmpty(id)) return false;
            if (!_active.TryGetValue(id, out var row) || row == null || row.Icon == null) return false;
            if (row.Root == null || !row.Root.activeSelf) return false;

            icon = (RectTransform)row.Icon.transform;
            return true;
        }

        /// <summary>Plays a short scale punch on a row's icon (e.g. when a flown reward lands).</summary>
        public void PunchRow(string id)
        {
            if (!TryGetRowIcon(id, out var icon)) return;
            icon.DOKill();
            icon.localScale = Vector3.one;
            icon.DOPunchScale(Vector3.one * 0.2f, 0.3f, 8, 0.6f).SetUpdate(true);
        }

        /// <summary>
        /// World position the icon of the row at <paramref name="index"/> will occupy. Lets a reward
        /// be flown to its final slot before that row exists (first time a reward type is earned).
        /// Mirrors the layout used by <see cref="CreateRow"/> / <see cref="CreateRowFromPrefab"/>:
        /// rows sit at y = -index * rowHeight and the icon is inset from the row's left edge.
        /// Targets the icon's pivot (its left edge), matching <see cref="TryGetRowIcon"/> so repeat
        /// and first-time rewards land in the same spot.
        /// </summary>
        public bool TryGetSlotIconWorldPosition(int index, out Vector3 worldPosition)
        {
            var parent = content != null ? content : (RectTransform)transform;
            var rect = parent.rect;

            const float iconLeftInset = 6f;
            float x = rect.xMin + iconLeftInset;
            float y = rect.yMax - index * rowHeight - rowHeight * 0.5f;

            worldPosition = parent.TransformPoint(new Vector3(x, y, 0f));
            return true;
        }

        private sealed class RowWidgets
        {
            public GameObject Root;
            public RectTransform Rect;
            public Image Icon;
            public TMP_Text Amount;
            public CanvasGroup Group;
        }

        public void SetRows(IReadOnlyList<RewardRowData> data)
        {
            int count = data?.Count ?? 0;

            RetireMissingRows(data, count);

            for (int i = 0; i < count; i++)
            {
                var row = data[i];
                string id = string.IsNullOrEmpty(row.Id) ? "row_" + i : row.Id;

                bool isNew = !_active.TryGetValue(id, out var widgets);
                if (isNew)
                {
                    widgets = Acquire(id);
                    _active[id] = widgets;
                }

                widgets.Root.SetActive(true);
                if (widgets.Icon != null) widgets.Icon.sprite = row.Icon;
                if (widgets.Amount != null)
                    widgets.Amount.text = row.Amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

                float targetY = -(i * rowHeight);

                if (isNew)
                {
                    // New reward: sit in its final slot and pop in while the rows below slide down.
                    widgets.Rect.DOKill();
                    widgets.Rect.anchoredPosition = new Vector2(0f, targetY);
                    PlayAppear(widgets);
                }
                else if (!Mathf.Approximately(widgets.Rect.anchoredPosition.y, targetY))
                {
                    // Existing reward: ease into the new slot so the list opens up gracefully.
                    widgets.Rect.DOKill();
                    widgets.Rect.DOAnchorPosY(targetY, shiftDuration).SetEase(shiftEase).SetUpdate(true);
                }
            }

            // Grow the content so the list can scroll when it overflows the viewport.
            if (content != null)
                content.sizeDelta = new Vector2(content.sizeDelta.x, count * rowHeight);

            if (emptyLabel != null)
                emptyLabel.gameObject.SetActive(count == 0);
        }

        private void RetireMissingRows(IReadOnlyList<RewardRowData> data, int count)
        {
            if (_active.Count == 0) return;

            _retire.Clear();
            foreach (var pair in _active)
            {
                if (!ContainsId(data, count, pair.Key)) _retire.Add(pair.Key);
            }

            for (int i = 0; i < _retire.Count; i++)
                Retire(_retire[i]);
        }

        private static bool ContainsId(IReadOnlyList<RewardRowData> data, int count, string id)
        {
            for (int i = 0; i < count; i++)
            {
                string candidate = string.IsNullOrEmpty(data[i].Id) ? "row_" + i : data[i].Id;
                if (string.Equals(candidate, id, System.StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private void PlayAppear(RowWidgets row)
        {
            if (appearDuration <= 0f)
            {
                if (row.Group != null) row.Group.alpha = 1f;
                row.Rect.localScale = Vector3.one;
                return;
            }

            if (row.Group != null)
            {
                row.Group.DOKill();
                row.Group.alpha = 0f;
                row.Group.DOFade(1f, appearDuration).SetUpdate(true);
            }

            row.Rect.localScale = Vector3.one * appearScaleFrom;
            row.Rect.DOScale(Vector3.one, appearDuration).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private RowWidgets Acquire(string id)
        {
            RowWidgets widgets;

            if (_pool.Count > 0)
            {
                widgets = _pool[_pool.Count - 1];
                _pool.RemoveAt(_pool.Count - 1);
            }
            else
            {
                var parent = content != null ? content : (RectTransform)transform;
                widgets = CreateRow(parent);
            }

            widgets.Root.name = "ui_reward_row_" + id + "_value";
            widgets.Root.transform.SetAsLastSibling();

            // Reset any leftover animation state from a previous life in the pool.
            widgets.Rect.DOKill();
            widgets.Rect.localScale = Vector3.one;
            if (widgets.Group != null)
            {
                widgets.Group.DOKill();
                widgets.Group.alpha = 1f;
            }

            return widgets;
        }

        private void Retire(string id)
        {
            if (!_active.TryGetValue(id, out var widgets)) return;
            _active.Remove(id);
            if (widgets == null) return;

            widgets.Rect.DOKill();
            if (widgets.Group != null) widgets.Group.DOKill();
            widgets.Root.SetActive(false);
            _pool.Add(widgets);
        }

        private RowWidgets CreateRow(RectTransform parent)
        {
            if (rowPrefab != null)
                return CreateRowFromPrefab(parent);

            var rowGo = new GameObject("ui_reward_row_value", typeof(RectTransform));
            rowGo.layer = gameObject.layer;
            rowGo.transform.SetParent(parent, false);

            var rowRect = (RectTransform)rowGo.transform;
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(0f, rowHeight);

            var iconGo = new GameObject("ui_image_reward_icon", typeof(RectTransform), typeof(Image));
            iconGo.layer = gameObject.layer;
            iconGo.transform.SetParent(rowGo.transform, false);
            var icon = iconGo.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            iconRect.anchoredPosition = new Vector2(6f, 0f);

            var amountGo = new GameObject("ui_text_reward_amount_value", typeof(RectTransform));
            amountGo.layer = gameObject.layer;
            amountGo.transform.SetParent(rowGo.transform, false);
            var amount = amountGo.AddComponent<TextMeshProUGUI>();
            amount.text = "0";
            amount.fontSize = 38f;
            amount.alignment = TextAlignmentOptions.Left;
            amount.color = Color.white;
            amount.raycastTarget = false;
            var amountRect = (RectTransform)amountGo.transform;
            amountRect.anchorMin = new Vector2(0f, 0f);
            amountRect.anchorMax = new Vector2(1f, 1f);
            amountRect.pivot = new Vector2(0f, 0.5f);
            amountRect.offsetMin = new Vector2(iconSize + 16f, 0f);
            amountRect.offsetMax = new Vector2(-8f, 0f);

            PrepareWidgets(icon, amount);

            return new RowWidgets
            {
                Root = rowGo,
                Rect = rowRect,
                Icon = icon,
                Amount = amount,
                Group = rowGo.AddComponent<CanvasGroup>()
            };
        }

        private RowWidgets CreateRowFromPrefab(RectTransform parent)
        {
            var instance = Instantiate(rowPrefab, parent, false);
            instance.name = "ui_reward_row_value";
            instance.layer = gameObject.layer;

            var icon = instance.GetComponentInChildren<Image>(true);
            var amount = instance.GetComponentInChildren<TMP_Text>(true);

            PrepareWidgets(icon, amount);

            return new RowWidgets
            {
                Root = instance,
                Rect = (RectTransform)instance.transform,
                Icon = icon,
                Amount = amount,
                Group = instance.GetComponent<CanvasGroup>() ?? instance.AddComponent<CanvasGroup>()
            };
        }

        /// <summary>
        /// Keeps row widgets consistent regardless of where they came from (prefab or code):
        /// non-interactive, but masked so the viewport's RectMask2D clips them.
        /// </summary>
        private static void PrepareWidgets(Image icon, TMP_Text amount)
        {
            if (icon != null)
            {
                icon.raycastTarget = false;
                icon.maskable = true;
            }

            if (amount != null)
            {
                amount.raycastTarget = false;
                if (amount is MaskableGraphic maskable) maskable.maskable = true;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (content == null)
            {
                var t = transform.Find("ui_rewards_list_content");
                if (t != null) content = (RectTransform)t;
            }

            if (emptyLabel == null)
            {
                var t = transform.Find("ui_text_rewards_empty_value");
                if (t != null) emptyLabel = t.GetComponent<TMP_Text>();
            }
        }
#endif
    }
}