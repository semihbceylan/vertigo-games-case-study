using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class CurrencyHudLifecycleTests
    {
        private const BindingFlags FIELDS = BindingFlags.Instance | BindingFlags.NonPublic;

        private static string Text(object label)
        {
            return (string)label.GetType().GetProperty("text").GetValue(label);
        }

        private static Component Find(string typeName)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp", true);
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) return component;
            }
            Assert.Fail("Missing scene component " + typeName);
            return null;
        }

        [UnityTest]
        public IEnumerator TopUpHidesHudBlocksCommandsAndNeverCreditsCurrency()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Component view = Find("Vertigo.WheelOfFortune.UI.CurrencyHudView");
            Component controller = Find("Vertigo.WheelOfFortune.Game.GameCompositionRoot");
            Type type = view.GetType();
            object session = controller.GetType().GetField("_session", FIELDS).GetValue(controller);
            var balances = (CanvasGroup)type.GetField("_balancesGroup", FIELDS).GetValue(view);
            Assert.AreEqual(0f, balances.alpha);
            session.GetType().GetMethod("BeginSpin").Invoke(session, null);
            object wheel = session.GetType().GetProperty("CurrentWheel").GetValue(session);
            int slices = (int)wheel.GetType().GetProperty("SliceCount").GetValue(wheel);
            for (int i = 0; i < slices; i++)
            {
                object slice = wheel.GetType().GetProperty("Item").GetValue(wheel, new object[] { i });
                if (!(bool)slice.GetType().GetField("IsBomb").GetValue(slice)) continue;
                session.GetType().GetMethod("ResolveSpin").Invoke(session, new object[] { i });
                break;
            }
            Assert.AreEqual(1f, balances.alpha);
            object cash = type.GetField("_cashAmount", FIELDS).GetValue(view);
            object gold = type.GetField("_goldAmount", FIELDS).GetValue(view);
            string originalCash = Text(cash);
            string originalGold = Text(gold);
            var groups = (CanvasGroup[])type.GetField("_underlyingHud", FIELDS).GetValue(view);
            var panel = (CanvasGroup)type.GetField("_topUpPanel", FIELDS).GetValue(view);
            for (int cycle = 0; cycle < 2; cycle++)
            {
                type.GetMethod("OpenTopUp").Invoke(view, null);
                Assert.IsTrue(panel.blocksRaycasts);
                foreach (CanvasGroup hud in groups)
                {
                    Assert.AreEqual(0, hud.alpha);
                    Assert.IsFalse(hud.interactable);
                    Assert.IsFalse(hud.blocksRaycasts);
                }
                var spinButton = (Component)controller.GetType().GetField("_spinButton", FIELDS).GetValue(controller);
                spinButton.GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue((bool)session.GetType().GetProperty("CanRestart").GetValue(session));
                foreach (Button button in panel.GetComponentsInChildren<Button>(true))
                {
                    if (button.name.Contains("close")) continue;
                    Assert.IsFalse(button.interactable);
                    Assert.AreEqual(0, button.onClick.GetPersistentEventCount());
                    button.onClick.Invoke();
                }
                Assert.AreEqual(originalCash, Text(cash));
                Assert.AreEqual(originalGold, Text(gold));
                type.GetMethod("CloseTopUp").Invoke(view, null);
                Assert.IsFalse(panel.blocksRaycasts);
                Assert.AreEqual(0, groups[0].alpha);
                Assert.AreEqual(1f, balances.alpha);
                view.gameObject.SetActive(false);
                view.gameObject.SetActive(true);
            }
            session.GetType().GetMethod("Restart").Invoke(session, null);
            Assert.AreEqual(0f, balances.alpha);
        }

        [UnityTest]
        public IEnumerator DeathScreenPreviewsRecoveryWithScrollAndStyledButtons()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Component controller = Find("Vertigo.WheelOfFortune.Game.GameCompositionRoot");
            object session = controller.GetType().GetField("_session", FIELDS).GetValue(controller);
            object wallet = session.GetType().GetProperty("Wallet").GetValue(session);
            MethodInfo add = wallet.GetType().GetMethod("Add");
            Type grantType = add.GetParameters()[0].ParameterType;
            // Fill the preview with real catalogue rewards so the list must scroll.
            Type rewardType = Type.GetType("Vertigo.WheelOfFortune.Data.RewardDefinition, Assembly-CSharp", true);
            int added = 0;
            foreach (UnityEngine.Object reward in Resources.FindObjectsOfTypeAll(rewardType))
            {
                string id = (string)rewardType.GetProperty("RewardId").GetValue(reward);
                add.Invoke(wallet, new[] { Activator.CreateInstance(grantType, id, 10L) });
                added++;
            }
            Assert.Greater(added, 10);
            session.GetType().GetMethod("BeginSpin").Invoke(session, null);
            object wheel = session.GetType().GetProperty("CurrentWheel").GetValue(session);
            int count = (int)wheel.GetType().GetProperty("SliceCount").GetValue(wheel);
            for (int i = 0; i < count; i++)
            {
                object slice = wheel.GetType().GetProperty("Item").GetValue(wheel, new object[] { i });
                if (!(bool)slice.GetType().GetField("IsBomb").GetValue(slice)) continue;
                session.GetType().GetMethod("ResolveSpin").Invoke(session, new object[] { i });
                break;
            }
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.IsTrue((bool)wallet.GetType().GetProperty("IsEmpty").GetValue(wallet));
            Component feedback = Find("Vertigo.WheelOfFortune.UI.FeedbackView");
            var list = (Component)feedback.GetType().GetField("_recoverableRewardsList", FIELDS).GetValue(feedback);
            var scroll = list.GetComponent<ScrollRect>();
            Assert.IsTrue(scroll.vertical);
            Assert.IsTrue(scroll.IsActive());
            Assert.Greater(scroll.content.rect.height, ((RectTransform)scroll.transform).rect.height);
            foreach (Button button in list.transform.parent.GetComponentsInChildren<Button>(true))
            {
                Assert.IsFalse(button.gameObject.activeInHierarchy, "Exit must not appear in recovery pane.");
            }
            scroll.verticalNormalizedPosition = 0f;
            yield return null;
            Assert.Less(scroll.verticalNormalizedPosition, 0.01f);
            var revive = (Component)feedback.GetType().GetField("_reviveButton", FIELDS).GetValue(feedback);
            object price = feedback.GetType().GetField("_reviveLabel", FIELDS).GetValue(feedback);
            var restartButton = (Component)feedback.GetType().GetField("_restartButton", FIELDS).GetValue(feedback);
            object restart = restartButton.GetComponentInChildren(price.GetType(), true);
            Assert.AreEqual(price.GetType().GetProperty("fontSize").GetValue(price),
                restart.GetType().GetProperty("fontSize").GetValue(restart));
            StringAssert.Contains("orange", revive.GetComponent<Image>().sprite.name);
        }

        [UnityTest]
        public IEnumerator LossAlwaysShowsRevivePriceAndInsufficientFundsMessage()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Component view = Find("Vertigo.WheelOfFortune.UI.FeedbackView");
            Type type = view.GetType();
            type.GetMethod("ShowLoss").Invoke(view, new object[] { null });
            type.GetMethod("SetReviveOffer").Invoke(view, new object[] { 825L, 4 });
            var button = (Component)type.GetField("_reviveButton", FIELDS).GetValue(view);
            object label = type.GetField("_reviveLabel", FIELDS).GetValue(view);
            Assert.IsTrue(button.gameObject.activeSelf);
            Assert.IsTrue(button.GetComponent<Button>().interactable);
            Assert.AreEqual("825", Text(label));
            type.GetMethod("ShowInsufficientFunds").Invoke(view, null);
            object status = type.GetField("_reviveStatus", FIELDS).GetValue(view);
            Assert.AreEqual("Not enough gold — 4 revives remaining", Text(status));
            Assert.IsTrue(button.GetComponent<Button>().interactable);
            type.GetMethod("SetReviveOffer").Invoke(view, new object[] { 825L, 0 });
            Assert.IsTrue(button.gameObject.activeSelf);
            Assert.IsFalse(button.GetComponent<Button>().interactable);
            StringAssert.Contains("limit reached", Text(status));
        }
    }
}
