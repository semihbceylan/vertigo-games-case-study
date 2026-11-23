using UnityEngine;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.UI;
using Vertigo.WheelOfFortune.Wheel;

namespace Vertigo.WheelOfFortune.Game
{
    /// <summary>
    /// Composition root: builds the session-scoped objects from serialized scene references and
    /// pairs their subscriptions with this component's enable/disable lifecycle. It holds no flow
    /// state and handles no input itself.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameCompositionRoot : MonoBehaviour
    {
        #region Serialized References

        [SerializeField] private GameConfiguration _configuration;
        [SerializeField] private WheelView _wheelView;
        [SerializeField] private WheelSpinner _wheelSpinner;
        [SerializeField] private GameScreenView _screen;
        [SerializeField] private FeedbackView _lossPanel;
        [SerializeField] private ResultsView _results;
        [SerializeField] private SpinButton _spinButton;
        [SerializeField] private UIButton _leaveButton;
        [SerializeField] private RewardPresentationController _rewardPresentation;
        [SerializeField] private WheelTransitionController _wheelTransitionController;
        [SerializeField] private CurrencyHudView _currencyHud;
        [SerializeField] private BombImpactView _bombImpact;
        [Tooltip("Optional hidden debug panel; only bound in the editor and development builds.")]
        [SerializeField] private AdminPanel _adminPanel;

        #endregion

        #region Session Scope

        private GameSession _session;
        private SessionPresenter _presentation;
        private SessionHudPresenter _hudPresenter;
        private SessionInputController _input;
        private RunCurrencySettlement _currencySettlement;
        private bool _isComposed;

        private void Start()
        {
            Compose();
            BindListeners();
            _session.StartNewRun();
        }

        private void Compose()
        {
            var zones = new ZoneCatalog(_configuration.Zones);
            var catalog = new RewardCatalogPresenter();
            catalog.Configure(_configuration.Zones);
            var events = new EventBus();
            var startingBalances = _configuration.CreateStartingBalances();
            var currencyWallet = new CurrencyWallet(new PlayerPrefsCurrencyStore(), events, startingBalances);
            IRandomSource random = CreateRandomSource();
            // Debug builds route spin outcomes through an overridable source; release builds never do.
            ForcedOutcomeRandom spinRandom = Debug.isDebugBuild && _adminPanel ? new ForcedOutcomeRandom(random) : null;
            _currencyHud.Bind(currencyWallet, events);
            _lossPanel.SetReviveCurrency(_configuration.ReviveCurrency);

            _presentation = new SessionPresenter(zones, _wheelView, _wheelSpinner, _screen, _rewardPresentation,
                _wheelTransitionController, new WheelVisualPresenter(catalog), catalog, _bombImpact, _currencyHud);
            _session = new GameSession(zones.Rules, new WheelTierFactory(_configuration.Zones), random,
                zones.StartingZone, zones.FinalZone, events, _presentation, currencyWallet,
                _configuration.CreateRevivePolicy(), spinRandom);
            _hudPresenter = new SessionHudPresenter(_session, zones, _screen, _spinButton, catalog);
            _presentation.Bind(_session, _hudPresenter);
            _currencySettlement = new RunCurrencySettlement(events, _session.Wallet, currencyWallet, catalog);
            _input = new SessionInputController(_session, _spinButton, _leaveButton, _lossPanel, _results);
            if (spinRandom != null)
            {
                _adminPanel.Bind(_session, zones, spinRandom, currencyWallet, startingBalances);
            }
            _isComposed = true;
        }

        private IRandomSource CreateRandomSource()
        {
            return _configuration.IsDeterministicRandom
                ? new SystemRandomSource(_configuration.RandomSeed)
                : new SystemRandomSource();
        }

        #endregion

        #region Listener Lifecycle

        private void OnEnable()
        {
            BindListeners();
        }

        private void OnDisable()
        {
            UnbindListeners();
        }

        private void OnDestroy()
        {
            if (_isComposed)
            {
                _presentation.Cancel();
            }
        }

        /// <summary>
        /// The first OnEnable runs before Start has composed the session, so Start binds once
        /// composition is complete; every later enable binds here. Each binding is idempotent.
        /// </summary>
        private void BindListeners()
        {
            if (!_isComposed)
            {
                return;
            }
            _hudPresenter.Subscribe();
            _currencySettlement.Subscribe();
            _input.Bind();
        }

        private void UnbindListeners()
        {
            if (!_isComposed)
            {
                return;
            }
            _hudPresenter.Unsubscribe();
            _currencySettlement.Unsubscribe();
            _input.Unbind();
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_wheelView) _wheelView = GetComponentInChildren<WheelView>(true);
            if (!_wheelSpinner) _wheelSpinner = GetComponentInChildren<WheelSpinner>(true);
            if (!_screen) _screen = GetComponentInChildren<GameScreenView>(true);
            if (!_lossPanel) _lossPanel = GetComponentInChildren<FeedbackView>(true);
            if (!_results) _results = GetComponentInChildren<ResultsView>(true);
            if (!_spinButton) _spinButton = GetComponentInChildren<SpinButton>(true);
            if (!_rewardPresentation) _rewardPresentation = GetComponent<RewardPresentationController>();
            if (!_wheelTransitionController) _wheelTransitionController = GetComponent<WheelTransitionController>();
            if (!_adminPanel) _adminPanel = GetComponentInChildren<AdminPanel>(true);
            if (!_bombImpact) _bombImpact = GetComponentInChildren<BombImpactView>(true);
            if (!_configuration || !_configuration.Zones || !_wheelView || !_wheelSpinner || !_screen || !_lossPanel ||
                !_results || !_spinButton || !_leaveButton || !_rewardPresentation ||
                !_wheelTransitionController || !_currencyHud || !_bombImpact)
            {
                Debug.LogError("GameCompositionRoot has missing serialized references.", this);
            }
        }

        #endregion
#endif
    }
}
