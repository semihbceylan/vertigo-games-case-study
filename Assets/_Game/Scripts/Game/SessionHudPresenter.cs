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

    /// <summary>Projects authoritative session capabilities into HUD widgets; never changes session flow.</summary>
    public sealed class SessionHudPresenter : ISessionHudPresenter
    {
        private readonly GameSession _session;
        private readonly ZoneConfiguration _zones;
        private readonly ISessionHudView _screen;
        private readonly IButtonInteractability _spinButton;
        private readonly RewardCatalogPresenter _catalog;

        public SessionHudPresenter(GameSession session, ZoneConfiguration zones, ISessionHudView screen,
            IButtonInteractability spinButton, RewardCatalogPresenter catalog)
        {
            _session = session;
            _zones = zones;
            _screen = screen;
            _spinButton = spinButton;
            _catalog = catalog;
        }

        public void Refresh()
        {
            if (_session.IsZoneTransitioning) _screen.SetZoneMaps(_session.Zone, _zones);
            else _screen.SetZone(_session.Zone, _zones);
            _screen.SetLeaveAvailable(_session.CanLeave);
            _spinButton.Interactable = _session.CanSpin;
            _screen.SetLeaveVisible(!_session.CanRestart && !_session.IsCompleted);
            _screen.SetRestartAvailable(_session.CanRestart);
            RefreshRewards();
        }

        public void RefreshRewards()
        {
            _screen.SetRewards(_catalog.BuildRows(_session.Wallet));
        }
    }
}
