using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ResultsPoolTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void PoolIsPrewarmedFromPrefabAndRejectsOverflowWithoutCreatingTiles()
        {
            Type type = Type.GetType("Vertigo.WheelOfFortune.UI.ResultsView, Assembly-CSharp", true);
            Type tileType = Type.GetType("Vertigo.WheelOfFortune.UI.ResultTileView, Assembly-CSharp", true);
            Type rowType = Type.GetType("Vertigo.WheelOfFortune.UI.RewardRowData, Assembly-CSharp", true);
            var owner = new GameObject("results-pool-test", typeof(RectTransform), typeof(CanvasGroup));
            owner.SetActive(false);
            try
            {
                LogAssert.ignoreFailingMessages = true;
                Component view = owner.AddComponent(type);
                LogAssert.ignoreFailingMessages = false;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/ui_panel_result_tile.prefab");
                type.GetField("_group", PRIVATE_INSTANCE).SetValue(view, owner.GetComponent<CanvasGroup>());
                type.GetField("_content", PRIVATE_INSTANCE).SetValue(view, (RectTransform)owner.transform);
                type.GetField("_tilePrefab", PRIVATE_INSTANCE).SetValue(view, prefab.GetComponent(tileType));
                type.GetField("_animation", PRIVATE_INSTANCE).SetValue(view,
                    AssetDatabase.LoadMainAssetAtPath("Assets/_Game/Data/Animation_Results.asset"));
                type.GetField("_poolCapacity", PRIVATE_INSTANCE).SetValue(view, 3);
                type.GetMethod("Awake", PRIVATE_INSTANCE).Invoke(view, null);
                var tiles = (IList)type.GetField("_tiles", PRIVATE_INSTANCE).GetValue(view);
                Assert.AreEqual(3, tiles.Count);
                Assert.AreEqual(3, owner.transform.childCount);
                for (int i = 0; i < owner.transform.childCount; i++)
                {
                    Assert.IsFalse(owner.transform.GetChild(i).gameObject.activeSelf);
                }
                MethodInfo build = type.GetMethod("BuildTiles", PRIVATE_INSTANCE);
                build.Invoke(view, new object[] { Array.CreateInstance(rowType, 3) });
                Assert.IsTrue(owner.transform.GetChild(2).gameObject.activeSelf);
                var exception = Assert.Throws<TargetInvocationException>(
                    () => build.Invoke(view, new object[] { Array.CreateInstance(rowType, 4) }));
                Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
                Assert.AreEqual(3, owner.transform.childCount);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
