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
        public void RowsAggregateUnknownContentAndReuseTheirBuffer()
        {
            Type type = Type.GetType("Vertigo.WheelOfFortune.Game.RewardCatalogPresenter, Assembly-CSharp", true);
            object presenter = Activator.CreateInstance(type);
            var wallet = new RewardWallet();
            wallet.Add(new RewardGrant("unconfigured-a", 10));
            wallet.Add(new RewardGrant("unconfigured-b", 20));
            wallet.Add(new RewardGrant("unconfigured-a", 5));
            var build = type.GetMethod("BuildRows");
            var rows = (IList)build.Invoke(presenter, new object[] { wallet });
            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual("unconfigured-a", rows[0].GetType().GetField("Id").GetValue(rows[0]));
            Assert.AreEqual(15L, rows[0].GetType().GetField("Amount").GetValue(rows[0]));
            wallet.Clear();
            Assert.AreSame(rows, build.Invoke(presenter, new object[] { wallet }));
            Assert.AreEqual(0, rows.Count);
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
