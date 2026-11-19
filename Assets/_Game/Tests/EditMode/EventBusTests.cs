using NUnit.Framework;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class EventBusTests
    {
        private EventBus _bus;
        private int _received;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _received = 0;
        }

        [Test]
        public void PublishesOnlyToMatchingMessageType()
        {
            _bus.Subscribe<BombHit>(OnBombHit);
            _bus.Publish(new ZoneAdvanced(5));
            Assert.AreEqual(0, _received);
            _bus.Publish(new BombHit(3));
            Assert.AreEqual(3, _received);
        }

        [Test]
        public void DuplicateSubscriptionDoesNotDuplicateDelivery()
        {
            _bus.Subscribe<BombHit>(OnBombHit);
            _bus.Subscribe<BombHit>(OnBombHit);
            _bus.Publish(new BombHit(1));
            Assert.AreEqual(1, _received);
        }

        [Test]
        public void UnsubscribeStopsDelivery()
        {
            _bus.Subscribe<BombHit>(OnBombHit);
            _bus.Unsubscribe<BombHit>(OnBombHit);
            _bus.Publish(new BombHit(1));
            Assert.AreEqual(0, _received);
        }

        [Test]
        public void RepeatedSubscriptionLifecycleDoesNotAccumulateListeners()
        {
            for (int i = 0; i < 5; i++)
            {
                _bus.Subscribe<BombHit>(OnBombHit);
                _bus.Publish(new BombHit(1));
                _bus.Unsubscribe<BombHit>(OnBombHit);
            }
            Assert.AreEqual(5, _received);
        }

        [Test]
        public void InstancesDoNotShareSubscribers()
        {
            _bus.Subscribe<BombHit>(OnBombHit);
            new EventBus().Publish(new BombHit(1));
            Assert.AreEqual(0, _received);
        }

        [Test]
        public void SubscriptionChangesDuringPublishApplyToNextMessage()
        {
            _bus.Subscribe<BombHit>(RemoveListener);
            _bus.Subscribe<BombHit>(OnBombHit);
            _bus.Publish(new BombHit(1));
            _bus.Publish(new BombHit(1));
            Assert.AreEqual(1, _received);
        }

        private void OnBombHit(BombHit message)
        {
            _received += message.Zone;
        }

        private void RemoveListener(BombHit message)
        {
            _bus.Unsubscribe<BombHit>(OnBombHit);
        }
    }
}
