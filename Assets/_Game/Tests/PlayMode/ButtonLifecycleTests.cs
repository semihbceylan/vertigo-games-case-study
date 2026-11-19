using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ButtonLifecycleTests
    {
        private int _clicks;

        [UnityTest]
        public IEnumerator DisabledWrapperDoesNotForwardClicksAndReenableDoesNotDuplicateListeners()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            Type type = Type.GetType("Vertigo.WheelOfFortune.UI.UIButton, Assembly-CSharp", true);
            Component view = null;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene() && component.gameObject.activeInHierarchy)
                {
                    view = component;
                    break;
                }
            }
            Assert.IsTrue(view);
            var button = (UnityEngine.UI.Button)type.GetProperty("Button").GetValue(view);
            // Remove gameplay subscribers from the equation by using a fresh wrapper with authored target.
            var owner = new GameObject("button-lifecycle", typeof(RectTransform), typeof(UnityEngine.UI.Button));
            owner.SetActive(false);
            var wrapper = (Behaviour)owner.AddComponent(type);
            button = owner.GetComponent<UnityEngine.UI.Button>();
            type.GetField("button", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(wrapper, button);
            type.GetEvent("Clicked").AddEventHandler(wrapper, (UnityAction)OnClick);
            _clicks = 0;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    owner.SetActive(true);
                    button.onClick.Invoke();
                    Assert.AreEqual(i + 1, _clicks);
                    wrapper.enabled = false;
                    button.onClick.Invoke();
                    Assert.AreEqual(i + 1, _clicks);
                    owner.SetActive(false);
                    wrapper.enabled = true;
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(owner);
            }
        }

        private void OnClick()
        {
            _clicks++;
        }
    }
}
