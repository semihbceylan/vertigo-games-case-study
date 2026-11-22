using UnityEngine;

namespace Vertigo.WheelOfFortune.Data
{
    /// <summary>
    /// Display grouping for accumulated rewards. Adding a category is a new asset, not a code change;
    /// lower sort orders are listed first in the rewards panel and the results summary.
    /// </summary>
    [CreateAssetMenu(menuName = "Wheel of Fortune/Reward Category", fileName = "RewardCategory_")]
    public sealed class RewardCategoryDefinition : ScriptableObject
    {
        #region Serialized Values

        [SerializeField] private string _displayName = "Category";
        [SerializeField, Min(0)] private int _sortOrder;

        #endregion

        #region Accessors

        public string DisplayName
        {
            get { return _displayName; }
        }

        public int SortOrder
        {
            get { return _sortOrder; }
        }

        #endregion
    }
}
