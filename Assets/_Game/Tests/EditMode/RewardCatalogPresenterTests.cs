using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class RewardCatalogPresenterTests
    {
        [Test]
        public void SessionPresentersUseImplementedViewAndSpinContracts()
        {
            Type hud = Type.GetType("Vertigo.WheelOfFortune.Game.SessionHudPresenter, Assembly-CSharp", true);
            Type presentation = Type.GetType("Vertigo.WheelOfFortune.Game.SessionPresenter, Assembly-CSharp", true);
            Type screen = Type.GetType("Vertigo.WheelOfFortune.UI.GameScreenView, Assembly-CSharp", true);
            Type button = Type.GetType("Vertigo.WheelOfFortune.UI.SpinButton, Assembly-CSharp", true);
            Type spinner = Type.GetType("Vertigo.WheelOfFortune.Wheel.WheelSpinner, Assembly-CSharp", true);
            Type hudContract = hud.GetConstructors()[0].GetParameters()[2].ParameterType;
            Type buttonContract = hud.GetConstructors()[0].GetParameters()[3].ParameterType;
            Type spinContract = presentation.GetConstructors()[0].GetParameters()[2].ParameterType;
            Type screenContract = presentation.GetConstructors()[0].GetParameters()[3].ParameterType;
            Assert.IsTrue(hudContract.IsInterface);
            Assert.IsTrue(buttonContract.IsInterface);
            Assert.IsTrue(spinContract.IsInterface);
            Assert.IsTrue(screenContract.IsInterface);
            Assert.IsTrue(hudContract.IsAssignableFrom(screen));
            Assert.IsTrue(screenContract.IsAssignableFrom(screen));
            Assert.IsTrue(buttonContract.IsAssignableFrom(button));
            Assert.IsTrue(spinContract.IsAssignableFrom(spinner));
        }

        [Test]
        public void RowsAggregateConfiguredContentAndReuseTheirBuffer()
        {
            Type type = Type.GetType("Vertigo.WheelOfFortune.Game.RewardCatalogPresenter, Assembly-CSharp", true);
            object presenter = Activator.CreateInstance(type);
            type.GetMethod("Configure").Invoke(presenter, new object[] { AssetDatabase.LoadMainAssetAtPath("Assets/_Game/Data/Zones_Default.asset") });
            var wallet = new RewardWallet();
            wallet.Add(new RewardGrant("cash", 10));
            wallet.Add(new RewardGrant("gold", 20));
            wallet.Add(new RewardGrant("cash", 5));
            var build = type.GetMethod("BuildRows");
            var rows = (IList)build.Invoke(presenter, new object[] { wallet });
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(15L, rows[IndexOf(rows, "cash")].GetType().GetField("Amount").GetValue(rows[IndexOf(rows, "cash")]));
            wallet.Clear();
            Assert.AreSame(rows, build.Invoke(presenter, new object[] { wallet }));
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void UnknownRewardContentIsRejectedRatherThanDisplayedRaw()
        {
            Type type = Type.GetType("Vertigo.WheelOfFortune.Game.RewardCatalogPresenter, Assembly-CSharp", true);
            object presenter = Activator.CreateInstance(type);
            type.GetMethod("Configure").Invoke(presenter, new object[] { AssetDatabase.LoadMainAssetAtPath("Assets/_Game/Data/Zones_Default.asset") });
            var wallet = new RewardWallet();
            wallet.Add(new RewardGrant("unconfigured", 10));
            var exception = Assert.Throws<System.Reflection.TargetInvocationException>(
                () => type.GetMethod("BuildRows").Invoke(presenter, new object[] { wallet }));
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
        }

        private static int IndexOf(IList rows, string id)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if ((string)rows[i].GetType().GetField("Id").GetValue(rows[i]) == id)
                {
                    return i;
                }
            }
            Assert.Fail("Missing row " + id);
            return -1;
        }

        [Test]
        public void ConfiguredCatalogResolvesAuthoredRewardContent()
        {
            Type type = Type.GetType("Vertigo.WheelOfFortune.Game.RewardCatalogPresenter, Assembly-CSharp", true);
            object presenter = Activator.CreateInstance(type);
            var configuration = AssetDatabase.LoadMainAssetAtPath("Assets/_Game/Data/Zones_Default.asset");
            Assert.IsTrue(configuration);
            type.GetMethod("Configure").Invoke(presenter, new object[] { configuration });
            string[] assets = AssetDatabase.FindAssets("t:RewardDefinition", new[] { "Assets/_Game/Data" });
            int resolved = 0;
            foreach (string guid in assets)
            {
                var reward = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                string id = (string)reward.GetType().GetProperty("RewardId").GetValue(reward);
                if (type.GetMethod("Resolve").Invoke(presenter, new object[] { id }) != null) resolved++;
            }
            Assert.Greater(resolved, 0);
        }
    }
}
