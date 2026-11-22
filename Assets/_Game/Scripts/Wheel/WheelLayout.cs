using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Wheel
{
    /// <summary>
    /// Places pooled slice slots on a circle: angleStep = 360 / sliceCount,
    /// angle = startAngle + index * angleStep, x = sin(angle) * radius, y = cos(angle) * radius.
    /// Slots are children of the rotating wheel so they spin with it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WheelLayout : MonoBehaviour
    {
        #region Configuration and Runtime

        public const float FULL_CIRCLE_DEGREES = 360f;
        private const int MINIMUM_SLICES = 1;
        private const string SLOT_NAME_PREFIX = "ui_panel_wheel_slice_";

        [SerializeField, FormerlySerializedAs("slicesRoot")] private RectTransform _slicesRoot;
        [SerializeField, FormerlySerializedAs("slotPrefab")] private WheelSliceView _slotPrefab;

        [Header("Radial Layout")]
        [SerializeField, Range(0f, FULL_CIRCLE_DEGREES), FormerlySerializedAs("startAngle")] private float _startAngle;
        [Tooltip("Distance from the wheel centre to each icon, in canvas units.")]
        [SerializeField, Min(0f), FormerlySerializedAs("radius")] private float _radius = 177.5f;
        [SerializeField, Min(1f), FormerlySerializedAs("iconSize")] private float _slotSize = 56f;
        [Tooltip("Measured offset of the socket ring from the rotation centre, in canvas units.")]
        [SerializeField, FormerlySerializedAs("socketCenterOffset")] private Vector2 _socketCenterOffset = new Vector2(-1.7f, -0.5f);
        [Tooltip("Rotate each slot so its up-axis points away from the centre.")]
        [SerializeField, FormerlySerializedAs("orientRadially")] private bool _isOrientedRadially = true;
        [Tooltip("Base icon size of reward slices; each reward multiplies it by its own icon scale.")]
        [SerializeField, Min(0.01f), FormerlySerializedAs("standardIconScale")] private float _rewardIconScale = 1.3f;

        private readonly List<WheelSliceView> _slots = new List<WheelSliceView>();
        private int _sliceCount;

        #endregion

        #region Queries

        public int SliceCount
        {
            get { return _sliceCount; }
        }

        public float StartAngle
        {
            get { return _startAngle; }
        }

        public float RewardIconScale
        {
            get { return _rewardIconScale; }
        }

        public Image GetIcon(int index)
        {
            return GetSlot(index).Icon;
        }

        public TMP_Text GetAmountText(int index)
        {
            return GetSlot(index).Amount;
        }

        private WheelSliceView GetSlot(int index)
        {
            if (index < 0 || index >= _sliceCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return _slots[index];
        }

        #endregion

        #region Layout

        /// <summary>
        /// Scales only the icon inside its slot so prize counts stay in uniform spots.
        /// </summary>
        public void SetSlotIconScale(int index, float scale)
        {
            GetSlot(index).Icon.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Reuses slots up to the largest requested wheel, then lays out the active subset.</summary>
        public void SetSliceCount(int count)
        {
            count = Mathf.Max(MINIMUM_SLICES, count);
            if (_slots.Count == 0)
            {
                // Authored editor-preview children are replaced only on first initialization.
                for (int i = _slicesRoot.childCount - 1; i >= 0; i--)
                {
                    GameObject child = _slicesRoot.GetChild(i).gameObject;
                    child.SetActive(false);
                    if (Application.isPlaying)
                    {
                        Destroy(child);
                    }
                    else
                    {
                        DestroyImmediate(child);
                    }
                }
            }
            while (_slots.Count < count)
            {
                WheelSliceView slot = Instantiate(_slotPrefab, _slicesRoot, false);
                slot.name = SLOT_NAME_PREFIX + _slots.Count;
                _slots.Add(slot);
            }
            _sliceCount = count;
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].gameObject.SetActive(i < count);
            }
            ApplyLayout();
        }

        /// <summary>Recomputes every slot position, rotation and size from the radial formula.</summary>
        public void ApplyLayout()
        {
            if (_sliceCount == 0)
            {
                return;
            }
            float angleStep = FULL_CIRCLE_DEGREES / _sliceCount;
            for (int i = 0; i < _sliceCount; i++)
            {
                RectTransform root = _slots[i].Rect;
                float angle = _startAngle + i * angleStep;
                float radians = angle * Mathf.Deg2Rad;
                root.anchoredPosition = new Vector2(
                    Mathf.Sin(radians) * _radius + _socketCenterOffset.x,
                    Mathf.Cos(radians) * _radius + _socketCenterOffset.y);
                root.sizeDelta = new Vector2(_slotSize, _slotSize);
                root.localRotation = _isOrientedRadially ? Quaternion.Euler(0f, 0f, -angle) : Quaternion.identity;
            }
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_slicesRoot)
            {
                _slicesRoot = (RectTransform)transform;
            }
            if (!_slotPrefab)
            {
                Debug.LogError("WheelLayout requires a slot prefab.", this);
                return;
            }
            // Domain reload restores the authored count before the runtime slot cache exists.
            if (_slots.Count >= _sliceCount)
            {
                ApplyLayout();
            }
        }

        #endregion
#endif
    }
}
