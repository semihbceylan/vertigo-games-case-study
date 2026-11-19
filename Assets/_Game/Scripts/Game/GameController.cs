using System;
using System.Collections.Generic;
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
    public sealed class GameController : MonoBehaviour, ISessionPresentation
    {
        [SerializeField] private GameConfiguration configuration;
        [SerializeField] private WheelView wheelView;
        [SerializeField] private WheelLayout wheelLayout;
        [SerializeField] private WheelSpinner wheelSpinner;
        [SerializeField] private GameScreenView screen;
        [SerializeField] private SpinButton spinButton;
        [SerializeField] private UIButton leaveButton;
        [SerializeField] private UIButton restartButton;
        [SerializeField] private RewardPresentationController _rewardPresentation;
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private WheelTransitionController _wheelTransitionController;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("Debug (Editor / Development builds only)")]
        [Tooltip("Start the run at this zone. 0 uses the configured starting zone.")]
        [SerializeField, Min(0)] private int debugStartZone = 0;
#endif

        private readonly RewardCatalogPresenter _rewardCatalog = new RewardCatalogPresenter();
        private IWheelVisualPresenter _wheelVisualPresenter;
        private ISessionHudPresenter _hudPresenter;
        private GameSession _session;
        private ICurrencyWallet _currencyWallet;
        private Vector2 _wheelHomePosition;
        private Action _zoneTransitionComplete;

        private void Awake()
        {
            if (wheelView != null && wheelView.WheelRotation != null)
                _wheelHomePosition = wheelView.WheelRotation.anchoredPosition;
        }

        private void OnEnable()
        {
            SubscribeSessionEvents();
            if (spinButton != null) spinButton.Clicked += OnSpinClicked;
            if (leaveButton != null) leaveButton.Clicked += OnLeaveClicked;
            if (restartButton != null) restartButton.Clicked += OnRestartClicked;
            if (screen != null && screen.Feedback != null) screen.Feedback.ActionRequested += OnFeedbackAction;
            if (screen != null && screen.Results != null) screen.Results.RestartRequested += OnResultsRestartRequested;
        }

        private void OnDisable()
        {
            UnsubscribeSessionEvents();
            if (spinButton != null) spinButton.Clicked -= OnSpinClicked;
            if (leaveButton != null) leaveButton.Clicked -= OnLeaveClicked;
            if (restartButton != null) restartButton.Clicked -= OnRestartClicked;
            if (screen != null && screen.Feedback != null) screen.Feedback.ActionRequested -= OnFeedbackAction;
            if (screen != null && screen.Results != null) screen.Results.RestartRequested -= OnResultsRestartRequested;
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
                configuration.Zones.FinalZone,
                presentation: this);

            _currencyWallet = new CurrencyWallet(new PlayerPrefsCurrencyStore(), _session.Events);
            _hudPresenter = new SessionHudPresenter(_session, configuration.Zones, screen, spinButton, _rewardCatalog);

            SubscribeSessionEvents();

            _session.StartNewRun();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugStartZone > 0) _session.JumpToZone(debugStartZone);
#endif
            ApplyZoneWheel();
            RefreshState();
        }

        private void OnDestroy()
        {
            Cancel();
            UnsubscribeSessionEvents();
        }

        private void SubscribeSessionEvents()
        {
            if (_session == null) return;
            _session.Events.Subscribe<SessionChanged>(OnSessionChanged);
            _session.Events.Subscribe<RunCompleted>(OnRunCompleted);
        }

        private void UnsubscribeSessionEvents()
        {
            if (_session == null) return;
            _session.Events.Unsubscribe<SessionChanged>(OnSessionChanged);
            _session.Events.Unsubscribe<RunCompleted>(OnRunCompleted);
        }

        private void OnSessionChanged(SessionChanged message)
        {
            RefreshState();
        }

        private IRandomSource CreateRandomSource()
        {
            return configuration.DeterministicRandom
                ? (IRandomSource)new SystemRandomSource(configuration.RandomSeed)
                : new SystemRandomSource();
        }

        private void BuildRewardLookup()
        {
            _rewardCatalog.Configure(configuration.Zones);
            if (_wheelVisualPresenter == null) _wheelVisualPresenter = new WheelVisualPresenter(_rewardCatalog);
        }

        private RewardDefinition ResolveReward(string rewardId)
        {
            return _rewardCatalog.Resolve(rewardId);
        }

        // ---- input ----

        private void OnSpinClicked()
        {
            if (_session == null || !_session.CanSpin) return;
            _session.BeginSpin();
        }

        public void PlaySpin(int sliceIndex, ZoneWheel wheel, Action onComplete)
        {
            float startAngle = wheelLayout != null ? wheelLayout.StartAngle : 0f;
            if (!wheelSpinner)
            {
                onComplete();
                return;
            }
            wheelSpinner.SpinToSlice(sliceIndex, wheel.SliceCount, startAngle, onComplete);
        }

        /// <summary>Refresh the collected row before delegating its presentation.</summary>
        public void PresentReward(RewardGrant grant, int sliceIndex, Action onComplete)
        {
            RefreshRewards();
            IRewardPresentation presentation = _rewardPresentation;
            presentation.Play(grant, ResolveReward(grant.RewardId), sliceIndex, onComplete);
        }

        private void OnLeaveClicked()
        {
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
            else if (_session.IsCompleted) _session.StartNewRun();
        }

        // ---- session events ----

        public void ShowLoss(int zone)
        {
            if (screen == null) return;
            var definition = configuration.Zones.GetWheelForZone(zone);
            screen.ShowBomb(definition != null ? definition.BombSprite : null);
        }

        public void PlayZoneTransition(Action onComplete)
        {
            _zoneTransitionComplete = onComplete;
            if (screen != null) screen.HideFeedback();
            PlayZoneTransition();
        }

        private void OnRunCompleted(RunCompleted message)
        {
            foreach (var pair in _session.Wallet.Totals)
            {
                RewardDefinition reward = ResolveReward(pair.Key);
                if (reward && reward.Currency)
                {
                    _currencyWallet.Credit(reward.Currency.CurrencyId, pair.Value);
                }
            }

        }

        public void ShowResults()
        {
            if (screen == null) return;

            screen.HideFeedback();
            if (screen.Results != null) screen.Results.Show(BuildRewardRows());
        }

        /// <summary>Silently drops any visible/deferred showcase (restart or new run).</summary>
        private void ClearJackpotState()
        {
            if (_rewardPresentation) _rewardPresentation.Cancel();
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

        public void Reset()
        {
            ClearJackpotState();
            KillWheelTransition();
            ApplyZoneWheel();
            if (screen != null) screen.HideFeedback();
            if (screen != null && screen.Results != null) screen.Results.Hide();
        }

        public void Cancel()
        {
            _zoneTransitionComplete = null;
            if (wheelSpinner) wheelSpinner.CancelSpin();
            if (_rewardPresentation) _rewardPresentation.Cancel();
            KillWheelTransition();
            if (screen) screen.HideJackpot();
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
            _wheelVisualPresenter.Fill(wheel, definition, rotation, baseImage, layout);
        }

        private void PlayZoneTransition()
        {
            IWheelTransition transition = _wheelTransitionController;
            transition.Play(PopulateIncomingWheel, StartZoneBarTransition(), FinishZoneTransition);
        }

        private void PopulateIncomingWheel(WheelDiscView incoming)
        {
            PopulateWheelVisual(incoming.Rect, incoming.Base, incoming.Layout);
        }

        /// <summary>Slides the numbered zone strip to the new zone in step with the wheel.</summary>
        private Sequence StartZoneBarTransition()
        {
            if (screen == null || screen.ZoneBar == null || _session == null) return null;

            var tween = screen.ZoneBar.PlayTransition(
                _session.Zone,
                configuration.Zones,
                _wheelTransitionController.Animation.SlideDuration,
                _wheelTransitionController.Animation.SlideEase);

            return tween;
        }

        private void FinishZoneTransition()
        {
            ApplyZoneWheel();

            CompleteZonePresentation();
        }

        private void CompleteZonePresentation()
        {
            Action completion = _zoneTransitionComplete;
            _zoneTransitionComplete = null;
            completion?.Invoke();
        }

        private void KillWheelTransition()
        {
            if (_wheelTransitionController) _wheelTransitionController.Cancel();
        }

        private void RefreshState()
        {
            _hudPresenter?.Refresh();
        }

        private void RefreshRewards()
        {
            _hudPresenter?.RefreshRewards();
        }

        /// <summary>
        /// Snapshot of the wallet as view rows, resolved to their configured icons/labels.
        /// Rows are grouped by <see cref="RewardDefinition.Category"/> (renders, helmets, chests,
        /// currency, consumables, points); the stable sort keeps first-obtained order within a group.
        /// </summary>
        private List<RewardRowData> BuildRewardRows()
        {
            return _rewardCatalog.BuildRows(_session.Wallet);
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
            if (_session == null || !_session.CanSpin) return;

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
            if (!wheelView) wheelView = GetComponentInChildren<WheelView>(true);
            if (!wheelLayout) wheelLayout = GetComponentInChildren<WheelLayout>(true);
            if (!wheelSpinner) wheelSpinner = GetComponentInChildren<WheelSpinner>(true);
            if (!screen) screen = GetComponentInChildren<GameScreenView>(true);
            if (!spinButton) spinButton = GetComponentInChildren<SpinButton>(true);
            if (!_rewardPresentation) _rewardPresentation = GetComponent<RewardPresentationController>();
            if (!_wheelTransitionController) _wheelTransitionController = GetComponent<WheelTransitionController>();
            if (!_canvasRect)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas) _canvasRect = (RectTransform)canvas.rootCanvas.transform;
            }
            foreach (UIButton button in GetComponentsInChildren<UIButton>(true))
            {
                if (!leaveButton && button.name == "ui_button_leave") leaveButton = button;
                if (!restartButton && button.name == "ui_button_restart") restartButton = button;
            }
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

            screen.ZoneBar.Set(zones.StartingZone, zones);
        }
#endif
    }
}
