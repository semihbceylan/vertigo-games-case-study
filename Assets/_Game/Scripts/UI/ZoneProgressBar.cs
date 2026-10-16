using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Horizontal zone progression strip. Renders a sliding window of numbered zone nodes that
    /// fill the bar height with no background. Only the current zone shows a background card:
    /// rounded white for normal, rounded safe for safe, gold for super (all from generated assets).
    /// Node sizing is computed from the bar rect so the editor preview and play mode match.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneProgressBar : MonoBehaviour
    {
        private const float NodeSpacing = 6f;
        private const float NumberFontFactor = 0.5f;
        private const float CurrentScale = 1f;
        private const float MarkerClearance = 18f;

        [SerializeField] private RectTransform nodesRoot;
        [SerializeField] private RectTransform marker;
        [Tooltip("Optional node prefab. When set, nodes are cloned from it instead of built in code.")]
        [SerializeField] private GameObject nodePrefab;

        [Header("Window")]
        [SerializeField, Min(3)] private int visibleNodes = 13;
        [Tooltip("Index within the window where the current zone sits. Set to the middle to centre it.")]
        [SerializeField, Min(0)] private int currentSlot = 6;

        [Header("Number Colors")]
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color safeTextColor = new Color(0.05f, 0.45f, 0.18f);
        [SerializeField] private Color superTextColor = new Color(1f, 0.85f, 0.2f);
        [Tooltip("Color of the current zone number for normal and super zones.")]
        [SerializeField] private Color currentTextColor = Color.black;

        [Header("Current Zone Card")]
        [Tooltip("Rounded card for normal zones (generated assets).")]
        [SerializeField] private Sprite currentZoneBackground;
        [Tooltip("Rounded card for safe zones (generated assets).")]
        [SerializeField] private Sprite currentSafeZoneBackground;
        [Tooltip("Gold card for super zones (generated assets).")]
        [SerializeField] private Sprite currentSuperZoneBackground;
        [Tooltip("Border frame drawn around the current zone card.")]
        [SerializeField] private Sprite currentZoneFrame;

        private readonly List<Node> _nodes = new List<Node>();
        private int _displayedZone = int.MinValue;

        private sealed class Node
        {
            public RectTransform Rect;
            public Image Bg;
            public Image Frame;
            public TMP_Text Label;
        }

        public void Set(int currentZone, int safeInterval, int superInterval, int finalZone = 0)
        {
            var rules = new ZoneRules(safeInterval, superInterval);

            var self = (RectTransform)transform;
            float slot = self.rect.width / visibleNodes;
            float nodeHeight = self.rect.height;
            float nodeWidth = Mathf.Max(1f, slot - NodeSpacing);

            // Window slides so the current zone stays at currentSlot. Out-of-range slots stay
            // empty: zones below 1 don't exist, and the run ends at finalZone (0 = endless).
            int startZone = currentZone - currentSlot;
            EnsureCount(visibleNodes);

            for (int i = 0; i < visibleNodes; i++)
            {
                int zone = startZone + i;
                var node = _nodes[i];
                if (node == null || node.Rect == null) continue;

                if (zone < 1 || (finalZone > 0 && zone > finalZone))
                {
                    node.Rect.gameObject.SetActive(false);
                    continue;
                }

                node.Rect.gameObject.SetActive(true);
                var kind = ZoneClassifier.Classify(zone, rules);
                bool isCurrent = zone == currentZone;

                PlaceNode(node, i, slot, nodeWidth, nodeHeight, isCurrent);

                // Only the current zone shows a background card (rounded white / safe / gold from generated assets).
                if (node.Bg != null)
                {
                    if (isCurrent)
                    {
                        node.Bg.enabled = true;
                        ApplyCurrentBackground(node.Bg, kind);
                    }
                    else
                    {
                        node.Bg.enabled = false;
                    }
                }

                if (node.Label != null)
                {
                    node.Label.text = zone.ToString();
                    node.Label.color = TextColor(kind, isCurrent);
                }

                ApplyFrame(node, isCurrent, currentZoneFrame);
            }

            if (marker != null)
            {
                int index = Mathf.Clamp(currentZone - startZone, 0, visibleNodes - 1);
                marker.anchoredPosition = new Vector2(
                    (index - (visibleNodes - 1) * 0.5f) * slot,
                    nodeHeight * 0.5f + MarkerClearance);
            }

            _displayedZone = currentZone;
        }

        /// <summary>
        /// Slides the numbered strip from the currently displayed zone to <paramref name="newZone"/>
        /// so the new number travels into the current slot, cross-fading the current-zone card.
        /// The final layout is applied on completion. Returns the tween, or null when there is
        /// nothing to animate (in which case the strip is set instantly).
        /// </summary>
        public Sequence PlayTransition(int newZone, int safeInterval, int superInterval, float duration, Ease ease, int finalZone = 0)
        {
            if (duration <= 0f || newZone == _displayedZone)
            {
                Set(newZone, safeInterval, superInterval, finalZone);
                return null;
            }

            var rules = new ZoneRules(safeInterval, superInterval);
            var self = (RectTransform)transform;
            float slot = self.rect.width / visibleNodes;
            int step = newZone - _displayedZone;

            EnsureCount(visibleNodes);

            int outgoingIndex = Mathf.Clamp(currentSlot, 0, visibleNodes - 1);
            int incomingIndex = Mathf.Clamp(currentSlot + step, 0, visibleNodes - 1);

            var seq = DOTween.Sequence();
            seq.SetUpdate(true);

            // Slide every visible node by one step; the reset on completion is visually seamless
            // because the number now occupying each slot matches the one that just left it.
            for (int i = 0; i < visibleNodes; i++)
            {
                var node = _nodes[i];
                if (node == null || node.Rect == null || !node.Rect.gameObject.activeSelf) continue;

                float x = node.Rect.anchoredPosition.x;
                seq.Join(node.Rect.DOAnchorPosX(x - step * slot, duration).SetEase(ease));
            }

            var incoming = _nodes[incomingIndex];
            if (incoming != null)
            {
                var kind = ZoneClassifier.Classify(newZone, rules);

                if (incoming.Bg != null)
                {
                    incoming.Bg.enabled = true;
                    ApplyCurrentBackground(incoming.Bg, kind, faded: true);
                    seq.Join(incoming.Bg.DOColor(Color.white, duration).SetEase(ease));
                }

                if (incoming.Frame != null && currentZoneFrame != null)
                {
                    incoming.Frame.enabled = true;
                    incoming.Frame.sprite = currentZoneFrame;
                    incoming.Frame.type = Image.Type.Sliced;
                    incoming.Frame.color = new Color(1f, 1f, 1f, 0f);
                    seq.Join(incoming.Frame.DOColor(Color.white, duration).SetEase(ease));
                }

                // The number fading into the current-zone colour rides along with its slide.
                if (incoming.Label != null)
                    seq.Join(TweenTextColor(incoming.Label, TextColor(kind, true), duration, ease));
            }

            var outgoing = _nodes[outgoingIndex];
            if (outgoing != null)
            {
                if (outgoing.Bg != null)
                    seq.Join(outgoing.Bg.DOColor(new Color(1f, 1f, 1f, 0f), duration).SetEase(ease));

                // The border slides out to the left with the outgoing card.
                if (outgoing.Frame != null && outgoing.Frame.enabled)
                    seq.Join(outgoing.Frame.DOColor(new Color(1f, 1f, 1f, 0f), duration).SetEase(ease));

                // ...and the number that was current fades back to its plain colour.
                if (outgoing.Label != null)
                    seq.Join(TweenTextColor(outgoing.Label, TextColor(ZoneClassifier.Classify(_displayedZone, rules), false), duration, ease));
            }

            seq.OnComplete(() => Set(newZone, safeInterval, superInterval, finalZone));
            return seq;
        }

        /// <summary>Tweens a label's colour from its current value to <paramref name="target"/>.</summary>
        private static Tweener TweenTextColor(TMP_Text label, Color target, float duration, Ease ease)
        {
            return DOTween
                .To(() => label.color, c => label.color = c, target, duration)
                .SetEase(ease);
        }

        /// <summary>Positions a node and applies the shared number styling (size, bold, no wrap).</summary>
        private void PlaceNode(Node node, int index, float slot, float nodeWidth, float nodeHeight, bool isCurrent)
        {
            float x = (index - (visibleNodes - 1) * 0.5f) * slot;
            node.Rect.anchoredPosition = new Vector2(x, 0f);

            // Square root so the rounded card stays proportional (no stretch).
            float side = Mathf.Min(nodeHeight, nodeWidth);
            node.Rect.sizeDelta = new Vector2(side, side);
            node.Rect.localScale = Vector3.one * (isCurrent ? CurrentScale : 1f);

            if (node.Label != null)
            {
                node.Label.fontSize = nodeHeight * NumberFontFactor;
                node.Label.fontStyle = FontStyles.Bold;
                node.Label.textWrappingMode = TextWrappingModes.NoWrap;
                node.Label.raycastTarget = false;
            }

            if (node.Bg != null)
            {
                node.Bg.raycastTarget = false;
                node.Bg.maskable = false;
            }

            if (node.Frame != null)
            {
                node.Frame.raycastTarget = false;
                node.Frame.maskable = false;
            }
        }

        /// <summary>Applies the current-zone card: rounded white for normal, rounded safe for safe, gold for super.</summary>
        private void ApplyCurrentBackground(Image bg, ZoneKind kind, bool faded = false)
        {
            // Sliced + square node = no stretch; rounding is baked in the generated asset.
            if (kind == ZoneKind.Safe)
                bg.sprite = currentSafeZoneBackground;
            else if (kind == ZoneKind.Super)
                bg.sprite = currentSuperZoneBackground != null ? currentSuperZoneBackground : currentZoneBackground;
            else
                bg.sprite = currentZoneBackground;
            bg.type = Image.Type.Sliced;
            bg.preserveAspect = true;
            bg.color = faded ? new Color(1f, 1f, 1f, 0f) : Color.white;

            bg.raycastTarget = false;
            bg.maskable = false;
        }

        private Color TextColor(ZoneKind kind, bool isCurrent)
        {
            if (isCurrent)
                return kind == ZoneKind.Safe ? safeTextColor : currentTextColor;

            switch (kind)
            {
                case ZoneKind.Super: return superTextColor;
                case ZoneKind.Safe: return safeTextColor;
                default: return normalTextColor;
            }
        }

        /// <summary>Shows the border frame on the current node and hides it everywhere else.</summary>
        private static void ApplyFrame(Node node, bool isCurrent, Sprite frameSprite)
        {
            if (node == null || node.Frame == null) return;

            if (isCurrent && frameSprite != null)
            {
                node.Frame.enabled = true;
                node.Frame.sprite = frameSprite;
                node.Frame.type = Image.Type.Sliced;
                node.Frame.color = Color.white;
            }
            else
            {
                node.Frame.enabled = false;
            }
        }

        /// <summary>Finds the node's border frame image, creating it if the node lacks one.</summary>
        private Image EnsureFrame(Transform nodeRoot)
        {
            var existing = nodeRoot.Find("ui_image_zone_frame");
            if (existing != null)
            {
                var found = existing.GetComponent<Image>();
                if (found != null) return found;
            }

            var go = new GameObject("ui_image_zone_frame", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(nodeRoot, false);
            go.transform.SetSiblingIndex(0);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.maskable = false;
            image.type = Image.Type.Sliced;
            image.enabled = false;
            return image;
        }

        private void EnsureCount(int count)
        {
            var parent = nodesRoot != null ? nodesRoot : (RectTransform)transform;

            // Adopt any nodes already present under the root (e.g. authored previews).
            if (_nodes.Count == 0)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    var image = parent.GetChild(i).GetComponent<Image>();
                    if (image == null) continue;

                    image.raycastTarget = false;
                    image.maskable = false;
                    image.preserveAspect = true;

                    var adoptedLabel = image.GetComponentInChildren<TMP_Text>(true);
                    if (adoptedLabel != null)
                    {
                        adoptedLabel.raycastTarget = false;
                        adoptedLabel.maskable = false;
                    }

                    _nodes.Add(new Node
                    {
                        Rect = (RectTransform)image.transform,
                        Bg = image,
                        Frame = EnsureFrame(image.transform),
                        Label = adoptedLabel
                    });
                }
            }

            while (_nodes.Count < count)
                _nodes.Add(CreateNode(parent, _nodes.Count));

            for (int i = count; i < _nodes.Count; i++)
            {
                if (_nodes[i] != null && _nodes[i].Rect != null)
                    _nodes[i].Rect.gameObject.SetActive(false);
            }
        }

        private Node CreateNode(RectTransform parent, int index)
        {
            if (nodePrefab != null)
                return CreateNodeFromPrefab(parent, index);

            var go = new GameObject("ui_zone_node_" + index + "_value", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = go.GetComponent<Image>();
            image.sprite = currentZoneBackground;
            image.type = Image.Type.Sliced;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.maskable = false;

            var textGo = new GameObject("ui_text_zone_number_value", typeof(RectTransform));
            textGo.layer = gameObject.layer;
            textGo.transform.SetParent(go.transform, false);

            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var label = textGo.AddComponent<TextMeshProUGUI>();
            label.text = index.ToString();
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.maskable = false;

            return new Node { Rect = rect, Bg = image, Frame = EnsureFrame(go.transform), Label = label };
        }

        private Node CreateNodeFromPrefab(RectTransform parent, int index)
        {
            var instance = Instantiate(nodePrefab, parent, false);
            instance.name = "ui_zone_node_" + index + "_value";
            instance.layer = gameObject.layer;

            var bg = instance.GetComponent<Image>();
            if (bg != null)
            {
                bg.raycastTarget = false;
                bg.maskable = false;
                bg.preserveAspect = true;
            }

            var label = instance.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = index.ToString();
                label.raycastTarget = false;
                label.maskable = false;
            }

            return new Node
            {
                Rect = (RectTransform)instance.transform,
                Bg = bg,
                Frame = EnsureFrame(instance.transform),
                Label = label
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (nodesRoot == null)
            {
                var t = transform.Find("ui_zone_nodes");
                if (t != null) nodesRoot = (RectTransform)t;
            }

            if (marker == null)
            {
                var t = transform.Find("ui_image_zone_marker");
                if (t != null) marker = (RectTransform)t;
            }

            if (!Application.isPlaying)
                ApplyEditorPreview();
        }

        /// <summary>Applies the runtime node sizing in edit mode so the scene preview matches play.</summary>
        /// <remarks>
        /// Renders a fresh zone-1 run (intervals mirror Zones_Default: safe 5 / super 30) so the
        /// editor shows the current-zone card, frame and numbers exactly like play mode.
        /// The side maps are owned by ZoneMapsView and intentionally left untouched here.
        /// </remarks>
        private void ApplyEditorPreview()
        {
            Set(1, 5, 30);
        }
#endif
    }
}
