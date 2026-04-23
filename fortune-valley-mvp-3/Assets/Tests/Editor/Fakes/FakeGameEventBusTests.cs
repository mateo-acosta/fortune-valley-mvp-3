using NUnit.Framework;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class FakeGameEventBusTests
    {
        private class EventX { public int Value; }
        private class EventY { }

        [Test]
        public void Raise_RecordsPayloadInRaisedEvents()
        {
            var bus = new FakeGameEventBus();
            bus.Raise(new EventX { Value = 1 });
            bus.Raise(new EventY());

            Assert.AreEqual(2, bus.RaisedEvents.Count);
            Assert.IsInstanceOf<EventX>(bus.RaisedEvents[0]);
            Assert.IsInstanceOf<EventY>(bus.RaisedEvents[1]);
        }

        [Test]
        public void CountOf_ReturnsOnlyMatchingEvents()
        {
            var bus = new FakeGameEventBus();
            bus.Raise(new EventX { Value = 1 });
            bus.Raise(new EventX { Value = 2 });
            bus.Raise(new EventY());

            Assert.AreEqual(2, bus.CountOf<EventX>());
            Assert.AreEqual(1, bus.CountOf<EventY>());
        }

        [Test]
        public void Subscribe_StillNotifiesHandlersDuringRaise()
        {
            var bus = new FakeGameEventBus();
            EventX received = null;
            bus.Subscribe<EventX>(e => received = e);

            bus.Raise(new EventX { Value = 99 });

            Assert.IsNotNull(received);
            Assert.AreEqual(99, received.Value);
        }

        [Test]
        public void Unsubscribe_StopsNotifyingButStillRecords()
        {
            var bus = new FakeGameEventBus();
            var calls = 0;
            void Handler(EventX _) => calls++;
            bus.Subscribe<EventX>(Handler);
            bus.Unsubscribe<EventX>(Handler);

            bus.Raise(new EventX());

            Assert.AreEqual(0, calls);
            Assert.AreEqual(1, bus.RaisedEvents.Count);
        }
    }
}
