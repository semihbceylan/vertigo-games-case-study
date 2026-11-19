using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ShowcaseWiringTests
    {
        [Test]
        public void GameSceneShowcaseHasAuthoredWidgetsAnimationAndHud()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Game/Scenes/Game.unity");
            try
            {
                Type type = Type.GetType("Vertigo.WheelOfFortune.UI.JackpotShowcaseView, Assembly-CSharp", true);
                int count = 0;
                foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
                {
                    var component = (Component)candidate;
                    if (component.gameObject.scene != scene) continue;
                    count++;
                    var serialized = new SerializedObject(component);
                    foreach (string field in new[] { "_group", "_dismissButton", "_flash", "_bigIcon",
                        "_titleLabel", "_amountLabel", "_hintLabel", "_animation" })
                    {
                        Assert.IsTrue(serialized.FindProperty(field).objectReferenceValue, field);
                    }
                    var hud = serialized.FindProperty("_underlyingHud");
                    Assert.Greater(hud.arraySize, 0);
                    for (int i = 0; i < hud.arraySize; i++)
                    {
                        Assert.IsTrue(hud.GetArrayElementAtIndex(i).objectReferenceValue);
                    }
                    var flash = (UnityEngine.UI.Image)serialized.FindProperty("_flash").objectReferenceValue;
                    Assert.IsTrue(flash.sprite);
                    Assert.IsTrue(flash.preserveAspect);
                }
                Assert.AreEqual(1, count);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
