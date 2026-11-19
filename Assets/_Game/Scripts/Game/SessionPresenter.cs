using System;
using UnityEngine;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.UI;
using Vertigo.WheelOfFortune.Wheel;

namespace Vertigo.WheelOfFortune.Game
{
    /// <summary>Owns session presentation and completion callbacks, never authoritative flow.</summary>
    public sealed class SessionPresenter : ISessionPresentation
    {
        private readonly ZoneConfiguration _zones;
        private readonly WheelView _wheel;
        private readonly WheelLayout _layout;
        private readonly WheelSpinner _spinner;
        private readonly GameScreenView _screen;
        private readonly IRewardPresentation _rewards;
        private readonly IWheelTransition _transition;
        private readonly WheelTransitionAnimation _transitionAnimation;
        private readonly IWheelVisualPresenter _visuals;
        private readonly RewardCatalogPresenter _catalog;
        private readonly Vector2 _wheelHome;
        private GameSession _session;
        private ISessionHudPresenter _hud;
        private Action _zoneComplete;

        public SessionPresenter(ZoneConfiguration zones, WheelView wheel, WheelLayout layout,
            WheelSpinner spinner, GameScreenView screen, IRewardPresentation rewards,
            IWheelTransition transition, WheelTransitionAnimation transitionAnimation,
            IWheelVisualPresenter visuals, RewardCatalogPresenter catalog)
        {
            _zones = zones;
            _wheel = wheel;
            _layout = layout;
            _spinner = spinner;
            _screen = screen;
            _rewards = rewards;
            _transition = transition;
            _transitionAnimation = transitionAnimation;
            _visuals = visuals;
            _catalog = catalog;
            _wheelHome = wheel.WheelRotation.anchoredPosition;
        }

        public void Bind(GameSession session, ISessionHudPresenter hud)
        {
            if (_session != null) throw new InvalidOperationException("Session presenter is already bound.");
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
        }

        public void PlaySpin(int sliceIndex, ZoneWheel wheel, Action onComplete)
        {
            _spinner.SpinToSlice(sliceIndex, wheel.SliceCount, _layout.StartAngle, onComplete);
        }

        public void PresentReward(RewardGrant grant, int sliceIndex, Action onComplete)
        {
            _hud.RefreshRewards();
            _rewards.Play(grant, _catalog.Resolve(grant.RewardId), sliceIndex, onComplete);
        }

        public void ShowLoss(int zone)
        {
            _screen.ShowBomb(_zones.GetWheelForZone(zone).BombSprite);
        }

        public void ShowResults()
        {
            _screen.Feedback.HideImmediate();
            _screen.Results.Show(_catalog.BuildRows(_session.Wallet));
        }

        public void PlayZoneTransition(Action onComplete)
        {
            _zoneComplete = onComplete;
            _screen.HideFeedback();
            var strip = _screen.ZoneBar.PlayTransition(_session.Zone, _zones,
                _transitionAnimation.SlideDuration, _transitionAnimation.SlideEase);
            _transition.Play(PopulateIncoming, strip, FinishZoneTransition);
        }

        private void PopulateIncoming(WheelDiscView incoming)
        {
            _visuals.Fill(_session.CurrentWheel, _zones.GetWheelForZone(_session.Zone),
                incoming.Rect, incoming.Base, incoming.Layout);
        }

        private void FinishZoneTransition()
        {
            ApplyZoneWheel();
            Action completion = _zoneComplete;
            _zoneComplete = null;
            completion?.Invoke();
        }

        public void ApplyZoneWheel()
        {
            WheelDefinition definition = _zones.GetWheelForZone(_session.Zone);
            _wheel.WheelRotation.anchoredPosition = _wheelHome;
            if (_wheel.WheelIndicator && definition.IndicatorSprite)
            {
                _wheel.WheelIndicator.sprite = definition.IndicatorSprite;
            }
            _visuals.Fill(_session.CurrentWheel, definition, _wheel.WheelRotation, _wheel.WheelBase, _layout);
        }

        public void Reset()
        {
            Cancel();
            ApplyZoneWheel();
            _screen.Feedback.HideImmediate();
            _screen.Results.Hide();
        }

        public void Cancel()
        {
            _zoneComplete = null;
            _spinner.CancelSpin();
            _rewards.Cancel();
            _transition.Cancel();
            _screen.HideJackpot();
        }
    }
}
