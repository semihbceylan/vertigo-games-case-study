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
        [SerializeField] private CountUpAnimation _countAnimation;

        private ModalHudVisibility _hudVisibility;
        private ModalHudVisibility _deathScreenVisibility;
        private AmountCounter _cashCounter;
        private AmountCounter _goldCounter;
        private ICurrencyWallet _wallet;
        private IEventBus _events;
        private bool _isSubscribed;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _hudVisibility = new ModalHudVisibility(_underlyingHud);
            _deathScreenVisibility = new ModalHudVisibility(_deathScreenContent);
            EnsureCounters();
            CloseTopUp();
        }

        private void OnEnable()
        {
            _cashPlus.Clicked += OpenTopUp;
            _goldPlus.Clicked += OpenTopUp;
            _close.Clicked += CloseTopUp;
            Subscribe();
            Refresh(false);
        }

        private void OnDisable()
        {
            _cashPlus.Clicked -= OpenTopUp;
            _goldPlus.Clicked -= OpenTopUp;
            _close.Clicked -= CloseTopUp;
            Unsubscribe();
            CloseTopUp();
            if (_cashCounter != null)
            {
                _cashCounter.Complete();
                _goldCounter.Complete();
            }
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
            Refresh(false);
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
            Refresh(true);
        }

        /// <summary>Binding shows balances immediately; later balance changes roll to the new value.</summary>
        private void Refresh(bool isAnimated)
        {
            if (_wallet == null)
            {
                return;
            }
            EnsureCounters();
            ShowBalance(_cashCounter, _cash, isAnimated);
            ShowBalance(_goldCounter, _gold, isAnimated);
        }

        /// <summary>Bind can run before Awake when the HUD starts inactive, so counters are made on first use.</summary>
        private void EnsureCounters()
        {
            if (_cashCounter != null)
            {
                return;
            }
            _cashCounter = new AmountCounter(_cashAmount, gameObject);
            _goldCounter = new AmountCounter(_goldAmount, gameObject);
        }

        private void ShowBalance(AmountCounter counter, CurrencyDefinition currency, bool isAnimated)
        {
            long balance = _wallet.GetBalance(currency.CurrencyId);
            if (isAnimated)
            {
                counter.CountTo(balance, _countAnimation);
                return;
            }
            counter.Set(balance);
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

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_cash || !_gold || !_cashAmount || !_goldAmount || !_countAnimation)
            {
                Debug.LogError("CurrencyHudView requires serialized currencies, balance labels and a count-up profile.", this);
            }
        }

        #endregion
#endif
    }
}
