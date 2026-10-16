using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.UI;
using Vertigo.WheelOfFortune.Wheel;

namespace Vertigo.WheelOfFortune.Game
{
    /// <summary>
    /// Owns the runtime <see cref="GameSession"/> and connects it to the wheel view, animation and
    /// HUD. All button listeners are registered here in code.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private GameConfiguration configuration;
        [SerializeField] private WheelView wheelView;
        [SerializeField] private WheelLayout wheelLayout;
        [SerializeField] private WheelSpinner wheelSpinner;
        [SerializeField] private GameScreenView screen;
        [SerializeField] private SpinButton spinButton;
        [SerializeField] private UIButton leaveButton;
        [SerializeField] private UIButton restartButton;
        [SerializeField] private RewardFlyAnimator rewardFly;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("Debug (Editor / Development builds only)")]
        [Tooltip("Start the run at this zone. 0 uses the configured starting zone.")]
        [SerializeField, Min(0)] private int debugStartZone = 0;
#endif

        [Header("Zone wheel transition")]
        [Tooltip("How long the finished wheel stays on screen before the next zone slides in.")]
        [SerializeField, Min(0f)] private float wheelHoldDuration = 0.5f;
        [Tooltip("Duration of the slide animation.")]
        [SerializeField, Min(0.05f)] private float wheelSlideDuration = 0.35f;
        [SerializeField] private Ease wheelSlideEase = Ease.InOutCubic;

        private readonly Dictionary<string, RewardDefinition> _rewardLookup = new Dictionary<string, RewardDefinition>();
        private GameSession _session;
        private Sequence _wheelTransition;
        private RectTransform _incomingWheel;
        private Vector2 _wheelHomePosition;
        private bool _isTransitioning;
        private bool _suppressZoneBarRefresh;
        private bool _jackpotShowing;
        private bool _pendingZoneAdvance;
        private List<RewardRowData> _pendingResults;

        private void Awake()
        {
            ResolveReferences();

            if (wheelView != null && wheelView.WheelRotation != null)
                _wheelHomePosition = wheelView.WheelRotation.anchoredPosition;
        }

        private void OnEnable()
        {
            if (spinButton != null) spinButton.Clicked += OnSpinClicked;
            if (leaveButton != null) leaveButton.Clicked += OnLeaveClicked;
            if (restartButton != null) restartButton.Clicked += OnRestartClicked;
            if (screen != null && screen.Feedback != null) screen.Feedback.ActionRequested += OnFeedbackAction;
            if (screen != null && screen.Feedback != null) screen.Feedback.ReviveRequested += OnReviveRequested;
            if (screen != null && screen.Results != null) screen.Results.RestartRequested += OnResultsRestartRequested;
            if (screen != null)
            {
                var showcase = screen.EnsureShowcase();
                if (showcase != null) showcase.Dismissed += OnShowcaseDismissed;
            }
        }

        private void OnDisable()
        {
            if (spinButton != null) spinButton.Clicked -= OnSpinClicked;
            if (leaveButton != null) leaveButton.Clicked -= OnLeaveClicked;
            if (restartButton != null) restartButton.Clicked -= OnRestartClicked;
            if (screen != null && screen.Feedback != null) screen.Feedback.ActionRequested -= OnFeedbackAction;
            if (screen != null && screen.Feedback != null) screen.Feedback.ReviveRequested -= OnReviveRequested;
            if (screen != null && screen.Results != null) screen.Results.RestartRequested -= OnResultsRestartRequested;
            if (screen != null && screen.Showcase != null) screen.Showcase.Dismissed -= OnShowcaseDismissed;
        }

        private void Start()
        {
            if (configuration == null || configuration.Zones == null)
            {
                Debug.LogError("[GameController] A GameConfiguration with a ZoneConfiguration is required.", this);
                enabled = false;
                return;
            }

            BuildRewardLookup();

            _session = new GameSession(
                configuration.Zones.ToRules(),
                new WheelTierFactory(configuration.Zones),
                CreateRandomSource(),
                configuration.Zones.StartingZone,
                configuration.Zones.FinalZone);

            _session.StateChanged += RefreshState;
            _session.RewardCollected += OnRewardCollected;
            _session.BombHit += OnBombHit;
            _session.ZoneAdvanced += OnZoneAdvanced;
            _session.RunCompleted += OnRunCompleted;
            _session.RunRestarted += OnRunRestarted;

            _session.StartNewRun();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugStartZone > 0) _session.JumpToZone(debugStartZone);
#endif
            ApplyZoneWheel();
            RefreshState();
        }

        private void OnDestroy()
        {
            if (_wheelTransition != null && _wheelTransition.IsActive()) _wheelTransition.Kill(false);
            if (_session == null) return;
            _session.StateChanged -= RefreshState;
            _session.RewardCollected -= OnRewardCollected;
            _session.BombHit -= OnBombHit;
            _session.ZoneAdvanced -= OnZoneAdvanced;
            _session.RunCompleted -= OnRunCompleted;
            _session.RunRestarted -= OnRunRestarted;
        }

        private IRandomSource CreateRandomSource()
        {
            return configuration.DeterministicRandom
                ? (IRandomSource)new SystemRandomSource(configuration.RandomSeed)
                : new SystemRandomSource();
        }

        private void BuildRewardLookup()
        {
            _rewardLookup.Clear();

            foreach (var wheel in configuration.Zones.AllWheels)
            {
                if (wheel == null) continue;

                if (wheel.RewardPool != null)
                {
                    var entries = wheel.RewardPool.Entries;
                    for (int e = 0; e < entries.Length; e++)
                    {
                        if (entries[e].reward == null) continue;
                        _rewardLookup[entries[e].reward.RewardId] = entries[e].reward;
                    }
                }

                var slices = wheel.Slices;
                if (slices != null)
                {
                    for (int s = 0; s < slices.Length; s++)
                    {
                        var reward = slices[s].reward;
                        if (reward == null) continue;
                        _rewardLookup[reward.RewardId] = reward;
                    }
                }
            }
        }

        private RewardDefinition ResolveReward(string rewardId)
        {
            RewardDefinition definition;
            return _rewardLookup.TryGetValue(rewardId, out definition) ? definition : null;
        }

        private static readonly System.Collections.Generic.HashSet<string> PointsRewardIds =
            new System.Collections.Generic.HashSet<string>
            {
                "armor", "knife", "pistol", "rifle", "shotgun", "smg", "sniper"
            };

        private static bool IsPointsReward(string rewardId)
        {
            return rewardId != null && PointsRewardIds.Contains(rewardId);
        }

        /// <summary>
        /// True for jackpot rewards that deserve the big fullscreen showcase: weapon Renders
        /// (Assets/Art/Icons/Renders) and melee weapons (Assets/Art/Icons/Melee).
        /// Explicit <see cref="RewardDefinition.ShowcaseBig"/> wins; the id / icon-name checks
        /// below keep already-authored assets working without re-ticking every checkbox.
        /// </summary>
        private static bool IsJackpotReward(RewardDefinition definition)
        {
            if (definition == null) return false;
            if (definition.ShowcaseBig) return true;

            string id = (definition.RewardId ?? string.Empty).ToLowerInvariant();
            if (id.StartsWith("t1_") ||
                id.StartsWith("t2_") ||
                id.StartsWith("t3_") ||
                id.StartsWith("bayonet"))
                return true;

            string iconName = (definition.Icon != null ? definition.Icon.name : string.Empty).ToLowerInvariant();
            return iconName.Contains("render") || iconName.Contains("mle") || iconName.Contains("melee") || iconName.Contains("bayonet");
        }

        // ---- input ----

        private void OnSpinClicked()
        {
            if (_session == null || !_session.CanSpin) return;
            if (_isTransitioning) return;
            if (_jackpotShowing) return;
            if (wheelSpinner != null && wheelSpinner.IsSpinning) return;

            _session.BeginSpin();

            int sliceIndex = _session.PendingSliceIndex;
            int sliceCount = _session.CurrentWheel.SliceCount;
            float startAngle = wheelLayout != null ? wheelLayout.StartAngle : 0f;

            RefreshState();

            if (wheelSpinner == null)
            {
                _session.ResolvePendingSpin();
                return;
            }

            wheelSpinner.SpinToSlice(sliceIndex, sliceCount, startAngle, OnSpinAnimationComplete);
        }

        /// <summary>
        /// Called when the wheel stops. Jackpots resolve immediately so the showcase pops
        /// first, then the icon flies; normal rewards fly first and resolve on arrival.
        /// </summary>
        private void OnSpinAnimationComplete()
        {
            if (_session == null) return;

            int sliceIndex = _session.PendingSliceIndex;
            var wheel = _session.CurrentWheel;

            if (wheel != null && sliceIndex >= 0 && sliceIndex < wheel.SliceCount && !wheel[sliceIndex].IsBomb)
            {
                string rewardId = wheel[sliceIndex].Reward.RewardId;
                var source = wheelLayout != null ? wheelLayout.GetIcon(sliceIndex) : null;

                // Jackpot first: resolve now (shows showcase), fly after.
                var definition = ResolveReward(rewardId);
                if (IsJackpotReward(definition))
                {
                    var flySprite = source != null ? source.sprite : null;
                    var flyRect = source != null ? source.rectTransform : null;
                    _session.ResolvePendingSpin();
                    if (rewardFly != null && flySprite != null)
                    {
                        // Resolved before this point, so the row already exists: aim at the real icon.
                        var target = ResolveRewardTarget(rewardId);
                        rewardFly.Fly(flySprite, flyRect, target, () =>
                        {
                            if (screen != null && screen.RewardsList != null) screen.RewardsList.PunchRow(rewardId);
                        });
                    }
                    return;
                }

                if (rewardFly != null && source != null && source.sprite != null)
                {
                    var sprite = source.sprite;
                    var fromRect = source.rectTransform;
                    Action onArrive = () =>
                    {
                        _session.ResolvePendingSpin();
                        if (screen != null && screen.RewardsList != null) screen.RewardsList.PunchRow(rewardId);
                    };

                    if (screen != null && screen.RewardsList != null)
                    {
                        // Already banked before: home in on the existing row.
                        if (screen.RewardsList.TryGetRowIcon(rewardId, out var rowIcon))
                        {
                            rewardFly.Fly(sprite, fromRect, rowIcon, onArrive);
                            return;
                        }

                        // First time this reward is earned: fly to the slot it will occupy.
                        if (TryGetPredictedRowIconWorldPosition(rewardId, out var slotWorld))
                        {
                            rewardFly.FlyToPosition(sprite, fromRect, slotWorld, onArrive);
                            return;
                        }
                    }

                    rewardFly.Fly(sprite, fromRect, ResolveRewardTarget(rewardId), onArrive);
                    return;
                }
            }

            _session.ResolvePendingSpin();
        }

        /// <summary>The rewards-pane rect a flown reward should home in on (its row, else the list).</summary>
        private RectTransform ResolveRewardTarget(string rewardId)
        {
            if (screen == null || screen.RewardsList == null) return null;

            if (screen.RewardsList.TryGetRowIcon(rewardId, out var rowIcon)) return rowIcon;
            return screen.RewardsList.ListRect;
        }

        /// <summary>
        /// World position the icon of a not-yet-banked <paramref name="rewardId"/> will occupy, using
        /// the display category order. The stable sort keeps existing equal-category rows first and a
        /// brand-new wallet key is appended last, so the insertion index is just the count of existing
        /// rows whose category sorts at or before this reward's.
        /// </summary>
        private bool TryGetPredictedRowIconWorldPosition(string rewardId, out Vector3 worldPosition)
        {
            worldPosition = default;
            if (_session == null || screen == null || screen.RewardsList == null) return false;
            if (_session.Wallet.Totals.ContainsKey(rewardId)) return false;

            int rank = CategoryRank(ResolveReward(rewardId));
            int index = 0;
            foreach (var pair in _session.Wallet.Totals)
            {
                if (CategoryRank(ResolveReward(pair.Key)) <= rank) index++;
            }

            return screen.RewardsList.TryGetSlotIconWorldPosition(index, out worldPosition);
        }

        private void OnLeaveClicked()
        {
            if (_jackpotShowing) return;
            if (_session != null && _session.CanLeave) _session.Leave();
        }

        private void OnRestartClicked()
        {
            if (_session == null) return;
            if (_session.CanRestart) _session.Restart();
            else if (_session.IsIdle && screen != null) screen.HideFeedback();
        }

        private void OnFeedbackAction()
        {
            if (_session == null) return;
            if (_session.CanRestart) _session.Restart();
            else if (_session.Phase == SessionPhase.Completed) _session.StartNewRun();
        }

        private void OnReviveRequested()
        {
            if (_session == null || !_session.CanRevive) return;

            ClearJackpotState();
            KillWheelTransition();
            _session.Revive();
            ApplyZoneWheel();
            RefreshState();
        }

        // ---- session events ----

        private void OnRewardCollected(RewardGrant grant)
        {
            if (screen == null) return;
            var definition = ResolveReward(grant.RewardId);
            var icon = definition != null ? definition.Icon : null;
            var label = definition != null ? definition.DisplayName : grant.RewardId;

            if (IsJackpotReward(definition))
            {
                // Big fullscreen showcase. It stays until the player taps; the zone slide
                // and the end-of-run results wait behind it (see OnShowcaseDismissed).
                _jackpotShowing = true;
                _pendingZoneAdvance = false;
                _pendingResults = null;
                screen.HideFeedback();
                screen.ShowJackpot(icon, label, grant.Amount);
                SetSpinInput(false);
                return;
            }

            screen.ShowReward(icon, label, grant.Amount);
        }

        private void OnBombHit(int zone)
        {
            if (screen == null) return;
            var definition = configuration.Zones.GetWheelForZone(zone);
            screen.ShowBomb(definition != null ? definition.BombSprite : null);
        }

        private void OnZoneAdvanced(int zone)
        {
            if (_jackpotShowing)
            {
                // Defer the slide until the player dismisses the showcase.
                _pendingZoneAdvance = true;
                return;
            }

            if (screen != null) screen.HideFeedback();
            PlayZoneTransition();
        }

        private void OnRunCompleted(long banked)
        {
            if (_jackpotShowing)
            {
                // Defer the results screen until the showcase is dismissed.
                _pendingZoneAdvance = false;
                _pendingResults = BuildRewardRows();
                return;
            }

            if (screen == null) return;

            screen.HideFeedback();
            if (screen.Results != null) screen.Results.Show(BuildRewardRows());
        }

        /// <summary>Player tapped the jackpot showcase away. Flush whatever it was pausing.</summary>
        private void OnShowcaseDismissed()
        {
            _jackpotShowing = false;

            if (!_isTransitioning) SetSpinInput(true);

            if (_pendingResults != null)
            {
                var rows = _pendingResults;
                _pendingResults = null;
                _pendingZoneAdvance = false;
                if (screen != null)
                {
                    screen.HideFeedback();
                    if (screen.Results != null) screen.Results.Show(rows);
                }
                return;
            }

            if (_pendingZoneAdvance)
            {
                _pendingZoneAdvance = false;
                if (screen != null) screen.HideFeedback();
                PlayZoneTransition();
            }
        }

        /// <summary>Silently drops any visible/deferred showcase (restart, revive, new run).</summary>
        private void ClearJackpotState()
        {
            _jackpotShowing = false;
            _pendingZoneAdvance = false;
            _pendingResults = null;
            if (screen != null) screen.HideJackpot();
        }

        private void OnResultsRestartRequested()
        {
            if (_session == null) return;

            ClearJackpotState();
            KillWheelTransition();
            _session.StartNewRun();
            ApplyZoneWheel();
            RefreshState();
        }

        private void OnRunRestarted()
        {
            ClearJackpotState();
            KillWheelTransition();
            ApplyZoneWheel();
            if (screen != null) screen.HideFeedback();
            if (screen != null && screen.Results != null) screen.Results.Hide();
        }

        // ---- view ----

        private void ApplyZoneWheel()
        {
            if (_session == null || wheelLayout == null) return;

            var definition = configuration.Zones.GetWheelForZone(_session.Zone);

            if (wheelView != null && wheelView.WheelRotation != null)
                wheelView.WheelRotation.anchoredPosition = _wheelHomePosition;

            if (wheelView != null && wheelView.WheelIndicator != null && definition != null && definition.IndicatorSprite != null)
                wheelView.WheelIndicator.sprite = definition.IndicatorSprite;

            PopulateWheelVisual(
                wheelView != null ? wheelView.WheelRotation : null,
                wheelView != null ? wheelView.WheelBase : null,
                wheelLayout);
        }

        /// <summary>Fills a wheel disc (base sprite + slice icons) with the current zone's wheel.</summary>
        private void PopulateWheelVisual(RectTransform rotation, Image baseImage, WheelLayout layout)
        {
            if (_session == null || layout == null) return;

            FillWheelVisual(
                _session.CurrentWheel,
                configuration.Zones.GetWheelForZone(_session.Zone),
                rotation,
                baseImage,
                layout);
        }

        /// <summary>Shared wheel filler used by both the live session and the edit-mode preview.</summary>
        private void FillWheelVisual(ZoneWheel wheel, WheelDefinition definition, RectTransform rotation, Image baseImage, WheelLayout layout)
        {
            if (wheel == null || layout == null) return;

            if (baseImage != null && definition != null && definition.BaseSprite != null)
                baseImage.sprite = definition.BaseSprite;

            int count = wheel.SliceCount;
            layout.SetSliceCount(count);

            Sprite bombSprite = definition != null ? definition.BombSprite : null;

            for (int i = 0; i < count; i++)
            {
                var icon = layout.GetIcon(i);
                var amount = layout.GetAmountText(i);
                if (icon == null) continue;

                var slice = wheel[i];
                if (slice.IsBomb)
                {
                    icon.sprite = bombSprite;
                    layout.SetSlotIconScale(i, layout.BombIconScale);
                    if (amount != null) amount.gameObject.SetActive(false);
                }
                else
                {
                    var reward = ResolveReward(slice.Reward.RewardId);
                    icon.sprite = reward != null ? reward.Icon : null;
                    layout.SetSlotIconScale(i, IsPointsReward(slice.Reward.RewardId) ? layout.PointsIconScale : layout.StandardIconScale);
                    if (amount != null)
                    {
                        amount.gameObject.SetActive(true);
                        amount.text = slice.Reward.Amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                icon.color = Color.white;
            }

            if (rotation != null) rotation.localRotation = Quaternion.identity;
            layout.ApplyLayout();
        }

        /// <summary>
        /// Holds the just-finished wheel on screen for <see cref="wheelHoldDuration"/>, then slides
        /// it out to the left while the next zone's wheel slides in from the right.
        /// </summary>
        private void PlayZoneTransition()
        {
            RectTransform outgoing = wheelView != null ? wheelView.WheelRotation : null;
            var parent = outgoing != null ? outgoing.parent as RectTransform : null;

            if (outgoing == null || parent == null || wheelLayout == null)
            {
                ApplyZoneWheel();
                return;
            }

            KillWheelTransition();

            _isTransitioning = true;
            SetSpinInput(false);

            Vector2 home = outgoing.anchoredPosition;
            float distance = ComputeSlideDistance(parent, home);

            // Build the incoming wheel off-screen to the right, filled with the new zone's data.
            var incoming = Instantiate(outgoing.gameObject, parent).GetComponent<RectTransform>();
            incoming.SetSiblingIndex(outgoing.GetSiblingIndex());
            incoming.anchoredPosition = new Vector2(home.x + distance, home.y);
            incoming.localRotation = Quaternion.identity;
            _incomingWheel = incoming;

            PopulateWheelVisual(
                incoming,
                FindChildImage(incoming, "ui_image_wheel_base"),
                incoming.GetComponentInChildren<WheelLayout>(true));

            _wheelTransition = DOTween.Sequence();
            _wheelTransition.SetUpdate(true);
            _wheelTransition.AppendInterval(wheelHoldDuration);
            _wheelTransition.Append(outgoing.DOAnchorPosX(home.x - distance, wheelSlideDuration).SetEase(wheelSlideEase));
            _wheelTransition.Join(incoming.DOAnchorPosX(home.x, wheelSlideDuration).SetEase(wheelSlideEase));

            var barTween = StartZoneBarTransition();
            if (barTween != null) _wheelTransition.Join(barTween);

            _wheelTransition.OnComplete(() => FinishZoneTransition(outgoing, incoming, home));
        }

        /// <summary>Slides the numbered zone strip to the new zone in step with the wheel.</summary>
        private Sequence StartZoneBarTransition()
        {
            if (screen == null || screen.ZoneBar == null || _session == null) return null;

            var tween = screen.ZoneBar.PlayTransition(
                _session.Zone,
                configuration.Zones.SafeZoneInterval,
                configuration.Zones.SuperZoneInterval,
                wheelSlideDuration,
                wheelSlideEase,
                configuration.Zones.FinalZone);

            _suppressZoneBarRefresh = tween != null;
            return tween;
        }

        /// <summary>
        /// Distance that carries the wheel fully past the canvas edge: half the canvas width plus
        /// the wheel's own radius, measured in the wheel's parent space.
        /// </summary>
        private float ComputeSlideDistance(RectTransform parent, Vector2 home)
        {
            float halfCanvas = 960f;

            var canvas = GetComponentInParent<Canvas>();
            var canvasRect = canvas != null && canvas.rootCanvas != null
                ? canvas.rootCanvas.transform as RectTransform
                : null;

            if (canvasRect != null)
            {
                Vector3 canvasCorner = canvasRect.TransformPoint(new Vector3(canvasRect.rect.width * 0.5f, 0f, 0f));
                Vector3 cornerInParent = parent.InverseTransformPoint(canvasCorner);
                halfCanvas = Mathf.Abs(cornerInParent.x - home.x);
            }

            return halfCanvas + parent.rect.width * 0.5f;
        }

        private void FinishZoneTransition(RectTransform outgoing, RectTransform incoming, Vector2 home)
        {
            _wheelTransition = null;
            _suppressZoneBarRefresh = false;

            if (_incomingWheel != null)
            {
                Destroy(_incomingWheel.gameObject);
                _incomingWheel = null;
            }

            ApplyZoneWheel();

            _isTransitioning = false;
            SetSpinInput(true);
        }

        private void KillWheelTransition()
        {
            if (_wheelTransition != null && _wheelTransition.IsActive()) _wheelTransition.Kill(false);
            _wheelTransition = null;
            _suppressZoneBarRefresh = false;

            if (_incomingWheel != null)
            {
                Destroy(_incomingWheel.gameObject);
                _incomingWheel = null;
            }

            _isTransitioning = false;
            SetSpinInput(true);
        }

        private void SetSpinInput(bool enabled)
        {
            if (spinButton != null) spinButton.Interactable = enabled;
        }

        private static Image FindChildImage(Transform root, string name)
        {
            if (root == null) return null;

            var images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
                if (images[i].name == name) return images[i];

            return null;
        }

        private void RefreshState()
        {
            if (_session == null || screen == null) return;

            int zone = _session.Zone;

            if (_suppressZoneBarRefresh)
                screen.SetZoneMaps(zone, configuration.Zones.SafeZoneInterval, configuration.Zones.SuperZoneInterval, configuration.Zones.FinalZone);
            else
                screen.SetZone(zone, configuration.Zones.SafeZoneInterval, configuration.Zones.SuperZoneInterval, configuration.Zones.FinalZone);

            screen.SetLeaveAvailable(_session.CanLeave);
            screen.SetLeaveVisible(_session.Phase != SessionPhase.Bust);
            screen.SetRestartAvailable(_session.CanRestart);

            RefreshRewards();
        }

        private void RefreshRewards()
        {
            if (_session == null || screen == null) return;
            screen.SetRewards(BuildRewardRows());
        }

        /// <summary>
        /// Snapshot of the wallet as view rows, resolved to their configured icons/labels.
        /// Rows are grouped by <see cref="RewardDefinition.Category"/> (renders, helmets, chests,
        /// currency, consumables, points); the stable sort keeps first-obtained order within a group.
        /// </summary>
        private List<RewardRowData> BuildRewardRows()
        {
            var rows = new List<RewardRowData>();
            if (_session == null) return rows;

            var ordered = _session.Wallet.Totals
                .OrderBy(pair => CategoryRank(ResolveReward(pair.Key)));

            foreach (var pair in ordered)
            {
                var definition = ResolveReward(pair.Key);
                rows.Add(new RewardRowData(
                    pair.Key,
                    definition != null ? definition.Icon : null,
                    definition != null ? definition.DisplayName : pair.Key,
                    pair.Value));
            }

            return rows;
        }

        /// <summary>Sort rank for a reward's display category; unassigned rewards sort last.</summary>
        private static int CategoryRank(RewardDefinition definition)
        {
            if (definition == null || definition.Category == RewardCategory.Uncategorized)
                return int.MaxValue;

            return (int)definition.Category;
        }

        private void ResolveReferences()
        {
            if (wheelView == null) wheelView = GetComponentInChildren<WheelView>(true);
            if (wheelLayout == null) wheelLayout = GetComponentInChildren<WheelLayout>(true);
            if (wheelSpinner == null) wheelSpinner = GetComponentInChildren<WheelSpinner>(true);
            if (screen == null) screen = GetComponentInChildren<GameScreenView>(true);
            if (spinButton == null) spinButton = GetComponentInChildren<SpinButton>(true);
            if (leaveButton == null) leaveButton = FindButton("ui_button_leave");
            if (restartButton == null) restartButton = FindButton("ui_button_restart");
            if (rewardFly == null) rewardFly = GetComponentInChildren<RewardFlyAnimator>(true);
        }

        private UIButton FindButton(string name)
        {
            var buttons = GetComponentsInChildren<UIButton>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == name) return buttons[i];
            }
            return null;
        }

        // ---- development teleport (editor / development builds only) ----

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            if (_session == null) return;

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null) return;

            // Step one zone ( ] / [ ), a whole safe-zone gap ( PageUp / PageDown ),
            // or jump straight to a demo zone: 1 -> 54, 2 -> 60.
            if (keyboard.rightBracketKey.wasPressedThisFrame) DebugJumpTo(_session.Zone + 1);
            else if (keyboard.leftBracketKey.wasPressedThisFrame) DebugJumpTo(_session.Zone - 1);
            else if (keyboard.pageUpKey.wasPressedThisFrame) DebugJumpTo(_session.Zone + 5);
            else if (keyboard.pageDownKey.wasPressedThisFrame) DebugJumpTo(_session.Zone - 5);
            else if (keyboard.digit1Key.wasPressedThisFrame) DebugJumpTo(54);
            else if (keyboard.digit2Key.wasPressedThisFrame) DebugJumpTo(60);
        }

        /// <summary>Dev helper: teleports the live run to a zone and refreshes the whole view.</summary>
        private void DebugJumpTo(int zone)
        {
            if (_session == null) return;

            ClearJackpotState();
            KillWheelTransition();
            _session.JumpToZone(zone);
            ApplyZoneWheel();
            RefreshState();
        }
#endif

#if UNITY_EDITOR
        [ContextMenu("Debug/Jump To Zone 54")]
        private void DebugJumpToZone54() => DebugJumpTo(54);

        [ContextMenu("Debug/Jump To Zone 55 (Safe)")]
        private void DebugJumpToZone55() => DebugJumpTo(55);

        [ContextMenu("Debug/Jump To Zone 59")]
        private void DebugJumpToZone59() => DebugJumpTo(59);

        [ContextMenu("Debug/Jump To Zone 60 (Super)")]
        private void DebugJumpToZone60() => DebugJumpTo(60);
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            PreviewZoneOneInEditor();
        }

        /// <summary>
        /// Edit-mode only: stages the scene to look like a fresh zone-1 run (Bronze wheel +
        /// zone bar on 1) so edit mode matches play mode. Never touches the side maps.
        /// Deterministic seed keeps the preview stable between validations.
        /// </summary>
        private void PreviewZoneOneInEditor()
        {
            if (Application.isPlaying) return;
            if (configuration == null || configuration.Zones == null) return;
            if (wheelView == null || wheelLayout == null || screen == null || screen.ZoneBar == null) return;

            // Scene mutation (destroy/recreate slots) silently fails inside OnValidate while
            // Unity holds the scene graph, which stacks duplicate slots. Defer past validation.
            if (_previewQueued) return;
            _previewQueued = true;
            UnityEditor.EditorApplication.delayCall += RunQueuedPreview;
        }

        private bool _previewQueued;

        private void RunQueuedPreview()
        {
            _previewQueued = false;
            PreviewZoneOneNow();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Wheel of Fortune/Preview Zone 1 (Edit)")]
        private static void PreviewZoneOneMenu()
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<GameController>();
            if (controller == null)
            {
                UnityEngine.Debug.LogWarning("[GameController] No GameController in the open scene to preview.");
                return;
            }

            controller.PreviewZoneOneNow();
        }
#endif

        /// <summary>Immediate zone-1 preview. Must run outside OnValidate (menu, delayed call).</summary>
        public void PreviewZoneOneNow()
        {
            if (Application.isPlaying) return;
            if (configuration == null || configuration.Zones == null) return;
            if (wheelView == null || wheelLayout == null || screen == null || screen.ZoneBar == null) return;

            var zones = configuration.Zones;
            var kind = ZoneClassifier.Classify(1, zones.ToRules());
            var definition = zones.GetWheel(kind);
            if (definition == null) return;

            BuildRewardLookup();

            var preview = new WheelTierFactory(zones).CreateWheel(1, kind, new SystemRandomSource(12345));
            if (wheelView.WheelIndicator != null && definition.IndicatorSprite != null)
                wheelView.WheelIndicator.sprite = definition.IndicatorSprite;

            FillWheelVisual(
                preview,
                definition,
                wheelView.WheelRotation,
                wheelView.WheelBase,
                wheelLayout);

            screen.ZoneBar.Set(1, zones.SafeZoneInterval, zones.SuperZoneInterval);
        }
#endif
    }
}
