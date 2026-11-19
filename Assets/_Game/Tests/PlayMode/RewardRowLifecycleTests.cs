using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class RewardRowLifecycleTests
    {
        [UnityTest]
        public IEnumerator DisableSettlesPartiallyAnimatedRowsWithoutGrowingPool()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Type type = Type.GetType("Vertigo.WheelOfFortune.UI.RewardsListView, Assembly-CSharp", true);
            Type rowType = Type.GetType("Vertigo.WheelOfFortune.UI.RewardRowData, Assembly-CSharp", true);
            Component view = null;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) view = component;
            }
            Assert.IsTrue(view);
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            var content = (RectTransform)type.GetField("content", FLAGS).GetValue(view);
            int initialChildren = content.childCount;
            Array data = Array.CreateInstance(rowType, 1);
            data.SetValue(Activator.CreateInstance(rowType, new object[] { "test-row", null, "Test", 123L }), 0);
            type.GetMethod("SetRows").Invoke(view, new object[] { data });
            type.GetMethod("PunchRow").Invoke(view, new object[] { "test-row" });
            view.gameObject.SetActive(false);
            var active = (IDictionary)type.GetField("_active", FLAGS).GetValue(view);
            object widgets = active["test-row"];
            Type widgetsType = widgets.GetType();
            var rect = (RectTransform)widgetsType.GetField("Rect").GetValue(widgets);
            var group = (CanvasGroup)widgetsType.GetField("Group").GetValue(widgets);
            var icon = (Component)widgetsType.GetField("Icon").GetValue(widgets);
            Assert.AreEqual(Vector3.one, rect.localScale);
            Assert.AreEqual(Vector3.one, icon.transform.localScale);
            Assert.AreEqual(1f, group.alpha);
            view.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(Vector3.one, rect.localScale);
            Assert.AreEqual(initialChildren, content.childCount);
        }
    }
}
