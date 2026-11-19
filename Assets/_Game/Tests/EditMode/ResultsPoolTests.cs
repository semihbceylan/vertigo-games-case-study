using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ResultsPoolTests
    {
        private const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void PoolIsPrewarmedAndRejectsOverflowWithoutCreatingTiles()
        {
            Type type = Type.GetType("Vertigo.WheelOfFortune.UI.ResultsView, Assembly-CSharp", true);
            var owner = new GameObject("results-pool-test", typeof(RectTransform));
            owner.SetActive(false);
            try
            {
                Component view = owner.AddComponent(type);
                type.GetField("content", PRIVATE_INSTANCE).SetValue(view, (RectTransform)owner.transform);
                type.GetField("_poolCapacity", PRIVATE_INSTANCE).SetValue(view, 3);
                type.GetMethod("Awake", PRIVATE_INSTANCE).Invoke(view, null);
                var tiles = (IList)type.GetField("_tiles", PRIVATE_INSTANCE).GetValue(view);
                Assert.AreEqual(3, tiles.Count);
                Assert.AreEqual(3, owner.transform.childCount);
                for (int i = 0; i < owner.transform.childCount; i++)
                {
                    Assert.IsFalse(owner.transform.GetChild(i).gameObject.activeSelf);
                }
                MethodInfo ensure = type.GetMethod("EnsureTileCount", PRIVATE_INSTANCE);
                ensure.Invoke(view, new object[] { 3 });
                var exception = Assert.Throws<TargetInvocationException>(() => ensure.Invoke(view, new object[] { 4 }));
                Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
                Assert.AreEqual(3, owner.transform.childCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
