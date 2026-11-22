using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    /// <summary>Completion callbacks of the scene's reward presentation are single-use and cancellable.</summary>
    public sealed class RewardPresentationLifecycleTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
        private Component _presenter;
        private Type _type;
        private int _completions;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            _type = Type.GetType("Vertigo.WheelOfFortune.Game.RewardPresentationController, Assembly-CSharp", true);
            _presenter = null;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(_type))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene())
                {
                    _presenter = component;
                }
            }
            Assert.IsTrue(_presenter);
            _completions = 0;
            _type.GetField("_rewardId", PRIVATE_INSTANCE).SetValue(_presenter, string.Empty);
            _type.GetField("_onComplete", PRIVATE_INSTANCE).SetValue(_presenter, (Action)CountCompletion);
        }

        [Test]
        public void CancelDiscardsCompletionWithoutAdvancingFlow()
        {
            _type.GetMethod("Cancel").Invoke(_presenter, null);
            InvokePrivate("Complete");
            Assert.AreEqual(0, _completions);
        }

        [Test]
        public void CompletionIsSingleUse()
        {
            InvokePrivate("Complete");
            InvokePrivate("Complete");
            Assert.AreEqual(1, _completions);
        }

        [Test]
        public void DisableDiscardsPendingCompletion()
        {
            ((MonoBehaviour)_presenter).enabled = false;
            InvokePrivate("Complete");
            Assert.AreEqual(0, _completions);
            ((MonoBehaviour)_presenter).enabled = true;
        }

        private void CountCompletion()
        {
            _completions++;
        }

        private void InvokePrivate(string name)
        {
            _type.GetMethod(name, PRIVATE_INSTANCE).Invoke(_presenter, null);
        }
    }
}
