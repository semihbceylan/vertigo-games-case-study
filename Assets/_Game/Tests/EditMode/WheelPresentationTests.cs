using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class WheelPresentationTests
    {
        [Test]
        public void WheelSlicePrefabHasTypedIconAndAmountReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Wheel/ui_wheel_slice.prefab");
            Type type = Type.GetType("Vertigo.WheelOfFortune.Wheel.WheelSliceView, Assembly-CSharp", true);
            Component view = prefab.GetComponent(type);
            Assert.IsTrue(view);
            Assert.IsTrue((UnityEngine.Object)type.GetProperty("Icon").GetValue(view));
            Assert.IsTrue((UnityEngine.Object)type.GetProperty("Amount").GetValue(view));
        }

        [Test]
        public void RepeatedWheelSizesReuseExistingSlotsAndHideUnusedSlots()
        {
            Type type = Type.GetType("Vertigo.WheelOfFortune.Wheel.WheelLayout, Assembly-CSharp", true);
            var owner = new GameObject("wheel-layout-test", typeof(RectTransform));
            try
            {
                Component layout = owner.AddComponent(type);
                var set = type.GetMethod("SetSliceCount");
                set.Invoke(layout, new object[] { 8 });
                Transform first = owner.transform.GetChild(0);
                set.Invoke(layout, new object[] { 4 });
                Assert.AreEqual(8, owner.transform.childCount);
                Assert.AreEqual(4, type.GetProperty("SliceCount").GetValue(layout));
                Assert.IsFalse(owner.transform.GetChild(7).gameObject.activeSelf);
                Assert.IsNull(type.GetMethod("GetIcon").Invoke(layout, new object[] { 7 }));
                set.Invoke(layout, new object[] { 8 });
                Assert.AreSame(first, owner.transform.GetChild(0));
                Assert.AreEqual(8, owner.transform.childCount);
                Assert.IsTrue(owner.transform.GetChild(7).gameObject.activeSelf);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
