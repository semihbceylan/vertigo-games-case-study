using System;
using NUnit.Framework;
using UnityEditor;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ZoneRuleTests
    {
        [Test]
        public void CustomIdentityUsesConfiguredRangeAndPriority()
        {
            var custom = new ZoneKind("festival", true, false);
            var rules = new ZoneRules(new ZoneRule(TestZones.Normal, 1, 1, 0, 0),
                new ZoneRule(custom, 3, 6, 12, 10));
            Assert.AreSame(TestZones.Normal, rules.Resolve(3));
            Assert.AreSame(custom, rules.Resolve(6));
            Assert.AreSame(custom, rules.Resolve(12));
            Assert.AreSame(TestZones.Normal, rules.Resolve(15));
            Assert.IsTrue(custom.IsBombFree);
            Assert.IsFalse(custom.IsLeaveAllowed);
        }

        [Test]
        public void MatchingEqualHighestPrioritiesAreRejected()
        {
            var rules = new ZoneRules(new ZoneRule(TestZones.Normal, 1, 1, 0, 0),
                new ZoneRule(TestZones.Safe, 5, 1, 0, 0));
            Assert.Throws<InvalidOperationException>(() => rules.Resolve(5));
        }

        [Test]
        public void MissingCoverageIsRejected()
        {
            var rules = new ZoneRules(new ZoneRule(TestZones.Safe, 5, 1, 0, 0));
            Assert.Throws<InvalidOperationException>(() => rules.Resolve(1));
        }

        [Test]
        public void DuplicateIdentityIsRejected()
        {
            Assert.Throws<ArgumentException>(() => new ZoneRules(
                new ZoneRule(TestZones.Safe, 5, 1, 0, 0),
                new ZoneRule(new ZoneKind("safe", false, false), 7, 1, 0, 1)));
        }

        [Test]
        public void NextZoneSkipsHigherPriorityMatchesAndStopsAtFinalZone()
        {
            var rules = new ZoneRules(
                new ZoneRule(TestZones.Normal, 1, 1, 0, 0),
                new ZoneRule(TestZones.Safe, 5, 1, 0, 1),
                new ZoneRule(TestZones.Super, 30, 1, 0, 2));
            Assert.AreEqual(5, rules.FindNextZone(1, TestZones.Safe.Id, 60));
            Assert.AreEqual(35, rules.FindNextZone(29, TestZones.Safe.Id, 60));
            Assert.AreEqual(30, rules.FindNextZone(29, TestZones.Super.Id, 60));
            Assert.AreEqual(ZoneRules.NO_ZONE, rules.FindNextZone(59, TestZones.Safe.Id, 60));
            Assert.AreEqual(ZoneRules.NO_ZONE, rules.FindNextZone(60, TestZones.Super.Id, 60));
        }

        [Test]
        public void FullyShadowedRuleHasNoNextZoneEvenWhenEndless()
        {
            var rules = new ZoneRules(
                new ZoneRule(TestZones.Normal, 1, 1, 0, 0),
                new ZoneRule(TestZones.Safe, 10, 1, 0, 1),
                new ZoneRule(TestZones.Super, 5, 1, 0, 2));
            Assert.AreEqual(ZoneRules.NO_ZONE, rules.FindNextZone(1, TestZones.Safe.Id, ZoneRules.ENDLESS_FINAL_ZONE));
            Assert.AreEqual(5, rules.FindNextZone(1, TestZones.Super.Id, ZoneRules.ENDLESS_FINAL_ZONE));
        }

        [Test]
        public void DefaultAssetsShareGameplayAndPresentationResolution()
        {
            var configuration = AssetDatabase.LoadMainAssetAtPath("Assets/_Game/Data/Zones_Default.asset");
            Assert.IsTrue(configuration);
            // Configuration lives in Assembly-CSharp, which a named test assembly cannot reference.
            // Keep core tests independent and exercise the asset adapter through its public contract.
            Type catalogType = Type.GetType("Vertigo.WheelOfFortune.Data.ZoneCatalog, Assembly-CSharp", true);
            object catalog = Activator.CreateInstance(catalogType, new object[] { configuration });
            ZoneRules rules = (ZoneRules)configuration.GetType().GetMethod("ToRules").Invoke(configuration, null);
            int startingZone = (int)catalogType.GetProperty("StartingZone").GetValue(catalog);
            int finalZone = (int)catalogType.GetProperty("FinalZone").GetValue(catalog);
            var resolve = catalogType.GetMethod("Resolve");
            var findNext = catalogType.GetMethod("FindNextZone");
            for (int zone = startingZone; zone <= finalZone; zone++)
            {
                var definition = (UnityEngine.Object)resolve.Invoke(catalog, new object[] { zone });
                var rule = new SerializedObject(definition);
                Assert.AreEqual(rules.Resolve(zone).Id, rule.FindProperty("_id").stringValue);
                Assert.IsTrue(rule.FindProperty("_wheel").objectReferenceValue);
                Assert.IsTrue(rule.FindProperty("_currentBackground").objectReferenceValue);
            }
            object safe = resolve.Invoke(catalog, new object[] { 5 });
            object super = resolve.Invoke(catalog, new object[] { 30 });
            Assert.AreEqual(35, findNext.Invoke(catalog, new object[] { 29, safe }));
            Assert.AreEqual(30, findNext.Invoke(catalog, new object[] { 29, super }));
            Assert.AreEqual(ZoneRules.NO_ZONE, findNext.Invoke(catalog, new object[] { 59, safe }));
            Assert.AreEqual(ZoneRules.NO_ZONE, findNext.Invoke(catalog, new object[] { 60, super }));
        }
    }
}
