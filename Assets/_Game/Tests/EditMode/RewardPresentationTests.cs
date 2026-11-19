using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class RewardPresentationTests
    {
        private GameObject _owner;
        private Component _presenter;
        private Type _type;
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            _type = Type.GetType("Vertigo.WheelOfFortune.Game.RewardPresentationController, Assembly-CSharp", true);
            _owner = new GameObject("reward-presentation-test");
            _owner.SetActive(false);
            _presenter = _owner.AddComponent(_type);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_owner);
        }

        [Test]
        public void CancelDiscardsCompletionWithoutAdvancingFlow()
        {
            int completions = 0;
            SetCompletion(() => completions++);
            _type.GetMethod("Cancel").Invoke(_presenter, null);
            InvokePrivate("Complete");
            Assert.AreEqual(0, completions);
        }

        [Test]
        public void CompletionIsSingleUse()
        {
            int completions = 0;
            SetCompletion(() => completions++);
            InvokePrivate("Complete");
            InvokePrivate("Complete");
            Assert.AreEqual(1, completions);
        }

        [Test]
        public void DisableDiscardsPendingCompletion()
        {
            int completions = 0;
            SetCompletion(() => completions++);
            InvokePrivate("OnDisable");
            InvokePrivate("Complete");
            Assert.AreEqual(0, completions);
        }

        private void SetCompletion(Action callback)
        {
            _type.GetField("_onComplete", PRIVATE_INSTANCE).SetValue(_presenter, callback);
        }

        private void InvokePrivate(string name)
        {
            _type.GetMethod(name, PRIVATE_INSTANCE).Invoke(_presenter, null);
        }
    }
}
