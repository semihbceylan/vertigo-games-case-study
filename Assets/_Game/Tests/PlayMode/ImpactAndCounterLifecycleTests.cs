using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ImpactAndCounterLifecycleTests
    {
        private const BindingFlags FIELDS = BindingFlags.Instance | BindingFlags.NonPublic;
        /// <summary>Extra real time allowed after an authored duration for the tween to finish.</summary>
        private const float SETTLE_MARGIN = 0.15f;
        private const float POSITION_TOLERANCE = 0.01f;
        private const float MINIMUM_FLICK_DEGREES = 0.1f;

        private static Component Find(string typeName, string objectName = null)
        {
            Type type = Type.GetType(typeName + ", Assembly-CSharp", true);
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene != SceneManager.GetActiveScene()) continue;
                if (objectName == null || component.name == objectName) return component;
            }
            return null;
        }

        private static float Profile(Component owner, string profileField, string property)
        {
            object profile = owner.GetType().GetField(profileField, FIELDS).GetValue(owner);
            return (float)profile.GetType().GetProperty(property).GetValue(profile);
        }

        [UnityTest]
        public IEnumerator BombImpactHandsOffOnceAndCancelNeverHandsOff()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Component impact = Find("Vertigo.WheelOfFortune.UI.BombImpactView");
            Assert.IsTrue(impact);
            Type type = impact.GetType();
            var shake = (RectTransform)type.GetField("_shakeTarget", FIELDS).GetValue(impact);
            var flash = (Image)type.GetField("_flash", FIELDS).GetValue(impact);
            Vector2 home = shake.anchoredPosition;
            float panelDelay = Profile(impact, "_animation", "PanelDelay");
            float shakeDuration = Profile(impact, "_animation", "ShakeDuration");
            MethodInfo play = type.GetMethod("Play");
            int handOffs = 0;
            Action onPanelDue = () => handOffs++;

            play.Invoke(impact, new object[] { 0, onPanelDue });
            type.GetMethod("Cancel").Invoke(impact, null);
            yield return new WaitForSecondsRealtime(panelDelay + SETTLE_MARGIN);
            Assert.AreEqual(0, handOffs, "A cancelled impact must never open the loss panel.");
            Assert.Less(Vector2.Distance(home, shake.anchoredPosition), POSITION_TOLERANCE);
            Assert.IsFalse(flash.enabled);

            play.Invoke(impact, new object[] { 0, onPanelDue });
            yield return new WaitForSecondsRealtime(Mathf.Max(panelDelay, shakeDuration) + SETTLE_MARGIN);
            Assert.AreEqual(1, handOffs);
            Assert.Less(Vector2.Distance(home, shake.anchoredPosition), POSITION_TOLERANCE);
            type.GetMethod("Cancel").Invoke(impact, null);
        }

        [UnityTest]
        public IEnumerator IndicatorFlickReturnsToRestWhenDisabled()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Component flick = Find("Vertigo.WheelOfFortune.Wheel.WheelIndicatorFlick");
            Assert.IsTrue(flick);
            Type type = flick.GetType();
            var indicator = (RectTransform)type.GetField("_indicator", FIELDS).GetValue(flick);
            float rest = indicator.localEulerAngles.z;
            float flickDuration = Profile(flick, "_animation", "FlickDuration");
            MethodInfo tick = type.GetMethod("OnSliceTicked", FIELDS);

            tick.Invoke(flick, null);
            yield return new WaitForSecondsRealtime(flickDuration);
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(rest, indicator.localEulerAngles.z)), MINIMUM_FLICK_DEGREES);
            tick.Invoke(flick, null);
            ((Behaviour)flick).enabled = false;
            Assert.IsNull(type.GetField("_sequence", FIELDS).GetValue(flick));
            Assert.AreEqual(0f, Mathf.DeltaAngle(rest, indicator.localEulerAngles.z), POSITION_TOLERANCE);
            ((Behaviour)flick).enabled = true;
        }

        [UnityTest]
        public IEnumerator RowAmountRollsToItsTargetAndSettlesWhenDisabled()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Component list = Find("Vertigo.WheelOfFortune.UI.RewardsListView", "ui_list_rewards");
            Assert.IsTrue(list);
            Type type = list.GetType();
            Type rowType = Type.GetType("Vertigo.WheelOfFortune.UI.RewardRowData, Assembly-CSharp", true);
            MethodInfo setRows = type.GetMethod("SetRows");
            float duration = Profile(list, "_countAnimation", "Duration");

            setRows.Invoke(list, new object[] { Rows(rowType, 10L) });
            Assert.AreEqual("10", AmountText(list));
            setRows.Invoke(list, new object[] { Rows(rowType, 1000L) });
            Assert.AreNotEqual("1,000", AmountText(list), "An increased amount must roll, not jump.");
            yield return new WaitForSecondsRealtime(duration + SETTLE_MARGIN);
            Assert.AreEqual("1,000", AmountText(list));

            setRows.Invoke(list, new object[] { Rows(rowType, 5000L) });
            list.gameObject.SetActive(false);
            Assert.AreEqual("5,000", AmountText(list), "Disabling mid-roll must settle on the target amount.");
            list.gameObject.SetActive(true);
        }

        private static object Rows(Type rowType, long amount)
        {
            var rows = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(rowType));
            rows.Add(Activator.CreateInstance(rowType, "test_reward", null, "Test", amount));
            return rows;
        }

        private static string AmountText(Component list)
        {
            var active = (IDictionary)list.GetType().GetField("_active", FIELDS).GetValue(list);
            object row = active["test_reward"];
            object label = row.GetType().GetProperty("Amount").GetValue(row);
            return (string)label.GetType().GetProperty("text").GetValue(label);
        }
    }
}
