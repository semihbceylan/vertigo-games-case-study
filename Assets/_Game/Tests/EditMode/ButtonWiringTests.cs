using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ButtonWiringTests
    {
        [Test]
        public void SceneButtonsHaveSerializedTargetsAndNoPersistentCalls()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Game/Scenes/Game.unity");
            try
            {
                Type type = Type.GetType("Vertigo.WheelOfFortune.UI.UIButton, Assembly-CSharp", true);
                int count = 0;
                foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
                {
                    var component = (Component)candidate;
                    if (component.gameObject.scene != scene) continue;
                    count++;
                    var serialized = new SerializedObject(component);
                    var button = (UnityEngine.UI.Button)serialized.FindProperty("_button").objectReferenceValue;
                    Assert.IsTrue(button, component.name);
                    Assert.AreEqual(0, button.onClick.GetPersistentEventCount(), component.name);
                }
                Assert.Greater(count, 0);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
