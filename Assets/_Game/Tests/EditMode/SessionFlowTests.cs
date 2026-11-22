using System;
using NUnit.Framework;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class SessionFlowTests
    {
        private sealed class Presentation : ISessionPresentation
        {
            public Action SpinComplete;
            public Action RewardComplete;
            public Action TransitionComplete;
            public int ResultsCount;
            public int LossCount;

            public void PlaySpin(int sliceIndex, ZoneWheel wheel, Action onComplete)
            {
                SpinComplete = onComplete;
            }

            public void PresentReward(RewardGrant grant, int sliceIndex, Action onComplete)
            {
                RewardComplete = onComplete;
            }

            public void PlayZoneTransition(Action onComplete)
            {
                TransitionComplete = onComplete;
            }

            public void ShowLoss(int zone) { LossCount++; }
            public void ShowResults() { ResultsCount++; }
            public void Reset() { }
            public void Cancel() { }
        }

        private Presentation _presentation;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _presentation = new Presentation();
            _session = new GameSession(TestZones.CreateRules(),
                new FakeWheelFactory(RewardWheel), new ScriptedRandom(),
                startingZone: 5, presentation: _presentation);
        }

        [Test]
        public void SpinRewardAndTransitionEachBlockInputUntilCompleted()
        {
            Assert.IsTrue(_session.IsLeaveAllowed);
            _session.BeginSpin();
            AssertBlocked();
            _presentation.SpinComplete();
            AssertBlocked();
            Assert.AreEqual(5, _session.Zone);
            Assert.AreEqual(10, _session.Wallet.AccumulatedAmount);
            _presentation.RewardComplete();
            AssertBlocked();
            Assert.AreEqual(6, _session.Zone);
            Assert.IsTrue(_session.IsZoneTransitioning);
            _presentation.TransitionComplete();
            Assert.IsTrue(_session.IsSpinAllowed);
        }

        [Test]
        public void FinalResultsWaitForRewardPresentation()
        {
            _session = new GameSession(TestZones.CreateRules(),
                new FakeWheelFactory(RewardWheel), new ScriptedRandom(),
                startingZone: 5, finalZone: 5, presentation: _presentation);
            _session.BeginSpin();
            _presentation.SpinComplete();
            Assert.IsFalse(_session.IsCompleted);
            Assert.AreEqual(0, _presentation.ResultsCount);
            _presentation.RewardComplete();
            Assert.IsTrue(_session.IsCompleted);
            Assert.AreEqual(1, _presentation.ResultsCount);
            Assert.AreEqual(10, _session.BankedTotal);
        }

        [Test]
        public void DuplicateCompletionCannotCollectTwice()
        {
            _session.BeginSpin();
            Action callback = _presentation.SpinComplete;
            callback();
            callback();
            Assert.AreEqual(10, _session.Wallet.AccumulatedAmount);
        }

        [Test]
        public void StaleCompletionAfterResetCannotResolveNewSpin()
        {
            _session.BeginSpin();
            Action stale = _presentation.SpinComplete;
            _session.StartNewRun();
            _session.BeginSpin();
            stale();
            Assert.IsTrue(_session.Wallet.IsEmpty);
            _presentation.SpinComplete();
            Assert.AreEqual(10, _session.Wallet.AccumulatedAmount);
        }

        [Test]
        public void BombClearsWalletBeforeLossPresentationAndRestartsAtConfiguredZone()
        {
            _session = new GameSession(TestZones.CreateRules(),
                new FakeWheelFactory(BombWheel), new ScriptedRandom(),
                startingZone: 2, presentation: _presentation);
            _session.Wallet.Add(new RewardGrant("gold", 100));
            _session.BeginSpin();
            _presentation.SpinComplete();
            Assert.IsTrue(_session.Wallet.IsEmpty);
            Assert.AreEqual(1, _presentation.LossCount);
            Assert.IsTrue(_session.IsRestartAllowed);
            _session.Restart();
            Assert.AreEqual(2, _session.Zone);
            Assert.IsTrue(_session.IsSpinAllowed);
        }

        private void AssertBlocked()
        {
            Assert.IsFalse(_session.IsSpinAllowed);
            Assert.IsFalse(_session.IsLeaveAllowed);
            Assert.Throws<InvalidOperationException>(_session.BeginSpin);
            Assert.Throws<InvalidOperationException>(_session.Leave);
        }

        private static ZoneWheel RewardWheel(int zone, ZoneKind kind)
        {
            return Wheels.RewardOnly("gold");
        }

        private static ZoneWheel BombWheel(int zone, ZoneKind kind)
        {
            return Wheels.WithBomb(2, 0, "gold");
        }
    }
}
