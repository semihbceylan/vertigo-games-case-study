using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class AdminPanelLifecycleTests
    {
        private const BindingFlags FIELDS = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float SLOW_TAP_MARGIN = 0.2f;

        private static Component Find(string typeName)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp", true);
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) return component;
            }
            return null;
        }

        private static void Invoke(Component target, string method)
        {
            target.GetType().GetMethod(method, FIELDS).Invoke(target, null);
        }

        private static CanvasGroup Window(Component admin)
        {
            return (CanvasGroup)admin.GetType().GetField("_window", FIELDS).GetValue(admin);
        }

        [UnityTest]
        public IEnumerator QuickTapsOpenThePanelButSlowTapsDoNot()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Component admin = Find("Vertigo.WheelOfFortune.Game.AdminPanel");
            Assert.IsTrue(admin);
            int required = (int)admin.GetType().GetField("_requiredTaps", FIELDS).GetValue(admin);
            float window = (float)admin.GetType().GetField("_tapWindow", FIELDS).GetValue(admin);
            Assert.AreEqual(0f, Window(admin).alpha);

            for (int i = 0; i < required - 1; i++) Invoke(admin, "OnTriggerTapped");
            yield return new WaitForSecondsRealtime(window + SLOW_TAP_MARGIN);
            Invoke(admin, "OnTriggerTapped");
            Assert.AreEqual(0f, Window(admin).alpha, "Taps spread beyond the window must not open the panel.");

            for (int i = 0; i < required; i++) Invoke(admin, "OnTriggerTapped");
            Assert.AreEqual(1f, Window(admin).alpha);
            Assert.IsTrue(Window(admin).blocksRaycasts);
            Invoke(admin, "Close");
            Assert.AreEqual(0f, Window(admin).alpha);
            Assert.IsFalse(Window(admin).blocksRaycasts);
        }

        [UnityTest]
        public IEnumerator ForceBombDecidesTheNextSpinAndSpeedResetsOnDisable()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Component admin = Find("Vertigo.WheelOfFortune.Game.AdminPanel");
            Component composition = Find("Vertigo.WheelOfFortune.Game.GameCompositionRoot");
            object session = composition.GetType().GetField("_session", FIELDS).GetValue(composition);
            Type sessionType = session.GetType();

            Invoke(admin, "ForceBomb");
            sessionType.GetMethod("BeginSpin").Invoke(session, null);
            int pending = (int)sessionType.GetProperty("PendingSliceIndex").GetValue(session);
            object wheel = sessionType.GetProperty("CurrentWheel").GetValue(session);
            object slice = wheel.GetType().GetProperty("Item").GetValue(wheel, new object[] { pending });
            Assert.IsTrue((bool)slice.GetType().GetField("IsBomb").GetValue(slice), "Force bomb must land the next spin on the bomb.");

            Type dotween = Type.GetType("DG.Tweening.DOTween, DOTween", true);
            FieldInfo timeScale = dotween.GetField("timeScale", BindingFlags.Static | BindingFlags.Public);
            float original = (float)timeScale.GetValue(null);
            Invoke(admin, "CycleSpeed");
            Assert.AreNotEqual(original, (float)timeScale.GetValue(null));
            admin.gameObject.SetActive(false);
            Assert.AreEqual(original, (float)timeScale.GetValue(null), "Disabling the panel must restore animation speed.");
            admin.gameObject.SetActive(true);
        }
    }
}
