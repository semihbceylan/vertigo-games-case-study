using NUnit.Framework;
using Vertigo.WheelOfFortune.Core;

namespace Vertigo.WheelOfFortune.Tests
{
    internal readonly struct TestEventA
    {
        public readonly int Value;

        public TestEventA(int value)
        {
            Value = value;
        }
    }

    internal readonly struct TestEventB
    {
        public readonly int Value;

        public TestEventB(int value)
        {
            Value = value;
        }
    }

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
            _bus.Subscribe<TestEventA>(OnTestEventA);
            _bus.Publish(new TestEventB(5));
            Assert.AreEqual(0, _received);
            _bus.Publish(new TestEventA(3));
            Assert.AreEqual(3, _received);
        }

        [Test]
        public void DuplicateSubscriptionDoesNotDuplicateDelivery()
        {
            _bus.Subscribe<TestEventA>(OnTestEventA);
            _bus.Subscribe<TestEventA>(OnTestEventA);
            _bus.Publish(new TestEventA(1));
            Assert.AreEqual(1, _received);
        }

        [Test]
        public void UnsubscribeStopsDelivery()
        {
            _bus.Subscribe<TestEventA>(OnTestEventA);
            _bus.Unsubscribe<TestEventA>(OnTestEventA);
            _bus.Publish(new TestEventA(1));
            Assert.AreEqual(0, _received);
        }

        [Test]
        public void RepeatedSubscriptionLifecycleDoesNotAccumulateListeners()
        {
            for (int i = 0; i < 5; i++)
            {
                _bus.Subscribe<TestEventA>(OnTestEventA);
                _bus.Publish(new TestEventA(1));
                _bus.Unsubscribe<TestEventA>(OnTestEventA);
            }
            Assert.AreEqual(5, _received);
        }

        [Test]
        public void InstancesDoNotShareSubscribers()
        {
            _bus.Subscribe<TestEventA>(OnTestEventA);
            new EventBus().Publish(new TestEventA(1));
            Assert.AreEqual(0, _received);
        }

        [Test]
        public void SubscriptionChangesDuringPublishApplyToNextMessage()
        {
            _bus.Subscribe<TestEventA>(RemoveListener);
            _bus.Subscribe<TestEventA>(OnTestEventA);
            _bus.Publish(new TestEventA(1));
            _bus.Publish(new TestEventA(1));
            Assert.AreEqual(1, _received);
        }

        private void OnTestEventA(TestEventA message)
        {
            _received += message.Value;
        }

        private void RemoveListener(TestEventA message)
        {
            _bus.Unsubscribe<TestEventA>(OnTestEventA);
        }
    }
}
