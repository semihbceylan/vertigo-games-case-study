using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>
    /// Display grouping for accumulated rewards. Declaration order doubles as the sort order used
    /// when the rewards panel and the end-of-run summary are built (see GameController.BuildRewardRows).
    /// <see cref="Uncategorized"/> is the default so a reward that has not been assigned yet sinks to
    /// the bottom of the list instead of jumping to the top.
    /// </summary>
    public enum RewardCategory
    {
        Uncategorized = 0,
        WeaponRender = 1,
        Helmet = 2,
        Chest = 3,
        Currency = 4,
        Consumable = 5,
        WeaponPoints = 6,
    }

    /// <summary>
    /// Editor-configurable description of a single reward that can appear on a wheel slice.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Reward", fileName = "Reward_")]
    public sealed class RewardDefinition : ScriptableObject
    {
        [SerializeField] private string rewardId = "reward";
        [SerializeField] private string displayName = "Reward";
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0)] private long amount = 1;
        [Tooltip("Display grouping; controls where this reward sorts in the rewards list and results summary.")]
        [SerializeField] private RewardCategory category = RewardCategory.Uncategorized;

        [Header("Jackpot showcase")]
        [Tooltip("When enabled, this reward pops up big fullscreen until the player taps to dismiss (use for weapon Renders and melee).")]
        [SerializeField] private bool showcaseBig = false;

        public string RewardId => rewardId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public long Amount => amount;
        public RewardCategory Category => category;
        public bool ShowcaseBig => showcaseBig;
    }
}
