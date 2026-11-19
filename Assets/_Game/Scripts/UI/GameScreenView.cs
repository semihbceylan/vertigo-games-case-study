using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Aggregates the HUD widgets behind a single facade so the game controller only talks to one
    /// place. References resolve themselves by name in the editor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameScreenView : MonoBehaviour
    {
        [SerializeField] private ZoneProgressBar zoneBar;
        [FormerlySerializedAs("zoneBadges")]
        [SerializeField] private ZoneMapsView zoneMaps;
        [SerializeField] private RewardsListView rewardsList;
        [SerializeField] private UIButton leaveButton;
        [SerializeField] private UIButton restartButton;
        [SerializeField] private FeedbackView feedback;
        [SerializeField] private ResultsView results;
        [SerializeField] private JackpotShowcaseView showcase;

        public ZoneProgressBar ZoneBar => zoneBar;
        public ZoneMapsView ZoneMaps => zoneMaps;
        public RewardsListView RewardsList => rewardsList;
        public UIButton LeaveButton => leaveButton;
        public UIButton RestartButton => restartButton;
        public FeedbackView Feedback => feedback;
        public ResultsView Results => results;
        public JackpotShowcaseView Showcase => showcase;

        public void SetZone(int zone, ZoneConfiguration configuration)
        {
            if (zoneBar) zoneBar.Set(zone, configuration);
            if (zoneMaps) zoneMaps.Set(zone, configuration);
        }

        /// <summary>Updates only the side maps, leaving the zone strip untouched (used while it animates).</summary>
        public void SetZoneMaps(int zone, ZoneConfiguration configuration)
        {
            if (zoneMaps) zoneMaps.Set(zone, configuration);
        }

        public void SetRewards(IReadOnlyList<RewardRowData> rows)
        {
            if (rewardsList != null) rewardsList.SetRows(rows);
        }

        public void SetLeaveAvailable(bool available)
        {
            if (leaveButton != null) leaveButton.Interactable = available;
        }

        /// <summary>Shows or hides the leave button entirely (hidden while the death screen is up).</summary>
        public void SetLeaveVisible(bool visible)
        {
            if (leaveButton != null) leaveButton.gameObject.SetActive(visible);
        }

        public void SetRestartAvailable(bool available)
        {
            if (restartButton != null) restartButton.Interactable = available;
        }

        public void ShowReward(Sprite icon, string label, long amount)
        {
            if (feedback != null) feedback.ShowToast("+" + RewardAmountFormatter.Format(amount) + "  " + label, Color.white, icon);
        }

        /// <summary>Big jackpot popup for weapon Renders / melee. Stays until the player taps.</summary>
        public void ShowJackpot(Sprite icon, string label, long amount)
        {
            if (showcase != null) showcase.Show(icon, label, amount);
            else ShowReward(icon, label, amount);
        }

        public bool IsJackpotShowing => showcase != null && showcase.IsShowing;

        public void HideJackpot()
        {
            if (showcase != null) showcase.HideImmediate();
        }

        public void ShowBomb(Sprite bombIcon)
        {
            if (feedback != null) feedback.ShowBanner("OH NO, A BOMB EXPLODED RIGHT IN YOUR HANDS!", Color.white, bombIcon, "RESTART");
        }

        public void ShowComplete()
        {
            if (feedback != null) feedback.ShowBanner("COMPLETE!  Rewards banked", Color.white, null, "PLAY AGAIN");
        }

        public void HideFeedback()
        {
            if (feedback != null) feedback.Hide();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (!zoneBar && child.name == "ui_zone_progress_bar") zoneBar = child.GetComponent<ZoneProgressBar>();
                if (!zoneMaps && child.name == "ui_game_zone_maps") zoneMaps = child.GetComponent<ZoneMapsView>();
                if (!rewardsList && child.name == "ui_rewards_list") rewardsList = child.GetComponent<RewardsListView>();
                if (!feedback && child.name == "ui_feedback_overlay") feedback = child.GetComponent<FeedbackView>();
                if (!results && child.name == "ui_results_screen") results = child.GetComponent<ResultsView>();
                if (!showcase && child.name == "ui_jackpot_showcase") showcase = child.GetComponent<JackpotShowcaseView>();
                if (!leaveButton && child.name == "ui_button_leave") leaveButton = child.GetComponent<UIButton>();
                if (!restartButton && child.name == "ui_button_restart") restartButton = child.GetComponent<UIButton>();
            }
        }
#endif
    }
}
