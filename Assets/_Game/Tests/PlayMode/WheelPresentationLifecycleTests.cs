using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class WheelPresentationLifecycleTests
    {
        [UnityTest]
        public IEnumerator ZoneChangesReuseSlotsAndMatchBombQuantityVisibility()
        {
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
            Type controllerType = Type.GetType("Vertigo.WheelOfFortune.Game.GameController, Assembly-CSharp", true);
            Component controller = null;
            foreach (UnityEngine.Object candidate in Resources.FindObjectsOfTypeAll(controllerType))
            {
                var component = (Component)candidate;
                if (component.gameObject.scene == SceneManager.GetActiveScene()) controller = component;
            }
            Assert.IsTrue(controller);
            object layout = controllerType.GetField("wheelLayout", FLAGS).GetValue(controller);
            Type layoutType = layout.GetType();
            object firstIcon = layoutType.GetMethod("GetIcon").Invoke(layout, new object[] { 0 });
            foreach (int zone in new[] { 1, 5, 30, 31, 5 })
            {
                controllerType.GetMethod("DebugJumpTo", FLAGS).Invoke(controller, new object[] { zone });
                object session = controllerType.GetField("_session", FLAGS).GetValue(controller);
                object wheel = session.GetType().GetProperty("CurrentWheel").GetValue(session);
                Type wheelType = wheel.GetType();
                int count = (int)wheelType.GetProperty("SliceCount").GetValue(wheel);
                Assert.AreEqual(count, layoutType.GetProperty("SliceCount").GetValue(layout));
                Assert.AreSame(firstIcon, layoutType.GetMethod("GetIcon").Invoke(layout, new object[] { 0 }));
                int hiddenAmounts = 0;
                for (int i = 0; i < count; i++)
                {
                    object slice = wheelType.GetProperty("Item").GetValue(wheel, new object[] { i });
                    bool isBomb = (bool)slice.GetType().GetField("IsBomb").GetValue(slice);
                    var amount = (Component)layoutType.GetMethod("GetAmountText").Invoke(layout, new object[] { i });
                    Assert.AreEqual(!isBomb, amount.gameObject.activeSelf);
                    if (!amount.gameObject.activeSelf) hiddenAmounts++;
                }
                Assert.AreEqual(wheelType.GetProperty("BombCount").GetValue(wheel), hiddenAmounts);
            }
        }
    }
}
