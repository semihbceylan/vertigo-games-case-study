using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Vertigo.WheelOfFortune.Core;

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
        [Obsolete("Use ZoneMaps instead.")]
        public ZoneMapsView ZoneBadges => zoneMaps;
        public RewardsListView RewardsList => rewardsList;
        public UIButton LeaveButton => leaveButton;
        public UIButton RestartButton => restartButton;
        public FeedbackView Feedback => feedback;
        public ResultsView Results => results;
        public JackpotShowcaseView Showcase => showcase;

        private void Awake()
        {
            // Editor OnValidate does not run in builds, so resolve at runtime as well.
            if (showcase == null) showcase = GetComponentInChildren<JackpotShowcaseView>(true);
        }

        /// <summary>Returns the showcase, creating one if the scene does not have it yet.</summary>
        public JackpotShowcaseView EnsureShowcase()
        {
            if (showcase == null) showcase = GetComponentInChildren<JackpotShowcaseView>(true);
            if (showcase == null)
            {
                var go = new GameObject("ui_jackpot_showcase", typeof(RectTransform), typeof(JackpotShowcaseView));
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.SetAsLastSibling();
                showcase = go.GetComponent<JackpotShowcaseView>();
            }
            return showcase;
        }

        public void SetZone(int zone, int safeInterval, int superInterval, int finalZone = 0)
        {
            if (zoneBar != null) zoneBar.Set(zone, safeInterval, superInterval, finalZone);
            if (zoneMaps != null) zoneMaps.Set(zone, safeInterval, superInterval, finalZone);
        }

        /// <summary>Updates only the side maps, leaving the zone strip untouched (used while it animates).</summary>
        public void SetZoneMaps(int zone, int safeInterval, int superInterval, int finalZone = 0)
        {
            if (zoneMaps != null) zoneMaps.Set(zone, safeInterval, superInterval, finalZone);
        }

        [Obsolete("Use SetZoneMaps instead.")]
        public void SetZoneBadges(int zone, int safeInterval, int superInterval, int finalZone = 0)
        {
            SetZoneMaps(zone, safeInterval, superInterval, finalZone);
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
            if (feedback != null) feedback.ShowToast("+" + amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + "  " + label, Color.white, icon);
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
            if (feedback != null) feedback.ShowBanner("OH NO, A BOMB EXPLODED RIGHT IN YOUR HANDS!", Color.white, bombIcon, "RESTART", true);
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
            if (zoneBar == null) zoneBar = FindByName<ZoneProgressBar>("ui_zone_progress_bar");
            if (zoneMaps == null) zoneMaps = FindByName<ZoneMapsView>("ui_game_zone_maps");
            if (zoneMaps == null) zoneMaps = FindByName<ZoneMapsView>("ui_game_zone_badges");
            if (rewardsList == null) rewardsList = FindByName<RewardsListView>("ui_rewards_list");
            if (feedback == null) feedback = FindByName<FeedbackView>("ui_feedback_overlay");
            if (results == null) results = FindByName<ResultsView>("ui_results_screen");
            if (showcase == null) showcase = FindByName<JackpotShowcaseView>("ui_jackpot_showcase");

            if (leaveButton == null) leaveButton = FindByName<UIButton>("ui_button_leave");
            if (restartButton == null) restartButton = FindByName<UIButton>("ui_button_restart");
        }

        private RectTransform FindTransform(string name)
        {
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name == name) return all[i] as RectTransform;
            }
            return null;
        }

        private T FindByName<T>(string name) where T : Component
        {
            var t = FindTransform(name);
            return t != null ? t.GetComponent<T>() : null;
        }
#endif
    }
}
