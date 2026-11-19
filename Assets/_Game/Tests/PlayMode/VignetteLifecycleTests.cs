using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class VignetteLifecycleTests
    {
        [UnityTest]
        public IEnumerator DisableReleasesOwnedSpriteAndTextureAndReenableRebuilds()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Type type = Type.GetType("Vertigo.WheelOfFortune.UI.ScreenVignette, Assembly-CSharp", true);
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            int count = 0;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
            {
                var view = (Behaviour)candidate;
                if (view.gameObject.scene != SceneManager.GetActiveScene() || !view.isActiveAndEnabled) continue;
                count++;
                var image = (UnityEngine.UI.Image)type.GetField("target", FLAGS).GetValue(view);
                Assert.IsTrue(image);
                Assert.IsTrue(image.sprite);
                view.enabled = false;
                Assert.IsFalse(image.sprite);
                Assert.IsNull(type.GetField("_sprite", FLAGS).GetValue(view));
                Assert.IsNull(type.GetField("_texture", FLAGS).GetValue(view));
                view.enabled = true;
                Assert.IsTrue(image.sprite);
            }
            Assert.Greater(count, 0);
            yield return null;
        }
    }
}
