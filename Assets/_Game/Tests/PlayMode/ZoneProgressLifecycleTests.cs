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
            object zones = Activator.CreateInstance(Type.GetType("Vertigo.WheelOfFortune.Data.ZoneCatalog, Assembly-CSharp", true),
                type.GetField("_previewConfiguration", FLAGS).GetValue(view));
            object profile = type.GetField("_animation", FLAGS).GetValue(view);
            float duration = (float)profile.GetType().GetProperty("SlideDuration").GetValue(profile);
            MethodInfo transition = type.GetMethod("PlayTransition");
            type.GetMethod("Set").Invoke(view, new object[] { 1, zones });
            transition.Invoke(view, new object[] { 2, zones });
            transition.Invoke(view, new object[] { 3, zones });
            yield return new WaitForSecondsRealtime(duration * 0.5f);
            Assert.AreEqual(1, type.GetField("_displayedZone", FLAGS).GetValue(view));
            yield return new WaitForSecondsRealtime(duration);
            Assert.AreEqual(3, type.GetField("_displayedZone", FLAGS).GetValue(view));
            transition.Invoke(view, new object[] { 4, zones });
            view.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(duration + 0.1f);
            Assert.IsNull(type.GetField("_transition", FLAGS).GetValue(view));
            Assert.AreEqual(3, type.GetField("_displayedZone", FLAGS).GetValue(view));
            view.gameObject.SetActive(true);
            type.GetMethod("Set").Invoke(view, new object[] { 1, zones });
        }
    }
}
