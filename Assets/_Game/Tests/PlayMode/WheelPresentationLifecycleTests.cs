using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class WheelPresentationLifecycleTests
    {
        private int _completions;

        [UnityTest]
        public IEnumerator OrdinaryRewardFliesWithoutShowingToast()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            Type controllerType = Type.GetType("Vertigo.WheelOfFortune.Game.GameCompositionRoot, Assembly-CSharp", true);
            Component controller = null;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(controllerType))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) controller = component;
            }
            Assert.IsTrue(controller);
            object session = controllerType.GetField("_session", FLAGS).GetValue(controller);
            Type sessionType = session.GetType();
            object wheel = sessionType.GetProperty("CurrentWheel").GetValue(session);
            int count = (int)wheel.GetType().GetProperty("SliceCount").GetValue(wheel);
            int rewardIndex = -1;
            for (int i = 0; i < count; i++)
            {
                object slice = wheel.GetType().GetProperty("Item").GetValue(wheel, new object[] { i });
                if ((bool)slice.GetType().GetField("IsBomb").GetValue(slice)) continue;
                rewardIndex = i;
                break;
            }
            Assert.GreaterOrEqual(rewardIndex, 0);
            sessionType.GetMethod("BeginSpin").Invoke(session, null);
            sessionType.GetMethod("ResolveSpin").Invoke(session, new object[] { rewardIndex });
            object feedback = controllerType.GetField("_lossPanel", FLAGS).GetValue(controller);
            var group = (CanvasGroup)feedback.GetType().GetField("_group", FLAGS).GetValue(feedback);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.AreEqual(0f, group.alpha, "Ordinary reward must not display a toast.");
            Assert.IsFalse(group.blocksRaycasts);
            object wallet = sessionType.GetProperty("Wallet").GetValue(session);
            Assert.AreEqual(1, wallet.GetType().GetProperty("Count").GetValue(wallet));
        }

        [UnityTest]
        public IEnumerator SessionPresenterCancelsTransitionSilentlyAndCanPlayAgain()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            Type controllerType = Type.GetType("Vertigo.WheelOfFortune.Game.GameCompositionRoot, Assembly-CSharp", true);
            Component controller = null;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(controllerType))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) controller = component;
            }
            Assert.IsTrue(controller);
            object presenter = controllerType.GetField("_presentation", FLAGS).GetValue(controller);
            Type presenterType = presenter.GetType();
            object transition = controllerType.GetField("_wheelTransitionController", FLAGS).GetValue(controller);
            object profile = transition.GetType().GetField("_animation", FLAGS).GetValue(transition);
            float duration = (float)profile.GetType().GetProperty("SlideDuration").GetValue(profile);
            duration += (float)profile.GetType().GetProperty("HoldDuration").GetValue(profile);
            _completions = 0;
            presenterType.GetMethod("PlayZoneTransition").Invoke(presenter, new object[] { (Action)OnComplete });
            presenterType.GetMethod("Cancel").Invoke(presenter, null);
            yield return new WaitForSecondsRealtime(duration + 0.1f);
            Assert.AreEqual(0, _completions);
            presenterType.GetMethod("PlayZoneTransition").Invoke(presenter, new object[] { (Action)OnComplete });
            yield return new WaitForSecondsRealtime(duration + 0.2f);
            Assert.AreEqual(1, _completions);
        }

        private void OnComplete()
        {
            _completions++;
        }

        [UnityTest]
        public IEnumerator ZoneChangesReuseSlotsAndMatchBombQuantityVisibility()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            Type controllerType = Type.GetType("Vertigo.WheelOfFortune.Game.GameCompositionRoot, Assembly-CSharp", true);
            Component controller = null;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(controllerType))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) controller = component;
            }
            Assert.IsTrue(controller);
            object wheelView = controllerType.GetField("_wheelView", FLAGS).GetValue(controller);
            object disc = wheelView.GetType().GetProperty("Disc").GetValue(wheelView);
            object layout = disc.GetType().GetProperty("Layout").GetValue(disc);
            Type layoutType = layout.GetType();
            object firstIcon = layoutType.GetMethod("GetIcon").Invoke(layout, new object[] { 0 });
            foreach (int zone in new[] { 1, 5, 30, 31, 5 })
            {
                object session = controllerType.GetField("_session", FLAGS).GetValue(controller);
                session.GetType().GetMethod("JumpToZone").Invoke(session, new object[] { zone });
                object wheel = session.GetType().GetProperty("CurrentWheel").GetValue(session);
                Type wheelType = wheel.GetType();
                int count = (int)wheelType.GetProperty("SliceCount").GetValue(wheel);
                Assert.AreEqual(count, layoutType.GetProperty("SliceCount").GetValue(layout));
                Assert.AreSame(firstIcon, layoutType.GetMethod("GetIcon").Invoke(layout, new object[] { 0 }));
                int hiddenAmounts = 0;
                for (int i = 0; i < count; i++)
                {
                    object slice = wheelType.GetProperty("Item").GetValue(wheel, new object[] { i });
                    bool isBomb = (bool)slice.GetType().GetField("IsBomb").GetValue(slice);
                    var amount = (Component)layoutType.GetMethod("GetAmountText").Invoke(layout, new object[] { i });
                    Assert.AreEqual(!isBomb, amount.gameObject.activeSelf);
                    if (!amount.gameObject.activeSelf) hiddenAmounts++;
                }
                Assert.AreEqual(wheelType.GetProperty("BombCount").GetValue(wheel), hiddenAmounts);
            }
        }
    }
}
