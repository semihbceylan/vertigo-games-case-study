using System;
using UnityEngine;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.UI;
using Vertigo.WheelOfFortune.Wheel;

namespace Vertigo.WheelOfFortune.Game
{
    /// <summary>
    /// Plays what the session asks for and reports completion through the supplied callbacks.
    /// It never decides flow: the session's state objects do.
    /// </summary>
    public sealed class SessionPresenter : ISessionPresentation
    {
        #region Construction

        private readonly ZoneCatalog _zones;
        private readonly IWheelView _wheel;
        private readonly IWheelSpinPresentation _spinner;
        private readonly ISessionScreenView _screen;
        private readonly IRewardPresentation _rewards;
        private readonly IWheelTransition _transition;
        private readonly IWheelVisualPresenter _visuals;
        private readonly RewardCatalogPresenter _catalog;
        private readonly IBombImpact _bombImpact;
        private readonly ICurrencyHudView _currencyHud;
        private readonly Vector2 _wheelHome;
        private readonly Action<IWheelDisc> _populateIncoming;
        private readonly Action _finishZoneTransition;
        private readonly Action _openLossPanel;
        private GameSession _session;
        private ISessionHudPresenter _hud;
        private Action _zoneComplete;
        private int _lossZone;

        public SessionPresenter(ZoneCatalog zones, IWheelView wheel, IWheelSpinPresentation spinner,
            ISessionScreenView screen, IRewardPresentation rewards, IWheelTransition transition,
            IWheelVisualPresenter visuals, RewardCatalogPresenter catalog, IBombImpact bombImpact,
            ICurrencyHudView currencyHud)
        {
            _zones = zones;
            _wheel = wheel;
            _spinner = spinner;
            _screen = screen;
            _rewards = rewards;
            _transition = transition;
            _visuals = visuals;
            _catalog = catalog;
            _bombImpact = bombImpact;
            _currencyHud = currencyHud;
            _wheelHome = wheel.Disc.Rect.anchoredPosition;
            // Cache delegates once so each zone transition does not allocate new ones.
            _populateIncoming = PopulateIncoming;
            _finishZoneTransition = FinishZoneTransition;
            _openLossPanel = OpenLossPanel;
        }

        public void Bind(GameSession session, ISessionHudPresenter hud)
        {
            if (_session != null)
            {
                throw new InvalidOperationException("Session presenter is already bound.");
            }
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
        }

        #endregion

        #region ISessionPresentation

        public void PlaySpin(int sliceIndex, ZoneWheel wheel, Action onComplete)
        {
            _spinner.SpinToSlice(sliceIndex, wheel.SliceCount, _wheel.Disc.Layout.StartAngle, onComplete);
        }

        public void PresentReward(RewardGrant grant, int sliceIndex, Action onComplete)
        {
            _hud.RefreshRewards();
            _rewards.Play(grant, _catalog.ResolveRequired(grant.RewardId), sliceIndex, onComplete);
        }

        /// <summary>Plays the bomb hit first; the loss panel and balances open when the impact hands off.</summary>
        public void ShowLoss(int zone, int bombSliceIndex)
        {
            _lossZone = zone;
            _bombImpact.Play(bombSliceIndex, _openLossPanel);
        }

        public void ShowResults()
        {
            _bombImpact.Cancel();
            _currencyHud.SetDeathScreenVisible(false);
            _screen.ShowResults(_catalog.BuildRows(_session.Wallet));
        }

        private void OpenLossPanel()
        {
            _screen.ShowBomb(_zones.GetWheelForZone(_lossZone).BombSprite);
            _screen.SetReviveOffer(_session.ReviveCost, _session.RevivesRemaining,
                _catalog.BuildRowsFromTotals(_session.RecoverableRewards));
            _currencyHud.SetDeathScreenVisible(true);
        }

        public void PlayZoneTransition(Action onComplete)
        {
            _zoneComplete = onComplete;
            _screen.HideFeedback();
            _transition.Play(_populateIncoming, _screen.PlayZoneTransition(_session.Zone, _zones), _finishZoneTransition);
        }

        public void Reset()
        {
            Cancel();
            ApplyZoneWheel();
            _screen.ResetPanels();
        }

        /// <summary>Stops every presentation and drops pending callbacks without advancing the session.</summary>
        public void Cancel()
        {
            _zoneComplete = null;
            _spinner.CancelSpin();
            _rewards.Cancel();
            _transition.Cancel();
            _bombImpact.Cancel();
            _currencyHud.SetDeathScreenVisible(false);
            _screen.HideJackpot();
        }

        #endregion

        #region Wheel

        public void ApplyZoneWheel()
        {
            WheelDefinition definition = _zones.GetWheelForZone(_session.Zone);
            _wheel.Disc.Rect.anchoredPosition = _wheelHome;
            _wheel.SetIndicator(definition.IndicatorSprite);
            _visuals.Fill(_session.CurrentWheel, definition, _wheel.Disc);
        }

        private void PopulateIncoming(IWheelDisc incoming)
        {
            _visuals.Fill(_session.CurrentWheel, _zones.GetWheelForZone(_session.Zone), incoming);
        }

        private void FinishZoneTransition()
        {
            ApplyZoneWheel();
            Action completion = _zoneComplete;
            _zoneComplete = null;
            if (completion != null)
            {
                completion();
            }
        }

        #endregion
    }
}
