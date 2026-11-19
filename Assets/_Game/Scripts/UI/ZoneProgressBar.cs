using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Data;

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

        [Header("Window")]
        [SerializeField, Min(3)] private int visibleNodes = 13;
        [Tooltip("Index within the window where the current zone sits. Set to the middle to centre it.")]
        [SerializeField, Min(0)] private int currentSlot = 6;

        [Header("Current Zone Card")]
        [Tooltip("Border frame drawn around the current zone card.")]
        [SerializeField] private Sprite currentZoneFrame;

        [SerializeField] private Node[] _nodes = new Node[0];
        private int _displayedZone = int.MinValue;
        [SerializeField] private ZoneConfiguration _configuration;
        private int _transitionZone;
        private Sequence _transition;

        [Serializable]
        private sealed class Node
        {
            public RectTransform Rect;
            public Image Bg;
            public Image Frame;
            public TMP_Text Label;
        }

        public void Set(int currentZone, ZoneConfiguration configuration)
        {
            _configuration = configuration;
            int finalZone = configuration.FinalZone;

            var self = (RectTransform)transform;
            float slot = self.rect.width / visibleNodes;
            float nodeHeight = self.rect.height;
            float nodeWidth = Mathf.Max(1f, slot - NodeSpacing);

            // Window slides so the current zone stays at currentSlot. Out-of-range slots stay
            // empty: zones below 1 don't exist, and the run ends at finalZone (0 = endless).
            int startZone = currentZone - currentSlot;
            ValidateNodes();

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
                var kind = configuration.Resolve(zone);
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
        public Sequence PlayTransition(int newZone, ZoneConfiguration configuration, float duration, Ease ease)
        {
            _configuration = configuration;
            _transitionZone = newZone;
            if (duration <= 0f || newZone == _displayedZone)
            {
                Set(newZone, configuration);
                return null;
            }

            var self = (RectTransform)transform;
            float slot = self.rect.width / visibleNodes;
            int step = newZone - _displayedZone;

            ValidateNodes();

            int outgoingIndex = Mathf.Clamp(currentSlot, 0, visibleNodes - 1);
            int incomingIndex = Mathf.Clamp(currentSlot + step, 0, visibleNodes - 1);

            var seq = DOTween.Sequence();
            _transition = seq;
            seq.SetLink(gameObject);
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
                var kind = configuration.Resolve(newZone);

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
                    seq.Join(TweenTextColor(outgoing.Label, TextColor(configuration.Resolve(_displayedZone), false), duration, ease));
            }

            seq.OnComplete(CompleteTransition);
            return seq;
        }

        private void CompleteTransition()
        {
            _transition = null;
            Set(_transitionZone, _configuration);
        }

        private void OnDestroy()
        {
            _transition?.Kill(false);
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
        private void ApplyCurrentBackground(Image bg, ZoneRuleDefinition kind, bool faded = false)
        {
            // Sliced + square node = no stretch; rounding is baked in the generated asset.
            bg.sprite = kind.CurrentBackground;
            bg.type = Image.Type.Sliced;
            bg.preserveAspect = true;
            bg.color = faded ? new Color(1f, 1f, 1f, 0f) : Color.white;

            bg.raycastTarget = false;
            bg.maskable = false;
        }

        private Color TextColor(ZoneRuleDefinition kind, bool isCurrent)
        {
            return isCurrent ? kind.CurrentTextColor : kind.TextColor;
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

        private void ValidateNodes()
        {
            if (_nodes.Length != visibleNodes)
            {
                throw new InvalidOperationException("Zone strip requires one serialized node per visible slot.");
            }
            foreach (Node node in _nodes)
            {
                if (node == null || !node.Rect || !node.Bg || !node.Frame || !node.Label)
                {
                    throw new InvalidOperationException("Zone strip node references must be wired in the scene.");
                }
            }
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

            if (_nodes.Length != visibleNodes)
            {
                if (gameObject.activeInHierarchy)
                    Debug.LogError("Zone strip requires one serialized node per visible slot.", this);
                return;
            }
            foreach (Node node in _nodes)
            {
                if (node == null || !node.Rect || !node.Bg || !node.Frame || !node.Label)
                {
                    Debug.LogError("Zone strip has missing serialized node references.", this);
                    return;
                }
            }
            if (!Application.isPlaying)
                ApplyEditorPreview();
        }

        /// <summary>Applies the runtime node sizing in edit mode so the scene preview matches play.</summary>
        /// <remarks>
        /// Renders a fresh run using the assigned configuration so the
        /// editor shows the current-zone card, frame and numbers exactly like play mode.
        /// The side maps are owned by ZoneMapsView and intentionally left untouched here.
        /// </remarks>
        private void ApplyEditorPreview()
        {
            if (_configuration) Set(_configuration.StartingZone, _configuration);
        }
#endif
    }
}
