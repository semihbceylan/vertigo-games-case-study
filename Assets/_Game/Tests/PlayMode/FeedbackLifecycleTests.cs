using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class FeedbackLifecycleTests
    {
        [UnityTest]
        public IEnumerator DisableCancelsToastAndFadeWithoutResurrectingFeedback()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            Type type = Type.GetType("Vertigo.WheelOfFortune.UI.FeedbackView, Assembly-CSharp", true);
            Component view = null;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) view = component;
            }
            Assert.IsTrue(view);
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            var group = (CanvasGroup)type.GetField("group", FLAGS).GetValue(view);
            for (int i = 0; i < 3; i++)
            {
                type.GetMethod("ShowToast").Invoke(view, new object[] { "Test", Color.white, null });
                view.gameObject.SetActive(false);
                Assert.IsNull(type.GetField("_toastSequence", FLAGS).GetValue(view));
                Assert.IsNull(type.GetField("_fade", FLAGS).GetValue(view));
                Assert.AreEqual(0f, group.alpha);
                Assert.IsFalse(group.blocksRaycasts);
                view.gameObject.SetActive(true);
            }
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(0f, group.alpha);
            Assert.IsFalse(group.interactable);
        }
    }
}
