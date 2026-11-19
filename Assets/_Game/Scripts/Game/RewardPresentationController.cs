using System;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.UI;
using Vertigo.WheelOfFortune.Wheel;

namespace Vertigo.WheelOfFortune.Game
{
    public interface IRewardPresentation
    {
        void Play(RewardGrant grant, RewardDefinition definition, int sliceIndex, Action onComplete);
        void Cancel();
    }

    /// <summary>Owns showcase dismissal and reward flights, never session progression.</summary>
    public sealed class RewardPresentationController : MonoBehaviour, IRewardPresentation
    {
        [SerializeField] private GameScreenView _screen;
        [SerializeField] private WheelLayout _wheelLayout;
        [SerializeField] private RewardFlyAnimator _rewardFly;

        private Action _onComplete;
        private string _rewardId;
        private int _sliceIndex;

        private void OnEnable()
        {
            if (_screen && _screen.Showcase) _screen.Showcase.Dismissed += OnShowcaseDismissed;
        }

        private void OnDisable()
        {
            if (_screen && _screen.Showcase) _screen.Showcase.Dismissed -= OnShowcaseDismissed;
            Cancel();
        }

        private void OnDestroy()
        {
            Cancel();
        }

        public void Play(RewardGrant grant, RewardDefinition definition, int sliceIndex, Action onComplete)
        {
            Cancel();
            _rewardId = grant.RewardId;
            _sliceIndex = sliceIndex;
            _onComplete = onComplete;
            if (definition && definition.ShowcaseBig)
            {
                _screen.HideFeedback();
                _screen.ShowJackpot(definition.Icon, definition.DisplayName, grant.Amount);
                return;
            }
            _screen.ShowReward(definition ? definition.Icon : null,
                definition ? definition.DisplayName : grant.RewardId, grant.Amount);
            PlayFlight();
        }

        public void Cancel()
        {
            _onComplete = null;
            if (_rewardFly) _rewardFly.CancelAll();
            if (_screen) _screen.HideJackpot();
        }

        private void OnShowcaseDismissed()
        {
            if (_onComplete == null) return;
            PlayFlight();
        }

        private void PlayFlight()
        {
            Image source = _wheelLayout.GetIcon(_sliceIndex);
            if (source && source.sprite)
            {
                RectTransform target;
                if (!_screen.RewardsList.TryGetRowIcon(_rewardId, out target))
                {
                    target = _screen.RewardsList.ListRect;
                }
                _rewardFly.Fly(source.sprite, source.rectTransform, target, Complete);
                return;
            }
            Complete();
        }

        private void Complete()
        {
            Action completion = _onComplete;
            _onComplete = null;
            if (completion == null) return;
            if (_screen && _screen.RewardsList) _screen.RewardsList.PunchRow(_rewardId);
            completion();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_screen) _screen = GetComponentInChildren<GameScreenView>(true);
            if (!_wheelLayout) _wheelLayout = GetComponentInChildren<WheelLayout>(true);
            if (!_rewardFly) _rewardFly = GetComponentInChildren<RewardFlyAnimator>(true);
            if (gameObject.activeInHierarchy &&
                (!_screen || !_screen.Showcase || !_screen.RewardsList || !_wheelLayout || !_rewardFly))
            {
                Debug.LogError("Reward presentation requires serialized screen, showcase, rewards list, wheel layout and fly animator references.", this);
            }
        }
#endif
    }
}
