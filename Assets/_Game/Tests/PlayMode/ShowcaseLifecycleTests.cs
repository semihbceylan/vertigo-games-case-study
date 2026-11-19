using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ShowcaseLifecycleTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
        private Component _view;
        private Type _type;
        private CanvasGroup _hud;
        private CanvasGroup _group;
        private int _dismissals;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            _type = Type.GetType("Vertigo.WheelOfFortune.UI.JackpotShowcaseView, Assembly-CSharp", true);
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(_type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) _view = component;
            }
            Assert.IsTrue(_view);
            _hud = ((CanvasGroup[])Field("_underlyingHud"))[0];
            _group = (CanvasGroup)Field("_group");
            _dismissals = 0;
            _type.GetEvent("Dismissed").AddEventHandler(_view, (Action)OnDismissed);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_view)
            {
                Call("HideImmediate");
                _type.GetEvent("Dismissed").RemoveEventHandler(_view, (Action)OnDismissed);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator DismissalWaitsForFadeAndRestoresPriorHudState()
        {
            _hud.alpha = 0.6f;
            _hud.interactable = false;
            _hud.blocksRaycasts = true;
            Show();
            Assert.AreEqual(0f, _hud.alpha);
            Assert.IsFalse(_hud.interactable);
            Assert.IsFalse(_hud.blocksRaycasts);
            yield return new WaitForSecondsRealtime(FadeDuration() + 0.1f);
            Call("Hide");
            Call("Hide");
            Assert.AreEqual(0, _dismissals);
            Assert.IsTrue(_group.blocksRaycasts);
            yield return new WaitForSecondsRealtime(FadeDuration() + 0.1f);
            Assert.AreEqual(1, _dismissals);
            Assert.IsFalse(_group.blocksRaycasts);
            Assert.AreEqual(0.6f, _hud.alpha);
            Assert.IsFalse(_hud.interactable);
            Assert.IsTrue(_hud.blocksRaycasts);
            Assert.IsNull(Field("_spin"));
        }

        [UnityTest]
        public IEnumerator CancellationAndDisableNeverCompleteObsoleteDismissals()
        {
            for (int i = 0; i < 3; i++)
            {
                Show();
                Call("Hide");
                _view.gameObject.SetActive(false);
                _view.gameObject.SetActive(true);
            }
            yield return new WaitForSecondsRealtime(FadeDuration() + 0.1f);
            Assert.AreEqual(0, _dismissals);
            Assert.IsFalse(_group.blocksRaycasts);
            Assert.IsNull(Field("_fade"));
            Assert.IsNull(Field("_pop"));
            Assert.IsNull(Field("_spin"));
            Show();
            Call("Hide");
            yield return new WaitForSecondsRealtime(FadeDuration() + 0.1f);
            Assert.AreEqual(1, _dismissals);
        }

        private object Field(string name)
        {
            return _type.GetField(name, PRIVATE_INSTANCE).GetValue(_view);
        }

        private void Call(string name)
        {
            _type.GetMethod(name).Invoke(_view, null);
        }

        private void Show()
        {
            _type.GetMethod("Show").Invoke(_view, new object[] { null, "Lifecycle test", 1000L });
        }

        private float FadeDuration()
        {
            object animation = Field("_animation");
            return (float)animation.GetType().GetProperty("FadeDuration").GetValue(animation);
        }

        private void OnDismissed()
        {
            _dismissals++;
        }
    }
}
