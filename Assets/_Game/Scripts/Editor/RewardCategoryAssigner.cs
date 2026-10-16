#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.EditorTools
{
    /// <summary>
    /// One-time (re-runnable) utility that stamps a <see cref="RewardCategory"/> onto every
    /// <see cref="RewardDefinition"/> asset so the rewards list and results summary sort in the
    /// intended display order. Safe to run again after adding rewards.
    /// </summary>
    public static class RewardCategoryAssigner
    {
        [MenuItem("Tools/Wheel of Fortune/Categorize Rewards")]
        public static void AssignCategories()
        {
            string[] guids = AssetDatabase.FindAssets("t:RewardDefinition");
            int changed = 0;
            var unclassified = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var reward = AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
                if (reward == null) continue;

                var category = Classify(reward.RewardId);
                if (category == RewardCategory.Uncategorized)
                    unclassified.Add(reward.RewardId + " (" + path + ")");

                var so = new SerializedObject(reward);
                var prop = so.FindProperty("category");
                if (prop == null || prop.enumValueIndex == (int)category) continue;

                prop.enumValueIndex = (int)category;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(reward);
                changed++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (unclassified.Count > 0)
            {
                Debug.LogWarning("[RewardCategoryAssigner] Uncategorized rewards (they will sort last):\n - " +
                                 string.Join("\n - ", unclassified));
            }

            Debug.Log($"[RewardCategoryAssigner] Categorized rewards. Updated {changed} asset(s), {guids.Length} scanned.");
        }

        /// <summary>Maps a reward id to its display category. Ids follow the asset naming in Data/Rewards.</summary>
        private static RewardCategory Classify(string rewardId)
        {
            if (string.IsNullOrEmpty(rewardId)) return RewardCategory.Uncategorized;

            string id = rewardId.ToLowerInvariant();

            // Weapon Renders (t1_/t2_/t3_) and melee bayonets.
            if (id.StartsWith("t1_") || id.StartsWith("t2_") || id.StartsWith("t3_") || id.StartsWith("bayonet"))
                return RewardCategory.WeaponRender;

            if (id == "pumpkin" || id == "aviator" || id == "cap" || id.Contains("helmet"))
                return RewardCategory.Helmet;

            if (id.StartsWith("chest"))
                return RewardCategory.Chest;

            if (id == "gold" || id == "cash")
                return RewardCategory.Currency;

            if (id.StartsWith("grenade") || id.StartsWith("health") || id == "molotov")
                return RewardCategory.Consumable;

            if (WeaponPointIds.Contains(id))
                return RewardCategory.WeaponPoints;

            return RewardCategory.Uncategorized;
        }

        private static readonly HashSet<string> WeaponPointIds = new HashSet<string>
        {
            "armor", "knife", "pistol", "rifle", "shotgun", "smg", "sniper"
        };
    }
}
#endif