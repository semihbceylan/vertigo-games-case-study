using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Vertigo.WheelOfFortune.Data;

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
    }

    /// <summary>
    /// Accumulated rewards panel. Rows are pooled instances of an authored prefab, keyed by reward
    /// id, so when a reward is inserted the rows below ease into their new slots.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RewardsListView : MonoBehaviour, IRewardRowTargets
    {
        #region Configuration and Runtime

        private const string ROW_NAME_PREFIX = "ui_panel_reward_row_";

        [SerializeField] private RectTransform _content;
        [Tooltip("Optional label shown while the list is empty.")]
        [SerializeField] private TMP_Text _emptyLabel;
        [SerializeField] private RewardRowView _rowPrefab;
        [SerializeField, Min(1)] private int _poolCapacity = 32;
        [SerializeField] private RewardListAnimation _animation;
        [SerializeField] private CountUpAnimation _countAnimation;

        private readonly Dictionary<string, RewardRowView> _active = new Dictionary<string, RewardRowView>();
        private readonly List<RewardRowView> _pool = new List<RewardRowView>();
        private readonly List<string> _retire = new List<string>();
        private float _rowHeight;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _rowHeight = _rowPrefab.Rect.rect.height;
            for (int i = 0; i < _poolCapacity; i++)
            {
                RewardRowView row = Instantiate(_rowPrefab, _content, false);
                row.gameObject.SetActive(false);
                _pool.Add(row);
            }
        }

        /// <summary>Settles interrupted rows so a re-enabled list never shows half-animated rows.</summary>
        private void OnDisable()
        {
            foreach (KeyValuePair<string, RewardRowView> pair in _active)
            {
                RewardRowView row = pair.Value;
                // Settle the amount before killing tweens, or the interrupted roll loses its target.
                row.Counter.Complete();
                KillRowTweens(row);
                row.Rect.localScale = Vector3.one;
                row.Group.alpha = _animation.VisibleAlpha;
                row.Icon.rectTransform.localScale = Vector3.one;
            }
            foreach (RewardRowView row in _pool)
            {
                KillRowTweens(row);
            }
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<string, RewardRowView> pair in _active)
            {
                KillRowTweens(pair.Value);
            }
            foreach (RewardRowView row in _pool)
            {
                KillRowTweens(row);
            }
        }

        private static void KillRowTweens(RewardRowView row)
        {
            if (!row)
            {
                return;
            }
            row.Rect.DOKill();
            row.Group.DOKill();
            row.Icon.rectTransform.DOKill();
            row.Counter.Kill();
        }

        #endregion

        #region Fly Targets

        /// <summary>The visible list rect, used as the target when a reward's row does not exist yet.</summary>
        public RectTransform ListRect
        {
            get { return (RectTransform)transform; }
        }

        /// <summary>True when an active row for <paramref name="id"/> exists; returns its icon rect.</summary>
        public bool TryGetRowIcon(string id, out RectTransform icon)
        {
            icon = null;
            RewardRowView row;
            if (string.IsNullOrEmpty(id) || !_active.TryGetValue(id, out row) || !row.gameObject.activeSelf)
            {
                return false;
            }
            icon = row.Icon.rectTransform;
            return true;
        }

        /// <summary>Plays a short scale punch on a row's icon when a flown reward lands.</summary>
        public void PunchRow(string id)
        {
            RectTransform icon;
            if (!TryGetRowIcon(id, out icon))
            {
                return;
            }
            icon.DOKill();
            icon.localScale = Vector3.one;
            icon.DOPunchScale(Vector3.one * _animation.PunchScale, _animation.PunchDuration,
                _animation.PunchVibrato, _animation.PunchElasticity).SetUpdate(true).SetLink(gameObject);
        }

        #endregion

        #region Rows

        public void SetRows(IReadOnlyList<RewardRowData> data)
        {
            int count = data.Count;
            RetireMissingRows(data);

            for (int i = 0; i < count; i++)
            {
                RewardRowData rowData = data[i];
                RewardRowView row;
                bool isNew = !_active.TryGetValue(rowData.Id, out row);
                if (isNew)
                {
                    row = Acquire(rowData.Id);
                    _active[rowData.Id] = row;
                }

                row.gameObject.SetActive(true);
                row.Icon.sprite = rowData.Icon;
                if (isNew)
                {
                    row.Counter.Set(rowData.Amount);
                }
                else
                {
                    row.Counter.CountTo(rowData.Amount, _countAnimation);
                }

                float targetY = -(i * _rowHeight);
                if (isNew)
                {
                    // New reward: sit in its final slot and pop in while the rows below slide down.
                    row.Rect.DOKill();
                    row.Rect.anchoredPosition = new Vector2(0f, targetY);
                    PlayAppear(row);
                }
                else if (!Mathf.Approximately(row.Rect.anchoredPosition.y, targetY))
                {
                    row.Rect.DOKill();
                    row.Rect.DOAnchorPosY(targetY, _animation.ShiftDuration).SetEase(_animation.ShiftEase)
                        .SetUpdate(true).SetLink(gameObject);
                }
            }

            // Grow the content so the list can scroll when it overflows the viewport.
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, count * _rowHeight);
            if (_emptyLabel)
            {
                _emptyLabel.gameObject.SetActive(count == 0);
            }
        }

        private void RetireMissingRows(IReadOnlyList<RewardRowData> data)
        {
            if (_active.Count == 0)
            {
                return;
            }
            _retire.Clear();
            foreach (KeyValuePair<string, RewardRowView> pair in _active)
            {
                if (!ContainsId(data, pair.Key))
                {
                    _retire.Add(pair.Key);
                }
            }
            for (int i = 0; i < _retire.Count; i++)
            {
                Retire(_retire[i]);
            }
        }

        private static bool ContainsId(IReadOnlyList<RewardRowData> data, string id)
        {
            for (int i = 0; i < data.Count; i++)
            {
                if (string.Equals(data[i].Id, id, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private void PlayAppear(RewardRowView row)
        {
            row.Group.DOKill();
            row.Group.alpha = _animation.HiddenAlpha;
            row.Group.DOFade(_animation.VisibleAlpha, _animation.AppearDuration).SetUpdate(true).SetLink(gameObject);
            row.Rect.localScale = Vector3.one * _animation.AppearScale;
            row.Rect.DOScale(Vector3.one, _animation.AppearDuration).SetEase(_animation.AppearEase)
                .SetUpdate(true).SetLink(gameObject);
        }

        private RewardRowView Acquire(string id)
        {
            if (_pool.Count == 0)
            {
                throw new InvalidOperationException("Reward row pool exhausted; configure capacity for the reward catalogue.");
            }
            int last = _pool.Count - 1;
            RewardRowView row = _pool[last];
            _pool.RemoveAt(last);
            row.name = ROW_NAME_PREFIX + id;
            row.transform.SetAsLastSibling();
            KillRowTweens(row);
            row.Rect.localScale = Vector3.one;
            row.Group.alpha = _animation.VisibleAlpha;
            return row;
        }

        private void Retire(string id)
        {
            RewardRowView row;
            if (!_active.TryGetValue(id, out row))
            {
                return;
            }
            _active.Remove(id);
            KillRowTweens(row);
            row.gameObject.SetActive(false);
            _pool.Add(row);
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_content)
            {
                Transform content = transform.Find("ui_layout_rewards_content");
                if (content) _content = (RectTransform)content;
            }
            if (!_content || !_rowPrefab || !_animation || !_countAnimation)
            {
                Debug.LogError("RewardsListView requires serialized content, row prefab and animation profiles.", this);
            }
        }

        #endregion
#endif
    }
}
