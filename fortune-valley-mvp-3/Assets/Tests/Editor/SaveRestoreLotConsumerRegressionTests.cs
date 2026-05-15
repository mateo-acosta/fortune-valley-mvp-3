using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Issue 12A regression coverage: consumers that previously relied on
    /// OnLotOwnershipChanged for state init must remain correct now that
    /// RaiseAllOwnedLotEvents (Phase 2 catch-up) only emits LotPurchased +
    /// LotTierChanged. DailyIncomeAccumulator restores its buckets via its
    /// own Hydrate path (Phase 1), so the test confirms the combined flow
    /// (Hydrate then RaiseAllOwnedLotEvents) produces correct buckets.
    /// </summary>
    [TestFixture]
    public class SaveRestoreLotConsumerRegressionTests
    {
        private GameObject _rootGO;
        private DailyIncomeAccumulator _accumulator;
        private FakeLotRegistry _lots;
        private FakeTickClock _clock;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _rootGO = new GameObject("RegressionFx");
            _accumulator = _rootGO.AddComponent<DailyIncomeAccumulator>();
            _lots = new FakeLotRegistry();
            _clock = new FakeTickClock { TicksPerDay = 10 };
            _accumulator.Initialize(_lots, _clock);
            InvokePrivate(_accumulator, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            if (_accumulator != null) InvokePrivate(_accumulator, "OnDisable");
            Object.DestroyImmediate(_rootGO);
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void DailyIncomeAccumulator_AfterHydrateAndRaiseAllOwnedLotEvents_PreservesBuckets()
        {
            // Stage saved state: a player-owned lot with pending income.
            _lots.RegisterLot("lot_A", Owner.Player, 2, 8f);
            _lots.SetStarterLotId("lot_A");

            var dto = new GamePlayerStateDTO
            {
                schema_version = 1,
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO
                    {
                        building_id = "lot_A",
                        daily_payout = 42f,
                        ticks_remaining = 3,
                        is_ready = false
                    }
                }
            };

            // Phase 1: accumulator hydrates buckets directly.
            _accumulator.Hydrate(dto);
            Assert.IsTrue(_accumulator.Accumulators.ContainsKey("lot_A"));
            Assert.AreEqual(42f, _accumulator.Accumulators["lot_A"].DailyPayout);

            // Phase 2: RaiseAllOwnedLotEvents fires LotPurchased + LotTierChanged.
            // These must NOT clobber the hydrated state: the LotTierChanged handler
            // just marks rates dirty (cosmetic), and LotPurchased is currently a
            // no-op for the accumulator (bucket already exists).
            GameEvents.RaiseLotPurchased("lot_A", Owner.Player);
            GameEvents.RaiseLotTierChanged("lot_A", 2);

            Assert.IsTrue(_accumulator.Accumulators.ContainsKey("lot_A"),
                "Bucket must survive Phase 2 re-emission");
            Assert.AreEqual(42f, _accumulator.Accumulators["lot_A"].DailyPayout,
                "Hydrated payout must not be reset by Phase 2 events");
        }

        [Test]
        public void DailyIncomeAccumulator_LotPurchasedAlone_DoesNotCreateBucket()
        {
            // Explicit confirmation of the design decision: the accumulator's
            // bucket lifecycle is driven by OnLotOwnershipChanged (live) or
            // Hydrate (save restore). LotPurchased alone is NOT a bucket-creation
            // signal -- if Phase 2 catch-up fires LotPurchased without prior
            // Hydrate, no bucket appears. This is the assumption that justifies
            // not firing OnLotOwnershipChanged in RaiseAllOwnedLotEvents.
            _lots.RegisterLot("lot_B", Owner.Player, 1, 5f);

            GameEvents.RaiseLotPurchased("lot_B", Owner.Player);

            Assert.IsFalse(_accumulator.Accumulators.ContainsKey("lot_B"),
                "LotPurchased alone does not create accumulator bucket; "
                + "Phase 1 Hydrate is the canonical restore path");
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var m = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (m != null) m.Invoke(target, null);
        }
    }
}
