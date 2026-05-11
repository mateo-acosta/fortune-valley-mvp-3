using System;
using NUnit.Framework;
using FortuneValley.Managers.Notifications;
using FortuneValley.Tests.Fakes;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class PlayerPrefsDebouncedFlusherTests
    {
        private InMemoryKeyValueStore _store;
        private FakeNowProvider _clock;
        private PlayerPrefsDebouncedFlusher _flusher;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryKeyValueStore();
            _clock = new FakeNowProvider();
            _flusher = new PlayerPrefsDebouncedFlusher(_store, _clock, TimeSpan.FromSeconds(5));
        }

        [Test]
        public void GetFlag_WhenUnset_ReturnsFalse()
        {
            Assert.IsFalse(_flusher.GetFlag("foo"));
        }

        [Test]
        public void SetFlag_True_WritesToStoreImmediately()
        {
            _flusher.SetFlag("foo", true);
            Assert.AreEqual(1, _store.GetInt("foo", 0));
            Assert.IsTrue(_flusher.GetFlag("foo"));
        }

        [Test]
        public void SetFlag_False_WritesZero()
        {
            _store.SetInt("foo", 1);
            _flusher.SetFlag("foo", false);
            Assert.AreEqual(0, _store.GetInt("foo", 0));
        }

        [Test]
        public void SetFlag_RepeatedSameValue_DoesNotMarkDirtyAgain()
        {
            _flusher.SetFlag("foo", true);
            _clock.AdvanceSeconds(10);
            _flusher.MaybeFlush();
            int firstSaveCount = _store.SaveCallCount;

            _flusher.SetFlag("foo", true); // same value
            _flusher.MaybeFlush();

            Assert.AreEqual(firstSaveCount, _store.SaveCallCount,
                "Setting same value should not require another flush");
        }

        [Test]
        public void MaybeFlush_BeforeInterval_DoesNotFlush()
        {
            _flusher.SetFlag("a", true);
            _flusher.SetFlag("b", true);

            _clock.AdvanceSeconds(2); // before interval (5s)
            _flusher.MaybeFlush();

            Assert.AreEqual(0, _store.SaveCallCount);
            Assert.AreEqual(2, _flusher.DirtyCount);
        }

        [Test]
        public void MaybeFlush_AfterInterval_FlushesOnce()
        {
            _flusher.SetFlag("a", true);
            _flusher.SetFlag("b", true);
            _flusher.SetFlag("c", true);

            _clock.AdvanceSeconds(6); // past interval
            _flusher.MaybeFlush();

            Assert.AreEqual(1, _store.SaveCallCount,
                "Three SetFlag calls within interval should produce ONE Save");
            Assert.AreEqual(0, _flusher.DirtyCount);
        }

        [Test]
        public void ForceFlush_FlushesEvenWithinInterval()
        {
            _flusher.SetFlag("a", true);
            _flusher.ForceFlush();

            Assert.AreEqual(1, _store.SaveCallCount);
            Assert.AreEqual(0, _flusher.DirtyCount);
        }

        [Test]
        public void ForceFlush_WhenNoDirty_DoesNotSave()
        {
            _flusher.ForceFlush();
            Assert.AreEqual(0, _store.SaveCallCount);
        }

        [Test]
        public void Burst_WithinInterval_CoalescesIntoOneSave()
        {
            for (int i = 0; i < 20; i++)
            {
                _flusher.SetFlag($"key-{i}", true);
            }
            _clock.AdvanceSeconds(6);
            _flusher.MaybeFlush();

            Assert.AreEqual(1, _store.SaveCallCount,
                "20 SetFlags inside interval should coalesce to one Save");
        }

        [Test]
        public void TwoFlushCycles_ProduceTwoSaves()
        {
            _flusher.SetFlag("a", true);
            _clock.AdvanceSeconds(6);
            _flusher.MaybeFlush();

            _flusher.SetFlag("b", true);
            _clock.AdvanceSeconds(6);
            _flusher.MaybeFlush();

            Assert.AreEqual(2, _store.SaveCallCount);
        }
    }
}
