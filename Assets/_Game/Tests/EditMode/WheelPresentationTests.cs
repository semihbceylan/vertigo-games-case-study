using System;
using NUnit.Framework;
using UnityEditor;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class WheelPresentationTests
    {
        [Test]
        public void WheelSlicePrefabHasTypedIconAndAmountReferences()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Wheel/ui_panel_wheel_slice.prefab");
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
                LogAssert.Expect(LogType.Error, "WheelLayout requires a slot prefab.");
                Component layout = owner.AddComponent(type);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Wheel/ui_panel_wheel_slice.prefab");
                const BindingFlags PRIVATE_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;
                type.GetField("_slicesRoot", PRIVATE_INSTANCE).SetValue(layout, (RectTransform)owner.transform);
                type.GetField("_slotPrefab", PRIVATE_INSTANCE).SetValue(layout,
                    prefab.GetComponent(Type.GetType("Vertigo.WheelOfFortune.Wheel.WheelSliceView, Assembly-CSharp", true)));
                var set = type.GetMethod("SetSliceCount");
                set.Invoke(layout, new object[] { 8 });
                Transform first = owner.transform.GetChild(0);
                set.Invoke(layout, new object[] { 4 });
                Assert.AreEqual(8, owner.transform.childCount);
                Assert.AreEqual(4, type.GetProperty("SliceCount").GetValue(layout));
                Assert.IsFalse(owner.transform.GetChild(7).gameObject.activeSelf);
                var exception = Assert.Throws<TargetInvocationException>(() => type.GetMethod("GetIcon").Invoke(layout, new object[] { 7 }));
                Assert.IsInstanceOf<ArgumentOutOfRangeException>(exception.InnerException);
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
