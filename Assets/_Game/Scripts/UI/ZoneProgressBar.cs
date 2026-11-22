using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Horizontal zone progression strip. Renders a sliding window of numbered zone nodes; only the
    /// current zone shows a background card and frame, both taken from the zone rule asset.
    /// Node sizing is computed from the bar rect so the editor preview and play mode match.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneProgressBar : MonoBehaviour
    {
        #region Configuration and Runtime

        private const int NO_DISPLAYED_ZONE = int.MinValue;
        /// <summary>Window slots are laid out symmetrically around the bar centre.</summary>
        private const float CENTRE_OFFSET = 0.5f;

        [Header("Window")]
        [SerializeField, Min(1)] private int _visibleNodes = 13;
        [Tooltip("Index within the window where the current zone sits. Set to the middle to centre it.")]
        [SerializeField, Min(0)] private int _currentSlot = 6;
        [Tooltip("Horizontal gap between neighbouring nodes, in canvas units.")]
        [SerializeField, Min(0f)] private float _nodeSpacing = 6f;
        [Tooltip("Zone number font size as a fraction of the bar height.")]
        [SerializeField, Range(0.1f, 1f)] private float _numberFontScale = 0.5f;

        [Header("Current Zone Card")]
        [Tooltip("Border frame drawn around the current zone card.")]
        [SerializeField] private Sprite _currentZoneFrame;
        [SerializeField] private ZoneStripAnimation _animation;

        [SerializeField] private Node[] _nodes = new Node[0];
        [Tooltip("Editor preview only: the strip is rendered at this configuration's starting zone.")]
        [SerializeField] private ZoneConfiguration _previewConfiguration;

        private int _displayedZone = NO_DISPLAYED_ZONE;
        private int _transitionZone;
        private ZoneCatalog _zones;
        private Sequence _transition;

        [Serializable]
        private sealed class Node
        {
            public RectTransform Rect;
            public Image Bg;
            public Image Frame;
            public TMP_Text Label;
        }

        #endregion

        #region Lifecycle

        private void OnDisable()
        {
            CancelTransition();
        }

        private void OnDestroy()
        {
            CancelTransition();
        }

        #endregion

        #region Layout

        public void Set(int currentZone, ZoneCatalog zones)
        {
            CancelTransition();
            ValidateNodes();
            int finalZone = zones.FinalZone;

            RectTransform self = (RectTransform)transform;
            float slot = self.rect.width / _visibleNodes;
            float nodeHeight = self.rect.height;
            float nodeWidth = Mathf.Max(0f, slot - _nodeSpacing);

            // The window slides so the current zone stays at _currentSlot. Out-of-range slots stay
            // empty: zones below the first don't exist, and the run ends at finalZone (0 = endless).
            int startZone = currentZone - _currentSlot;
            for (int i = 0; i < _visibleNodes; i++)
            {
                int zone = startZone + i;
                Node node = _nodes[i];
                bool isInRange = zone >= ZoneRules.FIRST_ZONE && (finalZone == ZoneRules.ENDLESS_FINAL_ZONE || zone <= finalZone);
                node.Rect.gameObject.SetActive(isInRange);
                if (!isInRange)
                {
                    continue;
                }

                ZoneRuleDefinition rule = zones.Resolve(zone);
                bool isCurrent = zone == currentZone;
                PlaceNode(node, i, slot, nodeWidth, nodeHeight);

                node.Bg.enabled = isCurrent;
                if (isCurrent)
                {
                    node.Bg.sprite = rule.CurrentBackground;
                    node.Bg.color = _animation.VisibleTint;
                }

                node.Label.text = zone.ToString();
                node.Label.color = GetTextColor(rule, isCurrent);
                ApplyFrame(node, isCurrent);
            }
            _displayedZone = currentZone;
        }

        private void PlaceNode(Node node, int index, float slot, float nodeWidth, float nodeHeight)
        {
            node.Rect.anchoredPosition = new Vector2(GetSlotX(index, slot), 0f);
            // Square node so the rounded card keeps its proportions.
            float side = Mathf.Min(nodeHeight, nodeWidth);
            node.Rect.sizeDelta = new Vector2(side, side);
            node.Rect.localScale = Vector3.one;
            node.Label.fontSize = nodeHeight * _numberFontScale;
        }

        private float GetSlotX(int index, float slot)
        {
            return (index - (_visibleNodes - 1) * CENTRE_OFFSET) * slot;
        }

        private void ApplyFrame(Node node, bool isCurrent)
        {
            bool hasFrame = isCurrent && _currentZoneFrame;
            node.Frame.enabled = hasFrame;
            if (hasFrame)
            {
                node.Frame.sprite = _currentZoneFrame;
                node.Frame.color = _animation.VisibleTint;
            }
        }

        private static Color GetTextColor(ZoneRuleDefinition rule, bool isCurrent)
        {
            return isCurrent ? rule.CurrentTextColor : rule.TextColor;
        }

        #endregion

        #region Transition

        /// <summary>
        /// Slides the numbered strip from the displayed zone to <paramref name="newZone"/>, cross-fading
        /// the current-zone card. The final layout is applied on completion. Returns null when there
        /// is nothing to animate, in which case the strip is set instantly.
        /// </summary>
        public Sequence PlayTransition(int newZone, ZoneCatalog zones)
        {
            CancelTransition();
            if (_displayedZone != NO_DISPLAYED_ZONE)
            {
                Set(_displayedZone, zones);
            }
            _zones = zones;
            _transitionZone = newZone;
            float duration = _animation.SlideDuration;
            if (duration <= 0f || newZone == _displayedZone || _displayedZone == NO_DISPLAYED_ZONE)
            {
                Set(newZone, zones);
                return null;
            }

            RectTransform self = (RectTransform)transform;
            float slot = self.rect.width / _visibleNodes;
            int step = newZone - _displayedZone;
            Ease ease = _animation.SlideEase;
            int outgoingIndex = Mathf.Clamp(_currentSlot, 0, _visibleNodes - 1);
            int incomingIndex = Mathf.Clamp(_currentSlot + step, 0, _visibleNodes - 1);

            _transition = DOTween.Sequence().SetLink(gameObject).SetUpdate(true);

            // Every visible node slides by the step; the reset on completion is seamless because the
            // number now occupying each slot matches the one that just left it.
            for (int i = 0; i < _visibleNodes; i++)
            {
                Node node = _nodes[i];
                if (!node.Rect.gameObject.activeSelf)
                {
                    continue;
                }
                float x = node.Rect.anchoredPosition.x;
                _transition.Join(node.Rect.DOAnchorPosX(x - step * slot, duration).SetEase(ease));
            }

            Node incoming = _nodes[incomingIndex];
            ZoneRuleDefinition incomingRule = zones.Resolve(newZone);
            incoming.Bg.enabled = true;
            incoming.Bg.sprite = incomingRule.CurrentBackground;
            incoming.Bg.color = _animation.HiddenTint;
            _transition.Join(incoming.Bg.DOColor(_animation.VisibleTint, duration).SetEase(ease));
            if (_currentZoneFrame)
            {
                incoming.Frame.enabled = true;
                incoming.Frame.sprite = _currentZoneFrame;
                incoming.Frame.color = _animation.HiddenTint;
                _transition.Join(incoming.Frame.DOColor(_animation.VisibleTint, duration).SetEase(ease));
            }
            _transition.Join(incoming.Label.DOColor(GetTextColor(incomingRule, true), duration).SetEase(ease));

            Node outgoing = _nodes[outgoingIndex];
            _transition.Join(outgoing.Bg.DOColor(_animation.HiddenTint, duration).SetEase(ease));
            if (outgoing.Frame.enabled)
            {
                _transition.Join(outgoing.Frame.DOColor(_animation.HiddenTint, duration).SetEase(ease));
            }
            ZoneRuleDefinition outgoingRule = zones.Resolve(_displayedZone);
            _transition.Join(outgoing.Label.DOColor(GetTextColor(outgoingRule, false), duration).SetEase(ease));

            _transition.OnComplete(CompleteTransition);
            return _transition;
        }

        private void CompleteTransition()
        {
            _transition = null;
            Set(_transitionZone, _zones);
        }

        public void CancelTransition()
        {
            if (_transition != null)
            {
                _transition.Kill(false);
            }
            _transition = null;
        }

        #endregion

        #region Validation

        private void ValidateNodes()
        {
            if (!HasValidNodes())
            {
                throw new InvalidOperationException("Zone strip requires one fully wired node per visible slot and an animation profile.");
            }
        }

        private bool HasValidNodes()
        {
            if (!_animation || _nodes.Length != _visibleNodes || _currentSlot >= _visibleNodes)
            {
                return false;
            }
            foreach (Node node in _nodes)
            {
                if (node == null || !node.Rect || !node.Bg || !node.Frame || !node.Label)
                {
                    return false;
                }
            }
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!HasValidNodes())
            {
                Debug.LogError("Zone strip requires one fully wired node per visible slot and an animation profile.", this);
                return;
            }
            if (!Application.isPlaying && _previewConfiguration)
            {
                Set(_previewConfiguration.StartingZone, new ZoneCatalog(_previewConfiguration));
            }
        }
#endif

        #endregion
    }
}
