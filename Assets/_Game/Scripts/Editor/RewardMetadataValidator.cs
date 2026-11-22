#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.EditorTools
{
    /// <summary>Reports rewards with missing authored metadata; never infers behaviour from IDs.</summary>
    public static class RewardMetadataValidator
    {
        #region Menu

        [MenuItem("Tools/Wheel of Fortune/Validate Reward Metadata")]
        public static void ValidateMetadata()
        {
            string[] guids = AssetDatabase.FindAssets("t:RewardDefinition");
            int invalidCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RewardDefinition reward = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
                if (!reward.Category || string.IsNullOrWhiteSpace(reward.RewardId) || !reward.Icon)
                {
                    Debug.LogWarning("Reward requires an authored ID, category and icon: " + path, reward);
                    invalidCount++;
                }
            }
            Debug.Log("Reward metadata validation: " + guids.Length + " scanned, " + invalidCount + " incomplete.");
        }

        #endregion
    }
}
#endif
