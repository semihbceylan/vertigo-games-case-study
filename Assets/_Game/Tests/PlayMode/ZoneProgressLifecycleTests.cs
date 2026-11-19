using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ZoneProgressLifecycleTests
    {
        [UnityTest]
        public IEnumerator ReplacementAndDisablePreventObsoleteZoneCompletion()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            // Let the scene composition root finish Start before driving presentation directly.
            yield return null;
            Type type = Type.GetType("Vertigo.WheelOfFortune.UI.ZoneProgressBar, Assembly-CSharp", true);
            Component view = null;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) view = component;
            }
            Assert.IsTrue(view);
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            object configuration = type.GetField("_configuration", FLAGS).GetValue(view);
            MethodInfo transition = type.GetMethod("PlayTransition");
            Type easeType = transition.GetParameters()[3].ParameterType;
            object ease = Enum.Parse(easeType, "Linear");
            type.GetMethod("Set").Invoke(view, new object[] { 1, configuration });
            transition.Invoke(view, new object[] { 2, configuration, 0.05f, ease });
            transition.Invoke(view, new object[] { 3, configuration, 0.2f, ease });
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.AreEqual(1, type.GetField("_displayedZone", FLAGS).GetValue(view));
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.AreEqual(3, type.GetField("_displayedZone", FLAGS).GetValue(view));
            transition.Invoke(view, new object[] { 4, configuration, 0.1f, ease });
            view.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsNull(type.GetField("_transition", FLAGS).GetValue(view));
            Assert.AreEqual(3, type.GetField("_displayedZone", FLAGS).GetValue(view));
            view.gameObject.SetActive(true);
            type.GetMethod("Set").Invoke(view, new object[] { 1, configuration });
        }
    }
}
