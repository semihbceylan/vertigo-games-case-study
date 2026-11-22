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
        public IEnumerator BannerHidesHudUntilFadeFinishesAndDisableRestoresPreviousState()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Type type = Type.GetType("Vertigo.WheelOfFortune.UI.FeedbackView, Assembly-CSharp", true);
            Component view = null;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) view = component;
            }
            Assert.IsTrue(view);
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            var groups = (CanvasGroup[])type.GetField("_underlyingHud", FLAGS).GetValue(view);
            Assert.Greater(groups.Length, 0);
            CanvasGroup hud = groups[0];
            hud.alpha = 0.7f;
            hud.interactable = false;
            hud.blocksRaycasts = true;
            var panel = (CanvasGroup)type.GetField("_group", FLAGS).GetValue(view);
            object profile = type.GetField("_animation", FLAGS).GetValue(view);
            float duration = (float)profile.GetType().GetProperty("FadeDuration").GetValue(profile);
            for (int i = 0; i < 2; i++)
            {
                type.GetMethod("ShowLoss").Invoke(view, new object[] { null });
                Assert.AreEqual(0f, hud.alpha);
                Assert.IsFalse(hud.interactable);
                Assert.IsFalse(hud.blocksRaycasts);
                type.GetMethod("Hide").Invoke(view, null);
                Assert.IsTrue(panel.blocksRaycasts);
                Assert.AreEqual(0f, hud.alpha);
                yield return new WaitForSecondsRealtime(duration + 0.1f);
                Assert.AreEqual(0.7f, hud.alpha);
                Assert.IsFalse(hud.interactable);
                Assert.IsTrue(hud.blocksRaycasts);
            }
            type.GetMethod("ShowLoss").Invoke(view, new object[] { null });
            view.gameObject.SetActive(false);
            Assert.AreEqual(0.7f, hud.alpha);
            Assert.IsFalse(hud.interactable);
            Assert.IsTrue(hud.blocksRaycasts);
        }

        [UnityTest]
        public IEnumerator ResultsOpeningAfterLossCannotBeUndoneByObsoleteFeedbackFade()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Type type = Type.GetType("Vertigo.WheelOfFortune.Game.GameCompositionRoot, Assembly-CSharp", true);
            Component controller = null;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) controller = component;
            }
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            object presenter = type.GetField("_presentation", FLAGS).GetValue(controller);
            Type presenterType = presenter.GetType();
            presenterType.GetMethod("ShowLoss").Invoke(presenter, new object[] { 1 });
            presenterType.GetMethod("ShowResults").Invoke(presenter, null);
            object results = type.GetField("_results", FLAGS).GetValue(controller);
            var groups = (CanvasGroup[])results.GetType().GetField("_underlyingHud", FLAGS).GetValue(results);
            Assert.AreEqual(4, groups.Length);
            yield return new WaitForSecondsRealtime(0.5f);
            foreach (CanvasGroup group in groups)
            {
                Assert.AreEqual(0f, group.alpha, group.name);
                Assert.IsFalse(group.interactable, group.name);
                Assert.IsFalse(group.blocksRaycasts, group.name);
            }
        }

        [UnityTest]
        public IEnumerator DisableCancelsLossFadeWithoutResurrectingFeedback()
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
            var group = (CanvasGroup)type.GetField("_group", FLAGS).GetValue(view);
            for (int i = 0; i < 3; i++)
            {
                type.GetMethod("ShowLoss").Invoke(view, new object[] { null });
                view.gameObject.SetActive(false);
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
