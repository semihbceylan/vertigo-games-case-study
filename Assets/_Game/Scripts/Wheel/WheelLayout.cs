using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Wheel
{
    /// <summary>
    /// Places wheel slice slots on a circle using a mathematical radial layout:
    /// angleStep = 360 / sliceCount; angle = startAngle + index * angleStep;
    /// x = sin(angle) * radius; y = cos(angle) * radius.
    /// Each slot holds a reward icon plus a prize-count label on the hub-facing side.
    /// Slots are children of the rotating wheel so they spin with it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WheelLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform slicesRoot;
        [Tooltip("Optional slot prefab. When set, slices are cloned from it instead of built in code.")]
        [SerializeField] private GameObject slotPrefab;

        [Header("Radial Layout")]
        [SerializeField, Range(0f, 360f)] private float startAngle = 0f;
        [Tooltip("Distance from the wheel centre to each icon, in canvas units.")]
        [SerializeField] private float radius = 177.5f;
        [SerializeField] private float iconSize = 56f;
        [SerializeField, Min(0.05f)] private float iconScale = 1f;
        [Tooltip("Measured offset of the socket ring from the rotation centre, in canvas units. Keeps icons centred in their sockets.")]
        [SerializeField] private Vector2 socketCenterOffset = new Vector2(-1.7f, -0.5f);
        [Tooltip("Rotate each slot so its up-axis points away from the centre.")]
        [SerializeField] private bool orientRadially = true;

        [Header("Per-Category Icon Scales")]
        [Tooltip("Points rewards are square 256px icons that read large; kept smaller.")]
        [SerializeField, Min(0.1f)] private float pointsIconScale = 0.8f;
        [Tooltip("Standard rewards (wide renders/chests letterbox small); scaled up.")]
        [SerializeField, Min(0.1f)] private float standardIconScale = 1.3f;
        [Tooltip("Bomb/death icon; biggest on the wheel.")]
        [SerializeField, Min(0.1f)] private float bombIconScale = 1.5f;

        private readonly List<SliceSlot> _slots = new List<SliceSlot>();

        private sealed class SliceSlot
        {
            public RectTransform Root;
            public Image Icon;
            public TMP_Text Amount;
        }

        public RectTransform SlicesRoot => slicesRoot != null ? slicesRoot : (RectTransform)transform;
        public int SliceCount => _slots.Count;
        public float StartAngle => startAngle;
        public float Radius => radius;
        public float IconScale => iconScale;
        public float PointsIconScale => pointsIconScale;
        public float StandardIconScale => standardIconScale;
        public float BombIconScale => bombIconScale;

        public float AngleStep => _slots.Count > 0 ? 360f / _slots.Count : 0f;

        public Image GetIcon(int index)
        {
            return IsValid(index) ? _slots[index].Icon : null;
        }

        public TMP_Text GetAmountText(int index)
        {
            return IsValid(index) ? _slots[index].Amount : null;
        }

        /// <summary>
        /// Scales only the icon inside its slot. Slot roots and amount labels are untouched,
        /// so prize counts stay in uniform spots no matter the category.
        /// </summary>
        public void SetSlotIconScale(int index, float scale)
        {
            if (!IsValid(index)) return;
            var icon = _slots[index].Icon;
            if (icon != null) icon.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        private bool IsValid(int index)
        {
            return index >= 0 && index < _slots.Count && _slots[index] != null;
        }

        /// <summary>Rebuilds exactly <paramref name="count"/> slice slots, then lays them out.</summary>
        public void SetSliceCount(int count)
        {
            if (count < 1) count = 1;

            var root = SlicesRoot;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            _slots.Clear();
            for (int i = 0; i < count; i++)
                _slots.Add(CreateSlot(root, i));

            ApplyLayout();
        }

        /// <summary>Recomputes every slot position, rotation and size from the radial formula.</summary>
        public void ApplyLayout()
        {
            int count = _slots.Count;
            if (count == 0) return;

            float angleStep = 360f / count;
            float slotSize = iconSize * iconScale;

            for (int i = 0; i < count; i++)
            {
                var slot = _slots[i];
                if (slot == null || slot.Root == null) continue;

                float angle = startAngle + i * angleStep;
                float radians = angle * Mathf.Deg2Rad;

                slot.Root.anchorMin = new Vector2(0.5f, 0.5f);
                slot.Root.anchorMax = new Vector2(0.5f, 0.5f);
                slot.Root.pivot = new Vector2(0.5f, 0.5f);
                slot.Root.anchoredPosition = new Vector2(
                    Mathf.Sin(radians) * radius + socketCenterOffset.x,
                    Mathf.Cos(radians) * radius + socketCenterOffset.y);
                slot.Root.sizeDelta = new Vector2(slotSize, slotSize);
                slot.Root.localRotation = orientRadially
                    ? Quaternion.Euler(0f, 0f, -angle)
                    : Quaternion.identity;
            }
        }

        private SliceSlot CreateSlot(RectTransform parent, int index)
        {
            if (slotPrefab != null)
                return CreateSlotFromPrefab(parent, index);

            var rootGo = new GameObject("ui_wheel_slice_" + (index + 1) + "_value", typeof(RectTransform));
            rootGo.layer = gameObject.layer;
            rootGo.transform.SetParent(parent, false);

            var iconGo = new GameObject("ui_image_wheel_reward_" + (index + 1) + "_value", typeof(RectTransform), typeof(Image));
            iconGo.layer = gameObject.layer;
            iconGo.transform.SetParent(rootGo.transform, false);

            var icon = iconGo.GetComponent<Image>();
            icon.raycastTarget = false;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;

            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            // Prize count sits at the slot's bottom edge, which faces the hub.
            var amountGo = new GameObject("ui_text_wheel_amount_value", typeof(RectTransform));
            amountGo.layer = gameObject.layer;
            amountGo.transform.SetParent(rootGo.transform, false);

            var amount = amountGo.AddComponent<TextMeshProUGUI>();
            amount.text = string.Empty;
            amount.fontSize = 26f;
            amount.alignment = TextAlignmentOptions.Center;
            amount.color = Color.white;
            amount.raycastTarget = false;
            amount.enableWordWrapping = false;

            var amountRect = (RectTransform)amountGo.transform;
            amountRect.anchorMin = new Vector2(0f, 0f);
            amountRect.anchorMax = new Vector2(1f, 0f);
            amountRect.pivot = new Vector2(0.5f, 1f);
            amountRect.sizeDelta = new Vector2(0f, 32f);
            amountRect.anchoredPosition = new Vector2(0f, 8f);

            return new SliceSlot { Root = (RectTransform)rootGo.transform, Icon = icon, Amount = amount };
        }

        private SliceSlot CreateSlotFromPrefab(RectTransform parent, int index)
        {
            var instance = Instantiate(slotPrefab, parent, false);
            instance.name = "ui_wheel_slice_" + (index + 1) + "_value";
            instance.layer = gameObject.layer;

            return new SliceSlot
            {
                Root = (RectTransform)instance.transform,
                Icon = instance.GetComponentInChildren<Image>(true),
                Amount = instance.GetComponentInChildren<TMP_Text>(true)
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (slicesRoot == null) slicesRoot = (RectTransform)transform;
            ApplyLayout();
        }
#endif
    }
}
