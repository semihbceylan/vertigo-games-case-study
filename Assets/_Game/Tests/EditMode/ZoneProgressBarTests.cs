using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ZoneProgressBarTests
    {
        [Test]
        public void GameSceneHasCompleteZoneNodeReferences()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Game/Scenes/Game.unity");
            try
            {
                Type type = Type.GetType("Vertigo.WheelOfFortune.UI.ZoneProgressBar, Assembly-CSharp", true);
                int count = 0;
                foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
                {
                    var component = (Component)candidate;
                    if (component.gameObject.scene != scene) continue;
                    count++;
                    var serialized = new SerializedObject(component);
                    var nodes = serialized.FindProperty("_nodes");
                    Assert.AreEqual(serialized.FindProperty("_visibleNodes").intValue, nodes.arraySize);
                    for (int i = 0; i < nodes.arraySize; i++)
                    {
                        foreach (string field in new[] { "Rect", "Bg", "Frame", "Label" })
                        {
                            Assert.IsTrue(nodes.GetArrayElementAtIndex(i).FindPropertyRelative(field).objectReferenceValue,
                                "Missing node " + i + " reference: " + field);
                        }
                    }
                }
                Assert.AreEqual(1, count);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void UnwiredNodesAreRejectedRatherThanCreated()
        {
            Type type = Type.GetType("Vertigo.WheelOfFortune.UI.ZoneProgressBar, Assembly-CSharp", true);
            var owner = new GameObject("zone-strip-test", typeof(RectTransform));
            owner.SetActive(false);
            try
            {
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Zone strip requires"));
                Component component = owner.AddComponent(type);
                MethodInfo validate = type.GetMethod("ValidateNodes", BindingFlags.NonPublic | BindingFlags.Instance);
                var exception = Assert.Throws<TargetInvocationException>(() => validate.Invoke(component, null));
                Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
                Assert.AreEqual(0, owner.transform.childCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
