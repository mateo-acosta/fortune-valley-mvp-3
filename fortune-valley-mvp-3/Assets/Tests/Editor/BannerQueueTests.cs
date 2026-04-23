using System;
using System.Collections.Generic;
using NUnit.Framework;
using FortuneValley.Domain.Notifications;
using FortuneValley.Managers.Notifications;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class BannerQueueTests
    {
        private static GuidanceBannerRequest Banner(GuidanceSeverity severity, string id = null)
        {
            return new GuidanceBannerRequest(
                title: $"t-{id ?? severity.ToString()}",
                message: $"m-{id ?? severity.ToString()}",
                severity: severity,
                targetIntent: GuidanceTargetIntent.None,
                targetData: null,
                sourceTipId: id ?? severity.ToString());
        }

        // ===============================================================
        // BASIC ENQUEUE / DEQUEUE
        // ===============================================================

        [Test]
        public void NewQueue_IsEmptyAndNotFull()
        {
            var q = new BannerQueue();
            Assert.AreEqual(0, q.Count);
            Assert.IsTrue(q.IsEmpty);
            Assert.IsFalse(q.IsFull);
            Assert.AreEqual(BannerQueue.DefaultCapacity, q.Capacity);
        }

        [Test]
        public void Enqueue_BelowCapacity_Accepts()
        {
            var q = new BannerQueue();
            for (int i = 0; i < BannerQueue.DefaultCapacity; i++)
            {
                Assert.IsTrue(q.TryEnqueue(Banner(GuidanceSeverity.Info, $"i{i}")));
            }
            Assert.AreEqual(BannerQueue.DefaultCapacity, q.Count);
            Assert.IsTrue(q.IsFull);
        }

        [Test]
        public void Dequeue_ReturnsItemsInFifoOrder()
        {
            var q = new BannerQueue();
            q.TryEnqueue(Banner(GuidanceSeverity.Info, "a"));
            q.TryEnqueue(Banner(GuidanceSeverity.Info, "b"));
            q.TryEnqueue(Banner(GuidanceSeverity.Info, "c"));

            Assert.IsTrue(q.TryDequeue(out var first));
            Assert.AreEqual("a", first.SourceTipId);
            Assert.IsTrue(q.TryDequeue(out var second));
            Assert.AreEqual("b", second.SourceTipId);
            Assert.IsTrue(q.TryDequeue(out var third));
            Assert.AreEqual("c", third.SourceTipId);

            Assert.IsFalse(q.TryDequeue(out _));
        }

        [Test]
        public void Peek_DoesNotConsume()
        {
            var q = new BannerQueue();
            q.TryEnqueue(Banner(GuidanceSeverity.Info, "x"));

            Assert.IsTrue(q.TryPeek(out var peeked));
            Assert.AreEqual("x", peeked.SourceTipId);
            Assert.AreEqual(1, q.Count);
        }

        [Test]
        public void Peek_OnEmpty_ReturnsFalse()
        {
            var q = new BannerQueue();
            Assert.IsFalse(q.TryPeek(out _));
        }

        [Test]
        public void Clear_RemovesAll()
        {
            var q = new BannerQueue();
            q.TryEnqueue(Banner(GuidanceSeverity.Info, "a"));
            q.TryEnqueue(Banner(GuidanceSeverity.Critical, "b"));
            q.Clear();
            Assert.AreEqual(0, q.Count);
            Assert.IsTrue(q.IsEmpty);
        }

        // ===============================================================
        // SEVERITY-BASED EVICTION
        // ===============================================================

        [Test]
        public void EnqueueAtCapacity_HigherSeverity_EvictsOldestOfLowest()
        {
            var q = new BannerQueue(capacity: 3);
            q.TryEnqueue(Banner(GuidanceSeverity.Info, "info-old"));
            q.TryEnqueue(Banner(GuidanceSeverity.Warning, "warn-mid"));
            q.TryEnqueue(Banner(GuidanceSeverity.Info, "info-new"));

            Assert.IsTrue(q.TryEnqueue(Banner(GuidanceSeverity.Critical, "crit")));

            var snapshot = q.Snapshot();
            // info-old was the oldest of the lowest severity; it must be gone.
            Assert.IsFalse(SnapshotContains(snapshot, "info-old"));
            Assert.IsTrue(SnapshotContains(snapshot, "warn-mid"));
            Assert.IsTrue(SnapshotContains(snapshot, "info-new"));
            Assert.IsTrue(SnapshotContains(snapshot, "crit"));
            Assert.AreEqual(3, q.Count);
        }

        [Test]
        public void EnqueueAtCapacity_EqualSeverityToLowest_IsDropped()
        {
            var q = new BannerQueue(capacity: 3);
            q.TryEnqueue(Banner(GuidanceSeverity.Warning, "w1"));
            q.TryEnqueue(Banner(GuidanceSeverity.Warning, "w2"));
            q.TryEnqueue(Banner(GuidanceSeverity.Warning, "w3"));

            Assert.IsFalse(q.TryEnqueue(Banner(GuidanceSeverity.Warning, "w4")));
            Assert.AreEqual(3, q.Count);
            Assert.IsFalse(SnapshotContains(q.Snapshot(), "w4"));
        }

        [Test]
        public void EnqueueAtCapacity_LowerSeverity_IsDropped()
        {
            var q = new BannerQueue(capacity: 3);
            q.TryEnqueue(Banner(GuidanceSeverity.Critical, "c1"));
            q.TryEnqueue(Banner(GuidanceSeverity.Critical, "c2"));
            q.TryEnqueue(Banner(GuidanceSeverity.Critical, "c3"));

            Assert.IsFalse(q.TryEnqueue(Banner(GuidanceSeverity.Info, "info-doomed")));
            Assert.AreEqual(3, q.Count);
            Assert.IsFalse(SnapshotContains(q.Snapshot(), "info-doomed"));
        }

        [Test]
        public void Eviction_PrefersOldestAmongLowestSeverity()
        {
            var q = new BannerQueue(capacity: 4);
            q.TryEnqueue(Banner(GuidanceSeverity.Info, "info-1"));
            q.TryEnqueue(Banner(GuidanceSeverity.Warning, "warn-1"));
            q.TryEnqueue(Banner(GuidanceSeverity.Info, "info-2"));
            q.TryEnqueue(Banner(GuidanceSeverity.Warning, "warn-2"));

            Assert.IsTrue(q.TryEnqueue(Banner(GuidanceSeverity.Alert, "alert")));

            var snap = q.Snapshot();
            Assert.IsFalse(SnapshotContains(snap, "info-1"));
            Assert.IsTrue(SnapshotContains(snap, "info-2"));
            Assert.IsTrue(SnapshotContains(snap, "warn-1"));
            Assert.IsTrue(SnapshotContains(snap, "warn-2"));
            Assert.IsTrue(SnapshotContains(snap, "alert"));
        }

        [Test]
        public void EightInfoThenOneCritical_CriticalSurvivesOldestInfoEvicted()
        {
            var q = new BannerQueue();
            for (int i = 0; i < BannerQueue.DefaultCapacity; i++)
            {
                Assert.IsTrue(q.TryEnqueue(Banner(GuidanceSeverity.Info, $"info-{i}")));
            }

            Assert.IsTrue(q.TryEnqueue(Banner(GuidanceSeverity.Critical, "crit")));

            var snap = q.Snapshot();
            Assert.IsTrue(SnapshotContains(snap, "crit"));
            Assert.IsFalse(SnapshotContains(snap, "info-0"));
            for (int i = 1; i < BannerQueue.DefaultCapacity; i++)
            {
                Assert.IsTrue(SnapshotContains(snap, $"info-{i}"));
            }
        }

        // ===============================================================
        // PROPERTY-STYLE: RANDOM SEQUENCES, ASSERT INVARIANTS
        // ===============================================================

        [Test]
        public void Property_RandomSequence_NeverExceedsCapacity()
        {
            var rng = new System.Random(42);
            for (int trial = 0; trial < 50; trial++)
            {
                var q = new BannerQueue();
                int eventsToFire = rng.Next(20, 100);
                for (int i = 0; i < eventsToFire; i++)
                {
                    var sev = (GuidanceSeverity)rng.Next(0, 5);
                    q.TryEnqueue(Banner(sev, $"r{trial}-{i}"));
                    Assert.LessOrEqual(q.Count, q.Capacity, $"trial {trial} step {i}");
                }
            }
        }

        [Test]
        public void Property_HighestSeverityEventsAlwaysSurviveLowerSeverityFlood()
        {
            var rng = new System.Random(1234);
            for (int trial = 0; trial < 50; trial++)
            {
                var q = new BannerQueue();
                // Inject a Critical first, then flood with Info.
                q.TryEnqueue(Banner(GuidanceSeverity.Critical, "the-critical"));
                int floodCount = rng.Next(10, 30);
                for (int i = 0; i < floodCount; i++)
                {
                    q.TryEnqueue(Banner(GuidanceSeverity.Info, $"flood-{i}"));
                }
                Assert.IsTrue(SnapshotContains(q.Snapshot(), "the-critical"),
                    $"Critical evicted by Info flood in trial {trial}");
            }
        }

        [Test]
        public void Property_DequeueOrder_PreservesEnqueueOrderAmongSurvivors()
        {
            var rng = new System.Random(7);
            for (int trial = 0; trial < 30; trial++)
            {
                var q = new BannerQueue();
                var enqueueLog = new List<string>();
                int events = rng.Next(5, 20);
                for (int i = 0; i < events; i++)
                {
                    var sev = (GuidanceSeverity)rng.Next(0, 5);
                    var id = $"t{trial}-e{i}";
                    if (q.TryEnqueue(Banner(sev, id))) enqueueLog.Add(id);
                }

                // Walk what's in the queue now and find each ID's position in the enqueue log.
                var snapshot = q.Snapshot();
                int lastFoundIndex = -1;
                foreach (var item in snapshot)
                {
                    int idx = enqueueLog.IndexOf(item.SourceTipId);
                    Assert.GreaterOrEqual(idx, 0);
                    Assert.Greater(idx, lastFoundIndex,
                        $"Order broken in trial {trial}: items not in original enqueue order");
                    lastFoundIndex = idx;
                }
            }
        }

        // ===============================================================
        // EDGE CASES
        // ===============================================================

        [Test]
        public void Capacity_OfOne_OperatesCorrectly()
        {
            var q = new BannerQueue(capacity: 1);
            Assert.IsTrue(q.TryEnqueue(Banner(GuidanceSeverity.Info, "a")));
            Assert.IsFalse(q.TryEnqueue(Banner(GuidanceSeverity.Info, "b"))); // equal severity dropped
            Assert.IsTrue(q.TryEnqueue(Banner(GuidanceSeverity.Critical, "c"))); // higher evicts
            Assert.IsTrue(q.TryDequeue(out var only));
            Assert.AreEqual("c", only.SourceTipId);
        }

        [Test]
        public void DequeueAfterEvictionFlow_ReturnsRemainingInOrder()
        {
            var q = new BannerQueue(capacity: 3);
            q.TryEnqueue(Banner(GuidanceSeverity.Info, "info-doomed"));
            q.TryEnqueue(Banner(GuidanceSeverity.Warning, "w1"));
            q.TryEnqueue(Banner(GuidanceSeverity.Warning, "w2"));
            q.TryEnqueue(Banner(GuidanceSeverity.Critical, "crit")); // evicts info-doomed

            Assert.IsTrue(q.TryDequeue(out var a));
            Assert.AreEqual("w1", a.SourceTipId);
            Assert.IsTrue(q.TryDequeue(out var b));
            Assert.AreEqual("w2", b.SourceTipId);
            Assert.IsTrue(q.TryDequeue(out var c));
            Assert.AreEqual("crit", c.SourceTipId);
            Assert.IsFalse(q.TryDequeue(out _));
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private static bool SnapshotContains(IReadOnlyList<GuidanceBannerRequest> snapshot, string sourceTipId)
        {
            foreach (var item in snapshot)
            {
                if (item.SourceTipId == sourceTipId) return true;
            }
            return false;
        }
    }
}
