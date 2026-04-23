using NUnit.Framework;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class GameEventBusTests
    {
        private GameEventBus _bus;

        private class EventA { public int Value; }
        private class EventB { public string Text; }

        [SetUp]
        public void SetUp()
        {
            _bus = new GameEventBus();
        }

        [Test]
        public void Raise_WithNoSubscribers_DoesNothing()
        {
            Assert.DoesNotThrow(() => _bus.Raise(new EventA { Value = 1 }));
        }

        [Test]
        public void Subscribe_ThenRaise_HandlerReceivesPayload()
        {
            EventA received = null;
            _bus.Subscribe<EventA>(e => received = e);

            _bus.Raise(new EventA { Value = 42 });

            Assert.IsNotNull(received);
            Assert.AreEqual(42, received.Value);
        }

        [Test]
        public void MultipleSubscribers_AllInvoked()
        {
            var calls = 0;
            _bus.Subscribe<EventA>(_ => calls++);
            _bus.Subscribe<EventA>(_ => calls++);
            _bus.Subscribe<EventA>(_ => calls++);

            _bus.Raise(new EventA());

            Assert.AreEqual(3, calls);
        }

        [Test]
        public void Unsubscribe_RemovesOnlyTheTargetHandler()
        {
            var aCalls = 0;
            var bCalls = 0;
            void HandlerA(EventA _) => aCalls++;
            void HandlerB(EventA _) => bCalls++;

            _bus.Subscribe<EventA>(HandlerA);
            _bus.Subscribe<EventA>(HandlerB);
            _bus.Unsubscribe<EventA>(HandlerA);

            _bus.Raise(new EventA());

            Assert.AreEqual(0, aCalls);
            Assert.AreEqual(1, bCalls);
        }

        [Test]
        public void Unsubscribe_NotPreviouslySubscribed_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _bus.Unsubscribe<EventA>(_ => { }));
        }

        [Test]
        public void EventTypes_AreIsolated()
        {
            var aCalls = 0;
            var bCalls = 0;
            _bus.Subscribe<EventA>(_ => aCalls++);
            _bus.Subscribe<EventB>(_ => bCalls++);

            _bus.Raise(new EventA());

            Assert.AreEqual(1, aCalls);
            Assert.AreEqual(0, bCalls);
        }

        [Test]
        public void NullHandler_OnSubscribe_IsIgnored()
        {
            Assert.DoesNotThrow(() => _bus.Subscribe<EventA>(null));
            Assert.DoesNotThrow(() => _bus.Raise(new EventA()));
        }

        [Test]
        public void NullHandler_OnUnsubscribe_IsIgnored()
        {
            _bus.Subscribe<EventA>(_ => { });
            Assert.DoesNotThrow(() => _bus.Unsubscribe<EventA>(null));
        }

        [Test]
        public void SubscribeSameHandlerTwice_RaisesTwice()
        {
            // Document the underlying multicast-delegate semantics so future
            // readers are not surprised. Subscribers responsible for not double-subscribing.
            var calls = 0;
            void Handler(EventA _) => calls++;

            _bus.Subscribe<EventA>(Handler);
            _bus.Subscribe<EventA>(Handler);

            _bus.Raise(new EventA());

            Assert.AreEqual(2, calls);
        }

        [Test]
        public void UnsubscribeAllInstances_NeedsAsManyUnsubscribes()
        {
            var calls = 0;
            void Handler(EventA _) => calls++;

            _bus.Subscribe<EventA>(Handler);
            _bus.Subscribe<EventA>(Handler);
            _bus.Unsubscribe<EventA>(Handler);
            _bus.Raise(new EventA());

            Assert.AreEqual(1, calls);

            _bus.Unsubscribe<EventA>(Handler);
            _bus.Raise(new EventA());

            Assert.AreEqual(1, calls);
        }
    }
}
