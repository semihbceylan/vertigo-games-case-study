using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.UI;

namespace Vertigo.WheelOfFortune.Game
{
    public interface ISessionHudPresenter
    {
        void Refresh();
        void RefreshRewards();
    }

    /// <summary>
    /// Projects the session's state-object capabilities into HUD widgets whenever the session
    /// publishes <see cref="SessionChanged"/>. It never changes session flow.
    /// </summary>
    public sealed class SessionHudPresenter : ISessionHudPresenter
    {
        #region Construction

        private readonly GameSession _session;
        private readonly ZoneCatalog _zones;
        private readonly ISessionHudView _screen;
        private readonly IButtonInteractability _spinButton;
        private readonly ICurrencyHudView _currencyHud;
        private readonly RewardCatalogPresenter _catalog;
        private bool _isSubscribed;

        public SessionHudPresenter(GameSession session, ZoneCatalog zones, ISessionHudView screen,
            IButtonInteractability spinButton, ICurrencyHudView currencyHud, RewardCatalogPresenter catalog)
        {
            _session = session;
            _zones = zones;
            _screen = screen;
            _spinButton = spinButton;
            _currencyHud = currencyHud;
            _catalog = catalog;
        }

        #endregion

        #region Subscription

        /// <summary>Idempotent so repeated enable cycles never register a second listener.</summary>
        public void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }
            _session.Events.Subscribe<SessionChanged>(OnSessionChanged);
            _isSubscribed = true;
        }

        public void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }
            _session.Events.Unsubscribe<SessionChanged>(OnSessionChanged);
            _isSubscribed = false;
        }

        private void OnSessionChanged(SessionChanged message)
        {
            Refresh();
        }

        #endregion

        #region Projection

        public void Refresh()
        {
            if (_session.IsZoneTransitioning)
            {
                _screen.SetZoneMaps(_session.Zone, _zones);
            }
            else
            {
                _screen.SetZone(_session.Zone, _zones);
            }
            _screen.SetLeaveAvailable(_session.CanLeave);
            _spinButton.IsInteractable = _session.CanSpin;
            _screen.SetLeaveVisible(!_session.CanRestart);
            _screen.SetRestartAvailable(_session.IsLost);
            _currencyHud.SetDeathScreenVisible(_session.IsLost);
            RefreshRewards();
        }

        public void RefreshRewards()
        {
            _screen.SetRewards(_catalog.BuildRows(_session.Wallet));
        }

        #endregion
    }
}
