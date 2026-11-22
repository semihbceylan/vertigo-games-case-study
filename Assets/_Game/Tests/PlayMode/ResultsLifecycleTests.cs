using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ResultsLifecycleTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
        private Component _view;
        private Type _type;
        private CanvasGroup _hud;
        private CanvasGroup _group;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            _type = Type.GetType("Vertigo.WheelOfFortune.UI.ResultsView, Assembly-CSharp", true);
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(_type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) _view = component;
            }
            Assert.IsTrue(_view);
            _hud = ((CanvasGroup[])Field("_underlyingHud"))[0];
            _group = (CanvasGroup)Field("_group");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_view) _type.GetMethod("HideImmediate").Invoke(_view, null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RepeatedShowPreservesHudSnapshotUntilFadeCompletes()
        {
            _hud.alpha = 0.4f;
            _hud.interactable = false;
            _hud.blocksRaycasts = true;
            Show();
            Show();
            Assert.AreEqual(0f, _hud.alpha);
            Assert.IsFalse(_hud.blocksRaycasts);
            _type.GetMethod("Hide").Invoke(_view, null);
            Assert.IsTrue(_group.blocksRaycasts);
            Assert.AreEqual(0f, _hud.alpha);
            yield return new WaitForSecondsRealtime(FadeDuration() + 0.1f);
            Assert.IsFalse(_group.blocksRaycasts);
            Assert.AreEqual(0.4f, _hud.alpha);
            Assert.IsFalse(_hud.interactable);
            Assert.IsTrue(_hud.blocksRaycasts);
        }

        [UnityTest]
        public IEnumerator DisableCancelsFadeAndRestoresHudAcrossRepeatedCycles()
        {
            for (int i = 0; i < 3; i++)
            {
                Show();
                _view.gameObject.SetActive(false);
                Assert.IsNull(Field("_fade"));
                Assert.IsFalse(_group.blocksRaycasts);
                Assert.AreEqual(1f, _hud.alpha);
                _view.gameObject.SetActive(true);
            }
            yield return new WaitForSecondsRealtime(FadeDuration() + 0.1f);
            Assert.AreEqual(1f, _hud.alpha);
        }

        private object Field(string name)
        {
            return _type.GetField(name, PRIVATE_INSTANCE).GetValue(_view);
        }

        private void Show()
        {
            Type rowType = Type.GetType("Vertigo.WheelOfFortune.UI.RewardRowData, Assembly-CSharp", true);
            _type.GetMethod("Show").Invoke(_view, new object[] { Array.CreateInstance(rowType, 0) });
        }

        private float FadeDuration()
        {
            object animation = Field("_animation");
            return (float)animation.GetType().GetProperty("FadeDuration").GetValue(animation);
        }
    }
}
