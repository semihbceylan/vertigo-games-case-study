using System;
using TMPro;
using UnityEngine;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>Persistent balances and an explicitly non-transactional demo purchase panel.</summary>
    public sealed class CurrencyHudView : MonoBehaviour
    {
        [SerializeField] private CurrencyDefinition _cash;
        [SerializeField] private CurrencyDefinition _gold;
        [SerializeField] private TMP_Text _cashAmount;
        [SerializeField] private TMP_Text _goldAmount;
        [SerializeField] private UIButton _cashPlus;
        [SerializeField] private UIButton _goldPlus;
        [SerializeField] private CanvasGroup _topUpPanel;
        [SerializeField] private UIButton _close;
        [SerializeField] private CanvasGroup[] _underlyingHud;
        [SerializeField] private CanvasGroup _balancesGroup;
        [SerializeField] private CanvasGroup[] _deathScreenContent;

        private ModalHudVisibility _hudVisibility;
        private ModalHudVisibility _deathScreenVisibility;
        private ICurrencyWallet _wallet;
        private IEventBus _events;

        public event Action TopUpRequested;
        public bool IsTopUpOpen { get { return _topUpPanel && _topUpPanel.blocksRaycasts; } }

        private void Awake()
        {
            _hudVisibility = new ModalHudVisibility(_underlyingHud);
            _deathScreenVisibility = new ModalHudVisibility(_deathScreenContent);
            CloseTopUp();
        }

        public void Bind(ICurrencyWallet wallet, IEventBus events)
        {
            Unsubscribe();
            _wallet = wallet;
            _events = events;
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }

        private void OnEnable()
        {
            _cashPlus.Clicked += RequestTopUp;
            _goldPlus.Clicked += RequestTopUp;
            _close.Clicked += CloseTopUp;
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            _cashPlus.Clicked -= RequestTopUp;
            _goldPlus.Clicked -= RequestTopUp;
            _close.Clicked -= CloseTopUp;
            Unsubscribe();
            CloseTopUp();
        }

        private void Subscribe()
        {
            _events?.Subscribe<CurrencyBalanceChanged>(OnBalanceChanged);
        }

        private void Unsubscribe()
        {
            _events?.Unsubscribe<CurrencyBalanceChanged>(OnBalanceChanged);
        }

        private void OnBalanceChanged(CurrencyBalanceChanged message)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (_wallet == null) return;
            _cashAmount.text = RewardAmountFormatter.Format(_wallet.GetBalance(_cash.CurrencyId));
            _goldAmount.text = RewardAmountFormatter.Format(_wallet.GetBalance(_gold.CurrencyId));
        }

        public void SetDeathScreenVisible(bool visible)
        {
            if (!visible && IsTopUpOpen) CloseTopUp();
            _balancesGroup.alpha = visible ? 1f : 0f;
            _balancesGroup.interactable = visible;
            _balancesGroup.blocksRaycasts = visible;
        }

        private void RequestTopUp()
        {
            TopUpRequested?.Invoke();
        }

        public void OpenTopUp()
        {
            _hudVisibility.Hide();
            _deathScreenVisibility.Hide();
            _topUpPanel.alpha = 1f;
            _topUpPanel.interactable = true;
            _topUpPanel.blocksRaycasts = true;
        }

        public void CloseTopUp()
        {
            if (!_topUpPanel) return;
            _topUpPanel.alpha = 0f;
            _topUpPanel.interactable = false;
            _topUpPanel.blocksRaycasts = false;
            _hudVisibility?.Restore();
            _deathScreenVisibility?.Restore();
        }
    }
}
