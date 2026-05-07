using System.Collections.Generic;
using NUnit.Framework;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class GoalProgressTrackerTests
    {
        private LifeGoalSelectionService _selection;
        private List<LifeGoalEntry> _realizedFires;
        private int _progressFireCount;
        private float _lastProgressNetWorth;
        private float _lastProgressNextThreshold;
        private int _currentDay;
        private List<float> _allRealizedFires;

        [SetUp]
        public void SetUp()
        {
            _selection = new LifeGoalSelectionService();
            _realizedFires = new List<LifeGoalEntry>();
            _progressFireCount = 0;
            _lastProgressNetWorth = 0f;
            _lastProgressNextThreshold = 0f;
            _currentDay = 0;
            _allRealizedFires = new List<float>();

            GameEvents.OnGoalRealized += entry => _realizedFires.Add(entry);
            GameEvents.OnGoalProgressChanged += (cur, prev, next) =>
            {
                _progressFireCount++;
                _lastProgressNetWorth = cur;
                _lastProgressNextThreshold = next;
            };
            GameEvents.OnAllGoalsRealized += threshold => _allRealizedFires.Add(threshold);
        }

        [TearDown]
        public void TearDown()
        {
            _selection?.Dispose();
            GameEvents.ClearAllSubscriptions();
        }

        private static LifeGoalEntry[] StandardTrio()
        {
            return new[]
            {
                new LifeGoalEntry("first_apartment", LifeGoalTier.Starter, 100_000f),
                new LifeGoalEntry("family_home", LifeGoalTier.Mid, 500_000f),
                new LifeGoalEntry("retire_early", LifeGoalTier.Ambitious, 2_000_000f),
            };
        }

        private GoalProgressTracker BuildTracker()
        {
            return new GoalProgressTracker(_selection, () => _currentDay);
        }

        [Test]
        public void Below_Starter_Threshold_NoRealize_ProgressFires()
        {
            _selection.HydrateFromDto(StandardTrio());

            using (var tracker = BuildTracker())
            {
                GameEvents.RaiseNetWorthChanged(50_000f, 50_000f);

                Assert.AreEqual(0, _realizedFires.Count);
                Assert.AreEqual(1, _progressFireCount);
                Assert.AreEqual(100_000f, _lastProgressNextThreshold);
                Assert.AreEqual(50_000f, _lastProgressNetWorth);
            }
        }

        [Test]
        public void At_Starter_Threshold_ExactEquality_Realizes()
        {
            _selection.HydrateFromDto(StandardTrio());

            using (var tracker = BuildTracker())
            {
                GameEvents.RaiseNetWorthChanged(100_000f, 100_000f);

                Assert.AreEqual(1, _realizedFires.Count);
                Assert.AreEqual("first_apartment", _realizedFires[0].goal_id);
            }
        }

        [Test]
        public void Crosses_Two_Thresholds_FiresInAscendingOrder()
        {
            _selection.HydrateFromDto(StandardTrio());

            using (var tracker = BuildTracker())
            {
                _currentDay = 84;
                GameEvents.RaiseNetWorthChanged(750_000f, 750_000f);

                Assert.AreEqual(2, _realizedFires.Count);
                Assert.AreEqual("first_apartment", _realizedFires[0].goal_id);
                Assert.AreEqual("family_home", _realizedFires[1].goal_id);
                Assert.AreEqual(84, _realizedFires[0].realized_at_day);
                Assert.AreEqual(84, _realizedFires[1].realized_at_day);

                // Progress fires ONCE for the remaining unrealized (Ambitious).
                Assert.AreEqual(1, _progressFireCount);
                Assert.AreEqual(2_000_000f, _lastProgressNextThreshold);
            }
        }

        [Test]
        public void Crosses_All_Three_Thresholds_AllRealize_NoProgressFire()
        {
            _selection.HydrateFromDto(StandardTrio());

            using (var tracker = BuildTracker())
            {
                GameEvents.RaiseNetWorthChanged(3_000_000f, 3_000_000f);

                Assert.AreEqual(3, _realizedFires.Count);
                Assert.IsTrue(_selection.CurrentSelection.IsAllRealized());

                // No "next" goal exists once all are realized.
                Assert.AreEqual(0, _progressFireCount);
            }
        }

        [Test]
        public void Sticky_OnceRealized_StaysRealized_WhenNetWorthDrops()
        {
            _selection.HydrateFromDto(StandardTrio());

            using (var tracker = BuildTracker())
            {
                GameEvents.RaiseNetWorthChanged(150_000f, 150_000f); // realizes Starter

                Assert.AreEqual(1, _realizedFires.Count);
                _realizedFires.Clear();
                _progressFireCount = 0;

                // NW drops below the realized threshold
                GameEvents.RaiseNetWorthChanged(40_000f, 40_000f);

                // No new realizes, no un-realize
                Assert.AreEqual(0, _realizedFires.Count);
                Assert.IsTrue(_selection.CurrentSelection.Entries[0].realized,
                    "Starter should still be realized (sticky).");
            }
        }

        [Test]
        public void RealizesFiresOnce_PerGoal_AcrossMultipleEvents()
        {
            _selection.HydrateFromDto(StandardTrio());

            using (var tracker = BuildTracker())
            {
                GameEvents.RaiseNetWorthChanged(150_000f, 150_000f);
                GameEvents.RaiseNetWorthChanged(200_000f, 200_000f);
                GameEvents.RaiseNetWorthChanged(450_000f, 450_000f);

                // Starter should fire once across the three events.
                Assert.AreEqual(1, _realizedFires.Count);
            }
        }

        [Test]
        public void EarlyReturn_WhenAllRealized_NoProgressEventOnSubsequentTicks()
        {
            _selection.HydrateFromDto(StandardTrio());

            using (var tracker = BuildTracker())
            {
                GameEvents.RaiseNetWorthChanged(3_000_000f, 3_000_000f); // all realize
                _realizedFires.Clear();
                _progressFireCount = 0;

                GameEvents.RaiseNetWorthChanged(3_500_000f, 3_500_000f);

                Assert.AreEqual(0, _realizedFires.Count);
                Assert.AreEqual(0, _progressFireCount,
                    "Tracker must early-return when all goals realized.");
            }
        }

        [Test]
        public void NoSelection_DoesNothing()
        {
            // No HydrateFromDto call -> selection is null
            using (var tracker = BuildTracker())
            {
                GameEvents.RaiseNetWorthChanged(500_000f, 500_000f);

                Assert.AreEqual(0, _realizedFires.Count);
                Assert.AreEqual(0, _progressFireCount);
            }
        }

        [Test]
        public void Hydrate_AllAlreadyRealized_FiresAllGoalsRealizedOnce()
        {
            // Save load with all 3 goals already realized -> next NW event must
            // fire OnAllGoalsRealized so the HUD can pin the trophy slider.
            var entries = StandardTrio();
            for (int i = 0; i < entries.Length; i++) entries[i].MarkRealized(10);
            _selection.HydrateFromDto(entries);

            using (var tracker = BuildTracker())
            {
                GameEvents.RaiseNetWorthChanged(2_500_000f, 2_500_000f);

                Assert.AreEqual(1, _allRealizedFires.Count);
                Assert.AreEqual(2_000_000f, _allRealizedFires[0]);
                Assert.AreEqual(0, _realizedFires.Count, "No new realizes -- already realized.");
                Assert.AreEqual(0, _progressFireCount, "No next-unrealized -> no progress event.");
            }
        }

        [Test]
        public void Hydrate_PartiallyRealized_FiresProgressNotAllRealized()
        {
            // Save load with 2 of 3 realized -> tracker drives progress toward
            // the remaining unrealized; OnAllGoalsRealized must NOT fire.
            var entries = StandardTrio();
            entries[0].MarkRealized(5); // Starter
            entries[1].MarkRealized(20); // Mid
            _selection.HydrateFromDto(entries);

            using (var tracker = BuildTracker())
            {
                GameEvents.RaiseNetWorthChanged(750_000f, 750_000f);

                Assert.AreEqual(0, _allRealizedFires.Count);
                Assert.AreEqual(1, _progressFireCount);
                Assert.AreEqual(2_000_000f, _lastProgressNextThreshold);
                Assert.AreEqual(750_000f, _lastProgressNetWorth);
            }
        }

        [Test]
        public void CrossesFinalThreshold_FiresAllGoalsRealizedOnce_ThenIdempotent()
        {
            // Realize Starter + Mid first, then cross Ambitious in a single NW
            // change -> OnGoalRealized for Ambitious fires, OnAllGoalsRealized
            // fires once. Subsequent NW changes do not re-fire.
            _selection.HydrateFromDto(StandardTrio());

            using (var tracker = BuildTracker())
            {
                GameEvents.RaiseNetWorthChanged(750_000f, 750_000f); // Starter + Mid
                _realizedFires.Clear();
                _progressFireCount = 0;
                _allRealizedFires.Clear();

                GameEvents.RaiseNetWorthChanged(2_100_000f, 2_100_000f); // Ambitious

                Assert.AreEqual(1, _realizedFires.Count);
                Assert.AreEqual("retire_early", _realizedFires[0].goal_id);
                Assert.AreEqual(1, _allRealizedFires.Count);
                Assert.AreEqual(2_000_000f, _allRealizedFires[0]);
                Assert.AreEqual(0, _progressFireCount, "No next-unrealized after final cross.");

                _allRealizedFires.Clear();
                GameEvents.RaiseNetWorthChanged(2_500_000f, 2_500_000f);
                GameEvents.RaiseNetWorthChanged(3_000_000f, 3_000_000f);

                Assert.AreEqual(0, _allRealizedFires.Count, "Idempotent: no re-emit on later ticks.");
            }
        }

        [Test]
        public void AllRealized_NetWorthDropsBelowThreshold_NoSpuriousReEmit()
        {
            // Sticky behavior preserved post-realization: NW dropping below the
            // final threshold must not un-realize and must not re-fire any
            // event (no OnGoalRealized, OnGoalProgressChanged, or OnAllGoalsRealized).
            _selection.HydrateFromDto(StandardTrio());

            using (var tracker = BuildTracker())
            {
                GameEvents.RaiseNetWorthChanged(2_500_000f, 2_500_000f); // all realize
                _realizedFires.Clear();
                _progressFireCount = 0;
                _allRealizedFires.Clear();

                GameEvents.RaiseNetWorthChanged(500_000f, 500_000f);
                GameEvents.RaiseNetWorthChanged(0f, 0f);

                Assert.AreEqual(0, _realizedFires.Count);
                Assert.AreEqual(0, _progressFireCount);
                Assert.AreEqual(0, _allRealizedFires.Count);
                Assert.IsTrue(_selection.CurrentSelection.IsAllRealized(), "Goals stay sticky.");
            }
        }
    }
}
