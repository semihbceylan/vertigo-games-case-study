using NUnit.Framework;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class ForcedOutcomeTests
    {
        [Test]
        public void ForcedValueIsUsedOnceThenDrawsResume()
        {
            var forced = new ForcedOutcomeRandom(new ScriptedRandom().EnqueueInt(1).EnqueueInt(2));
            forced.Force(5);
            Assert.AreEqual(5, forced.NextInt(0, 8));
            Assert.IsFalse(forced.HasForcedOutcome);
            Assert.AreEqual(1, forced.NextInt(0, 8));
        }

        [Test]
        public void OutOfRangeForcedValueIsDiscarded()
        {
            var forced = new ForcedOutcomeRandom(new ScriptedRandom().EnqueueInt(3));
            forced.Force(9);
            Assert.AreEqual(3, forced.NextInt(0, 8), "A slot the wheel lacks must never be selected.");
            Assert.IsFalse(forced.HasForcedOutcome);
        }

        [Test]
        public void ForcingDrivesOnlyTheSpinNotWheelBuilding()
        {
            var spinRandom = new ForcedOutcomeRandom(new ScriptedRandom());
            var session = new GameSession(TestZones.CreateRules(),
                new FakeWheelFactory((zone, kind) => Wheels.WithBomb(4, 2, "gold")), new ScriptedRandom(),
                spinRandom: spinRandom);
            spinRandom.Force(2);
            session.JumpToZone(3);
            Assert.IsTrue(spinRandom.HasForcedOutcome, "Rebuilding the wheel must not consume the forced spin.");
            session.BeginSpin();
            Assert.AreEqual(2, session.PendingSliceIndex);
            session.ResolvePendingSpin();
            Assert.IsTrue(session.IsRestartAllowed, "The forced slot is the bomb, so the run is lost.");
        }
    }
}
