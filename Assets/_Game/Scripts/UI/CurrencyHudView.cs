using TMPro;
using UnityEngine;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Persistent balances shown on the loss panel, plus an explicitly non-transactional demo
    /// top-up panel. Balance changes arrive through the session event bus.
    /// </summary>
    public sealed class CurrencyHudView : MonoBehaviour, ICurrencyHudView
    {
        #region Configuration and Runtime

        private const float SHOWN_ALPHA = 1f;
        private const float HIDDEN_ALPHA = 0f;

        [SerializeField] private CurrencyDefinition _cash;
        [SerializeField] private CurrencyDefinition _gold;
        [SerializeField] private TMP_Text _cashAmount;
        [SerializeField] private TMP_Text _goldAmount;
        [SerializeField] private UIButton _cashPlus;
        [SerializeField] private UIButton _goldPlus;
        [SerializeField] private CanvasGroup _topUpPanel;
        [SerializeField] private UIButton _close;
        [SerializeField] private CanvasGroup[] _underlyingHud = new CanvasGroup[0];
        [SerializeField] private CanvasGroup _balancesGroup;
        [SerializeField] private CanvasGroup[] _deathScreenContent = new CanvasGroup[0];

        private ModalHudVisibility _hudVisibility;
        private ModalHudVisibility _deathScreenVisibility;
        private ICurrencyWallet _wallet;
        private IEventBus _events;
        private bool _isSubscribed;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _hudVisibility = new ModalHudVisibility(_underlyingHud);
            _deathScreenVisibility = new ModalHudVisibility(_deathScreenContent);
            CloseTopUp();
        }

        private void OnEnable()
        {
            _cashPlus.Clicked += OpenTopUp;
            _goldPlus.Clicked += OpenTopUp;
            _close.Clicked += CloseTopUp;
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            _cashPlus.Clicked -= OpenTopUp;
            _goldPlus.Clicked -= OpenTopUp;
            _close.Clicked -= CloseTopUp;
            Unsubscribe();
            CloseTopUp();
        }

        #endregion

        #region Balances

        public void Bind(ICurrencyWallet wallet, IEventBus events)
        {
            Unsubscribe();
            _wallet = wallet;
            _events = events;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
            Refresh();
        }

        private void Subscribe()
        {
            if (_isSubscribed || _events == null)
            {
                return;
            }
            _events.Subscribe<CurrencyBalanceChanged>(OnBalanceChanged);
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }
            _events.Unsubscribe<CurrencyBalanceChanged>(OnBalanceChanged);
            _isSubscribed = false;
        }

        private void OnBalanceChanged(CurrencyBalanceChanged message)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_wallet == null)
            {
                return;
            }
            _cashAmount.text = RewardAmountFormatter.Format(_wallet.GetBalance(_cash.CurrencyId));
            _goldAmount.text = RewardAmountFormatter.Format(_wallet.GetBalance(_gold.CurrencyId));
        }

        public void SetDeathScreenVisible(bool isVisible)
        {
            if (!isVisible)
            {
                CloseTopUp();
            }
            SetGroupVisible(_balancesGroup, isVisible);
        }

        #endregion

        #region Top-up Panel

        public void OpenTopUp()
        {
            _hudVisibility.Hide();
            _deathScreenVisibility.Hide();
            SetGroupVisible(_topUpPanel, true);
        }

        public void CloseTopUp()
        {
            SetGroupVisible(_topUpPanel, false);
            _hudVisibility.Restore();
            _deathScreenVisibility.Restore();
        }

        private static void SetGroupVisible(CanvasGroup group, bool isVisible)
        {
            group.alpha = isVisible ? SHOWN_ALPHA : HIDDEN_ALPHA;
            group.interactable = isVisible;
            group.blocksRaycasts = isVisible;
        }

        #endregion
    }
}
