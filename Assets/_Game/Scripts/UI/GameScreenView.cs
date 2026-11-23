using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Session-facing facade over the HUD widgets. Child references are serialized and only
    /// auto-filled by name inside OnValidate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameScreenView : MonoBehaviour, ISessionScreenView
    {
        #region Serialized References

        [SerializeField] private ZoneProgressBar _zoneBar;
        [SerializeField] private ZoneMapsView _zoneMaps;
        [SerializeField] private RewardsListView _rewardsList;
        [SerializeField] private UIButton _leaveButton;
        [SerializeField] private FeedbackView _feedback;
        [SerializeField] private ResultsView _results;
        [SerializeField] private JackpotShowcaseView _showcase;

        #endregion

        #region HUD

        public void SetZone(int zone, ZoneCatalog zones)
        {
            _zoneBar.Set(zone, zones);
            _zoneMaps.Set(zone, zones);
        }

        /// <summary>Updates only the side maps, leaving the zone strip untouched while it animates.</summary>
        public void SetZoneMaps(int zone, ZoneCatalog zones)
        {
            _zoneMaps.Set(zone, zones);
        }

        public void SetRewards(IReadOnlyList<RewardRowData> rows)
        {
            _rewardsList.SetRows(rows);
        }

        public void SetLeaveAvailable(bool isAvailable)
        {
            _leaveButton.IsInteractable = isAvailable;
        }

        /// <summary>Shows or hides the leave button entirely (hidden while the loss panel is up).</summary>
        public void SetLeaveVisible(bool isVisible)
        {
            _leaveButton.gameObject.SetActive(isVisible);
        }

        public Sequence PlayZoneTransition(int zone, ZoneCatalog zones)
        {
            return _zoneBar.PlayTransition(zone, zones);
        }

        #endregion

        #region Panels

        public void ShowBomb(Sprite bombIcon)
        {
            _feedback.ShowLoss(bombIcon);
        }

        public void SetReviveOffer(long cost, int remaining, IReadOnlyList<RewardRowData> rows)
        {
            _feedback.SetReviveOffer(cost, remaining);
            _feedback.SetRecoverableRewards(rows);
        }

        public void ShowResults(IReadOnlyList<RewardRowData> rows, bool isMissionComplete)
        {
            _feedback.HideImmediate();
            _results.Show(rows, isMissionComplete);
        }

        public void HideJackpot()
        {
            _showcase.HideImmediate();
        }

        public void HideFeedback()
        {
            _feedback.Hide();
        }

        public void ResetPanels()
        {
            _feedback.HideImmediate();
            _results.Hide();
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (!_zoneBar && child.name == "ui_panel_zone_progress_bar") _zoneBar = child.GetComponent<ZoneProgressBar>();
                if (!_zoneMaps && child.name == "ui_panel_game_zone_maps") _zoneMaps = child.GetComponent<ZoneMapsView>();
                if (!_rewardsList && child.name == "ui_list_rewards") _rewardsList = child.GetComponent<RewardsListView>();
                if (!_feedback && child.name == "ui_panel_feedback_overlay") _feedback = child.GetComponent<FeedbackView>();
                if (!_results && child.name == "ui_panel_results_screen") _results = child.GetComponent<ResultsView>();
                if (!_showcase && child.name == "ui_panel_jackpot_showcase") _showcase = child.GetComponent<JackpotShowcaseView>();
                if (!_leaveButton && child.name == "ui_button_leave") _leaveButton = child.GetComponent<UIButton>();
            }
            if (!_zoneBar || !_zoneMaps || !_rewardsList || !_feedback || !_results || !_showcase || !_leaveButton)
            {
                Debug.LogError("GameScreenView has missing serialized widget references.", this);
            }
        }

        #endregion
#endif
    }
}
