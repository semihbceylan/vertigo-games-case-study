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
        #region Serialized References

        [SerializeField] private JackpotShowcaseView _showcaseView;
        [SerializeField] private RewardsListView _rewardsListView;
        [SerializeField] private RewardFlyAnimator _rewardFlyView;
        [SerializeField] private WheelLayout _wheelLayout;

        #endregion

        #region Runtime State

        private IRewardShowcase _showcase;
        private IRewardRowTargets _rows;
        private IRewardFlight _flight;
        private Action _onComplete;
        private string _rewardId;
        private int _sliceIndex;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _showcase = _showcaseView;
            _rows = _rewardsListView;
            _flight = _rewardFlyView;
        }

        private void OnEnable()
        {
            _showcase.Dismissed += OnShowcaseDismissed;
        }

        private void OnDisable()
        {
            _showcase.Dismissed -= OnShowcaseDismissed;
            Cancel();
        }

        #endregion

        #region Presentation

        public void Play(RewardGrant grant, RewardDefinition definition, int sliceIndex, Action onComplete)
        {
            Cancel();
            _rewardId = grant.RewardId;
            _sliceIndex = sliceIndex;
            _onComplete = onComplete;
            if (definition.ShowcaseBig)
            {
                _showcase.Show(definition.Icon, definition.DisplayName, grant.Amount);
                return;
            }
            PlayFlight();
        }

        /// <summary>Drops the completion callback first so cancelled visuals cannot advance the session.</summary>
        public void Cancel()
        {
            _onComplete = null;
            _flight.CancelAll();
            _showcase.HideImmediate();
        }

        private void OnShowcaseDismissed()
        {
            if (_onComplete != null)
            {
                PlayFlight();
            }
        }

        private void PlayFlight()
        {
            Image source = _wheelLayout.GetIcon(_sliceIndex);
            RectTransform target;
            if (!_rows.TryGetRowIcon(_rewardId, out target))
            {
                target = _rows.ListRect;
            }
            _flight.Fly(source.sprite, source.rectTransform, target, Complete);
        }

        private void Complete()
        {
            Action completion = _onComplete;
            _onComplete = null;
            if (completion == null)
            {
                return;
            }
            _rows.PunchRow(_rewardId);
            completion();
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_showcaseView) _showcaseView = GetComponentInChildren<JackpotShowcaseView>(true);
            if (!_rewardFlyView) _rewardFlyView = GetComponentInChildren<RewardFlyAnimator>(true);
            if (!_showcaseView || !_rewardsListView || !_rewardFlyView || !_wheelLayout)
            {
                Debug.LogError("Reward presentation requires serialized showcase, rewards list, fly animator and wheel layout references.", this);
            }
        }

        #endregion
#endif
    }
}
