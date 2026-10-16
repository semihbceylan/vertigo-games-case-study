using System;
using System.Collections.Generic;
using NUnit.Framework;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Tests
{
    // ---- test doubles ----

    internal sealed class ScriptedRandom : IRandomSource
    {
        private readonly Queue<int> _ints = new Queue<int>();
        private readonly Queue<double> _doubles = new Queue<double>();

        public ScriptedRandom EnqueueInt(int value) { _ints.Enqueue(value); return this; }
        public ScriptedRandom EnqueueDouble(double value) { _doubles.Enqueue(value); return this; }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            return _ints.Count > 0 ? _ints.Dequeue() : minInclusive;
        }

        public double NextDouble()
        {
            return _doubles.Count > 0 ? _doubles.Dequeue() : 0.999;
        }
    }

    internal sealed class FakeWheelFactory : IZoneWheelFactory
    {
        private readonly Func<int, ZoneKind, ZoneWheel> _resolver;

        public FakeWheelFactory(Func<int, ZoneKind, ZoneWheel> resolver)
        {
            _resolver = resolver;
        }

        public ZoneWheel CreateWheel(int zone, ZoneKind kind, IRandomSource random)
        {
            return _resolver(zone, kind);
        }
    }

    internal static class Wheels
    {
        public static ZoneWheel RewardOnly(params string[] rewardIds)
        {
            var slices = new WheelSlice[rewardIds.Length];
            for (int i = 0; i < rewardIds.Length; i++)
                slices[i] = new WheelSlice(false, new RewardGrant(rewardIds[i], 10));
            return new ZoneWheel(slices);
        }

        public static ZoneWheel WithBomb(int sliceCount, int bombIndex, string rewardId)
        {
            var slices = new WheelSlice[sliceCount];
            for (int i = 0; i < sliceCount; i++)
                slices[i] = i == bombIndex
                    ? WheelSlice.Bomb
                    : new WheelSlice(false, new RewardGrant(rewardId, 10));
            return new ZoneWheel(slices);
        }
    }

    // ---- zone classification ----

    public sealed class ZoneClassifierTests
    {
        private static readonly ZoneRules Rules = new ZoneRules(5, 30);

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(7)]
        public void NormalZones(int zone) => Assert.AreEqual(ZoneKind.Normal, ZoneClassifier.Classify(zone, Rules));

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(25)]
        public void EveryFifthZoneIsSafe(int zone) => Assert.AreEqual(ZoneKind.Safe, ZoneClassifier.Classify(zone, Rules));

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(90)]
        public void EveryThirtiethZoneIsSuper(int zone) => Assert.AreEqual(ZoneKind.Super, ZoneClassifier.Classify(zone, Rules));

        [Test]
        public void SuperTakesPrecedenceOverSafe()
        {
            Assert.AreEqual(ZoneKind.Super, ZoneClassifier.Classify(30, Rules));
        }

        [Test]
        public void InvalidZoneThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ZoneClassifier.Classify(0, Rules));
        }

        [Test]
        public void SafeAndSuperAreBombFree()
        {
            Assert.IsTrue(ZoneClassifier.IsBombFree(ZoneKind.Safe));
            Assert.IsTrue(ZoneClassifier.IsBombFree(ZoneKind.Super));
            Assert.IsFalse(ZoneClassifier.IsBombFree(ZoneKind.Normal));
        }
    }

    // ---- wallet ----

    public sealed class RewardWalletTests
    {
        [Test]
        public void AddAccumulatesAmountAndTotals()
        {
            var wallet = new RewardWallet();
            wallet.Add(new RewardGrant("gold", 100));
            wallet.Add(new RewardGrant("gold", 50));
            wallet.Add(new RewardGrant("cash", 25));

            Assert.AreEqual(175, wallet.AccumulatedAmount);
            Assert.AreEqual(3, wallet.Count);
            Assert.AreEqual(150, wallet.Totals["gold"]);
            Assert.AreEqual(25, wallet.Totals["cash"]);
        }

        [Test]
        public void ClearResetsEverything()
        {
            var wallet = new RewardWallet();
            wallet.Add(new RewardGrant("gold", 100));
            wallet.Clear();

            Assert.AreEqual(0, wallet.AccumulatedAmount);
            Assert.IsTrue(wallet.IsEmpty);
            Assert.AreEqual(0, wallet.Totals.Count);
        }

        [Test]
        public void ChangedFiresOnAddAndClear()
        {
            var wallet = new RewardWallet();
            int events = 0;
            wallet.Changed += () => events++;

            wallet.Add(new RewardGrant("gold", 1));
            wallet.Clear();

            Assert.AreEqual(2, events);
        }
    }

    // ---- wheel building ----

    public sealed class ZoneWheelBuilderTests
    {
        private static readonly List<RewardPoolEntry> Pool = new List<RewardPoolEntry>
        {
            new RewardPoolEntry("gold", 100, 1.0)
        };

        [Test]
        public void NormalZoneHasExactlyOneBomb()
        {
            var builder = new ZoneWheelBuilder();
            var random = new ScriptedRandom().EnqueueInt(3).EnqueueDouble(0.5);

            var wheel = builder.Build(8, ZoneKind.Normal, Pool, random);

            Assert.AreEqual(8, wheel.SliceCount);
            Assert.AreEqual(1, wheel.BombCount);
            Assert.IsTrue(wheel[3].IsBomb);
        }

        [Test]
        public void SafeZoneHasNoBombs()
        {
            var builder = new ZoneWheelBuilder();
            var wheel = builder.Build(8, ZoneKind.Safe, Pool, new ScriptedRandom());

            Assert.AreEqual(0, wheel.BombCount);
        }

        [Test]
        public void SuperZoneHasNoBombsAndFillsRewards()
        {
            var builder = new ZoneWheelBuilder();
            var wheel = builder.Build(10, ZoneKind.Super, Pool, new ScriptedRandom());

            Assert.AreEqual(0, wheel.BombCount);
            for (int i = 0; i < wheel.SliceCount; i++)
            {
                Assert.IsFalse(wheel[i].IsBomb);
                Assert.AreEqual("gold", wheel[i].Reward.RewardId);
            }
        }
    }

    // ---- session ----

    public sealed class GameSessionTests
    {
        private static readonly ZoneRules Rules = new ZoneRules(5, 30);

        private static GameSession CreateSession(Func<int, ZoneKind, ZoneWheel> resolver, ScriptedRandom random, int startingZone = 1)
        {
            return new GameSession(Rules, new FakeWheelFactory((zone, kind) => resolver(zone, kind)), random, startingZone);
        }

        [Test]
        public void StartsIdleAndCanSpin()
        {
            var session = CreateSession((z, k) => Wheels.RewardOnly("gold", "cash"), new ScriptedRandom());

            Assert.AreEqual(SessionPhase.Idle, session.Phase);
            Assert.IsTrue(session.CanSpin);
            Assert.IsFalse(session.CanLeave); // zone 1 is Normal
        }

        [Test]
        public void BeginSpinSelectsOutcomeThenBlocksFurtherSpins()
        {
            var random = new ScriptedRandom().EnqueueInt(1);
            var session = CreateSession((z, k) => Wheels.RewardOnly("gold", "cash"), random);

            session.BeginSpin();

            Assert.AreEqual(SessionPhase.Spinning, session.Phase);
            Assert.AreEqual(1, session.PendingSliceIndex);
            Assert.IsFalse(session.CanSpin);
        }

        [Test]
        public void ResolvingRewardCollectsAndAdvancesZone()
        {
            var random = new ScriptedRandom().EnqueueInt(0);
            var session = CreateSession((z, k) => Wheels.RewardOnly("gold", "cash"), random);

            session.BeginSpin();
            session.ResolvePendingSpin();

            Assert.AreEqual(SessionPhase.Idle, session.Phase);
            Assert.AreEqual(10, session.Wallet.AccumulatedAmount);
            Assert.AreEqual(2, session.Zone);
        }

        [Test]
        public void BombBustsButKeepsRewards()
        {
            // zone 1: reward, zone 2+: bomb
            var resolver = (Func<int, ZoneKind, ZoneWheel>)((zone, kind) =>
                zone == 1 ? Wheels.RewardOnly("gold") : Wheels.WithBomb(4, 0, "gold"));

            var random = new ScriptedRandom().EnqueueInt(0).EnqueueInt(0);
            var session = CreateSession(resolver, random);

            session.BeginSpin();
            session.ResolvePendingSpin();
            Assert.AreEqual(10, session.Wallet.AccumulatedAmount);

            session.BeginSpin();
            session.ResolvePendingSpin();

            Assert.AreEqual(SessionPhase.Bust, session.Phase);
            Assert.AreEqual(10, session.Wallet.AccumulatedAmount); // kept until restart
            Assert.IsFalse(session.CanSpin);
            Assert.IsTrue(session.CanRestart);
        }

        [Test]
        public void RestartAfterBombClearsRewards()
        {
            var resolver = (Func<int, ZoneKind, ZoneWheel>)((zone, kind) =>
                zone == 1 ? Wheels.RewardOnly("gold") : Wheels.WithBomb(4, 0, "gold"));

            var random = new ScriptedRandom().EnqueueInt(0).EnqueueInt(0);
            var session = CreateSession(resolver, random);

            session.BeginSpin();
            session.ResolvePendingSpin();
            session.BeginSpin();
            session.ResolvePendingSpin();
            Assert.AreEqual(SessionPhase.Bust, session.Phase);

            session.Restart();

            Assert.AreEqual(SessionPhase.Idle, session.Phase);
            Assert.AreEqual(1, session.Zone);
            Assert.AreEqual(0, session.Wallet.AccumulatedAmount);
        }

        [Test]
        public void LeaveIsBlockedInNormalZone()
        {
            var session = CreateSession((z, k) => Wheels.RewardOnly("gold"), new ScriptedRandom());
            Assert.IsFalse(session.CanLeave);
            Assert.Throws<InvalidOperationException>(() => session.Leave());
        }

        [Test]
        public void LeaveIsAllowedInSafeZoneAndBanksRewards()
        {
            var random = new ScriptedRandom();
            for (int i = 0; i < 4; i++) random.EnqueueInt(0); // 4 spins to reach zone 5 (Safe)

            var session = CreateSession((z, k) => Wheels.RewardOnly("gold"), random);

            for (int i = 0; i < 4; i++)
            {
                session.BeginSpin();
                session.ResolvePendingSpin();
            }

            Assert.AreEqual(5, session.Zone);
            Assert.AreEqual(ZoneKind.Safe, session.CurrentZoneKind);
            Assert.IsTrue(session.CanLeave);

            session.Leave();

            Assert.AreEqual(SessionPhase.Completed, session.Phase);
            Assert.AreEqual(40, session.BankedTotal);
            Assert.IsFalse(session.CanSpin);
        }

        [Test]
        public void LeaveIsAllowedInNormalZoneOnceRewardsExist()
        {
            var random = new ScriptedRandom().EnqueueInt(0);
            var session = CreateSession((z, k) => Wheels.RewardOnly("gold"), random);

            session.BeginSpin();
            session.ResolvePendingSpin(); // zone 1 (Normal) -> zone 2, wallet = 10

            Assert.AreEqual(2, session.Zone);
            Assert.IsFalse(ZoneClassifier.IsBombFree(session.CurrentZoneKind));
            Assert.IsTrue(session.CanLeave);

            session.Leave();

            Assert.AreEqual(SessionPhase.Completed, session.Phase);
            Assert.AreEqual(10, session.BankedTotal);
        }

        [Test]
        public void RestartAfterBombResetsRun()
        {
            var resolver = (Func<int, ZoneKind, ZoneWheel>)((zone, kind) =>
                zone == 1 ? Wheels.WithBomb(2, 0, "gold") : Wheels.WithBomb(2, 0, "gold"));

            var random = new ScriptedRandom().EnqueueInt(0);
            var session = CreateSession(resolver, random);

            session.BeginSpin();
            session.ResolvePendingSpin();
            Assert.AreEqual(SessionPhase.Bust, session.Phase);

            session.Restart();

            Assert.AreEqual(SessionPhase.Idle, session.Phase);
            Assert.AreEqual(1, session.Zone);
            Assert.IsTrue(session.Wallet.IsEmpty);
            Assert.IsTrue(session.CanSpin);
        }

        [Test]
        public void RestartIsBlockedWhileNotBusted()
        {
            var session = CreateSession((z, k) => Wheels.RewardOnly("gold"), new ScriptedRandom());
            Assert.Throws<InvalidOperationException>(() => session.Restart());
        }

        [Test]
        public void ReviveResumesRunAtSameZoneAfterBomb()
        {
            var resolver = (Func<int, ZoneKind, ZoneWheel>)((zone, kind) => Wheels.WithBomb(2, 0, "gold"));
            var random = new ScriptedRandom().EnqueueInt(0);
            var session = CreateSession(resolver, random);

            session.BeginSpin();
            session.ResolvePendingSpin();
            Assert.AreEqual(SessionPhase.Bust, session.Phase);
            Assert.AreEqual(1, session.Zone);
            Assert.IsTrue(session.CanRevive);

            session.Revive();

            Assert.AreEqual(SessionPhase.Idle, session.Phase);
            Assert.AreEqual(1, session.Zone);
            Assert.IsTrue(session.CanSpin);
            Assert.IsFalse(session.CanRevive);
        }

        [Test]
        public void ReviveKeepsRewards()
        {
            var resolver = (Func<int, ZoneKind, ZoneWheel>)((zone, kind) =>
                zone == 1 ? Wheels.RewardOnly("gold") : Wheels.WithBomb(2, 0, "gold"));

            var random = new ScriptedRandom().EnqueueInt(0).EnqueueInt(0);
            var session = CreateSession(resolver, random);

            session.BeginSpin();
            session.ResolvePendingSpin(); // zone 1 reward, advance to zone 2
            Assert.AreEqual(10, session.Wallet.AccumulatedAmount);

            session.BeginSpin();
            session.ResolvePendingSpin(); // zone 2 bomb
            Assert.AreEqual(SessionPhase.Bust, session.Phase);

            session.Revive();

            Assert.AreEqual(SessionPhase.Idle, session.Phase);
            Assert.AreEqual(2, session.Zone);
            Assert.AreEqual(10, session.Wallet.AccumulatedAmount);
        }

        [Test]
        public void ReviveIsBlockedWhileNotBusted()
        {
            var session = CreateSession((z, k) => Wheels.RewardOnly("gold"), new ScriptedRandom());
            Assert.Throws<InvalidOperationException>(() => session.Revive());
        }

        [Test]
        public void CannotResolveWithoutSpin()
        {
            var session = CreateSession((z, k) => Wheels.RewardOnly("gold"), new ScriptedRandom());
            Assert.Throws<InvalidOperationException>(() => session.ResolvePendingSpin());
        }

        [Test]
        public void CollectingFinalZoneRewardCompletesAndBanksRun()
        {
            var random = new ScriptedRandom().EnqueueInt(0).EnqueueInt(0).EnqueueInt(0);
            var session = new GameSession(
                Rules,
                new FakeWheelFactory((z, k) => Wheels.RewardOnly("gold", "cash")),
                random,
                1,
                3);

            long banked = -1;
            session.RunCompleted += total => banked = total;

            for (int i = 0; i < 2; i++)
            {
                session.BeginSpin();
                session.ResolvePendingSpin();
            }

            Assert.AreEqual(3, session.Zone);
            Assert.AreEqual(SessionPhase.Idle, session.Phase);

            // Collecting the zone-3 reward banks everything and finishes the run.
            session.BeginSpin();
            session.ResolvePendingSpin();

            Assert.AreEqual(SessionPhase.Completed, session.Phase);
            Assert.AreEqual(30, session.Wallet.AccumulatedAmount);
            Assert.AreEqual(30, session.BankedTotal);
            Assert.AreEqual(30, banked);
            Assert.IsFalse(session.CanSpin);
            Assert.IsFalse(session.CanLeave);
        }

        [Test]
        public void ZeroFinalZoneKeepsRunEndless()
        {
            var random = new ScriptedRandom();
            for (int i = 0; i < 5; i++) random.EnqueueInt(0);

            var session = CreateSession((z, k) => Wheels.RewardOnly("gold"), random);

            for (int i = 0; i < 5; i++)
            {
                session.BeginSpin();
                session.ResolvePendingSpin();
            }

            Assert.AreEqual(SessionPhase.Idle, session.Phase);
            Assert.AreEqual(6, session.Zone);
        }
    }
}
