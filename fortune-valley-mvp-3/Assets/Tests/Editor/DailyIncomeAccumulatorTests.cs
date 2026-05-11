using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class DailyIncomeAccumulatorTests
    {
        private GameObject _rootGO;
        private DailyIncomeAccumulator _service;
        private FakeLotRegistry _lots;
        private FakeTickClock _clock;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _rootGO = new GameObject("TestRoot");
            _service = _rootGO.AddComponent<DailyIncomeAccumulator>();
            _lots = new FakeLotRegistry();
            _clock = new FakeTickClock { TicksPerDay = 10 };
            _service.Initialize(_lots, _clock);
            // EditMode tests don't fire Unity lifecycle reliably; poke OnEnable.
            InvokePrivate(_service, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            if (_service != null) InvokePrivate(_service, "OnDisable");
            Object.DestroyImmediate(_rootGO);
            GameEvents.ClearAllSubscriptions();
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var m = target.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (m != null) m.Invoke(target, null);
        }

        // ═══════════════════════════════════════════════════════════════
        // EnsureBucket / Per-tick accumulation
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void EnsureBucket_CreatesEmptyAccumulator()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            var a = _service.Accumulators["lot_A"];
            Assert.AreEqual(0f, a.DailyPayout);
            Assert.IsFalse(a.IsReady);
            Assert.IsTrue(a.RateDirty);
        }

        [Test]
        public void HandleTick_AccumulatesPerTickRate()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            for (int i = 0; i < 10; i++) GameEvents.RaiseTick(i + 1);

            // 5/tick * 10 ticks via cached rate = 50
            Assert.AreEqual(50f, _service.Accumulators["lot_A"].DailyPayout, 0.001f);
            Assert.IsFalse(_service.Accumulators["lot_A"].RateDirty);
        }

        [Test]
        public void HandleTick_DoesNotEmitCoinStateChangedPerTick()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            int emitCount = 0;
            GameEvents.OnCoinStateChanged += (_, _, _, _) => emitCount++;

            for (int i = 0; i < 5; i++) GameEvents.RaiseTick(i + 1);

            Assert.AreEqual(0, emitCount,
                "Per-tick label updates were intentionally dropped to avoid TMP rebuild churn.");
        }

        [Test]
        public void HandleTick_AfterUpgrade_UsesNewRateForSubsequentTicks()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            for (int i = 0; i < 4; i++) GameEvents.RaiseTick(i + 1);
            Assert.AreEqual(20f, _service.Accumulators["lot_A"].DailyPayout, 0.001f);

            _lots.UpgradeLotTier("lot_A", 2, perTickAtNewTier: 15f);
            GameEvents.RaiseLotTierChanged("lot_A", 2);

            for (int i = 4; i < 10; i++) GameEvents.RaiseTick(i + 1);

            // 4 ticks @ old rate 5 + 6 ticks @ new rate 15 = 20 + 90 = 110
            Assert.AreEqual(110f, _service.Accumulators["lot_A"].DailyPayout, 0.001f,
                "Mid-day upgrade pro-rates: pre-upgrade ticks pay old rate, post-upgrade ticks pay new rate.");
        }

        // ═══════════════════════════════════════════════════════════════
        // HandleDayEnd
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void HandleDayEnd_RaisesCollectRequestedForEveryNonEmptyBucket()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _lots.RegisterLot("lot_B", Owner.Player, 1, 8f);
            _service.EnsureBucket("lot_A");
            _service.EnsureBucket("lot_B");
            for (int i = 0; i < 10; i++) GameEvents.RaiseTick(i + 1);

            var collected = new List<(string id, CollectReason reason)>();
            GameEvents.OnIncomeCollectRequested += (id, r) => collected.Add((id, r));

            GameEvents.RaiseDayEnd(1);

            Assert.AreEqual(2, collected.Count);
            Assert.AreEqual(CollectReason.DayEnd, collected[0].reason);
            Assert.AreEqual(CollectReason.DayEnd, collected[1].reason);
            CollectionAssert.AreEquivalent(new[] { "lot_A", "lot_B" }, new[] { collected[0].id, collected[1].id });
        }

        [Test]
        public void HandleDayEnd_SkipsZeroPayoutBuckets()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            // No ticks accumulated.

            int requestCount = 0;
            GameEvents.OnIncomeCollectRequested += (_, _) => requestCount++;

            GameEvents.RaiseDayEnd(1);

            Assert.AreEqual(0, requestCount);
        }

        [Test]
        public void HandleDayEnd_MarksReady_SoTryCollectPasses()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            for (int i = 0; i < 10; i++) GameEvents.RaiseTick(i + 1);

            GameEvents.RaiseDayEnd(1);

            // After raising the collect request, the bucket is "ready" (the
            // standard pipeline calls TryCollect; we simulate that here).
            Assert.IsTrue(_service.Accumulators["lot_A"].IsReady);

            bool ok = _service.TryCollect("lot_A", out float amount);
            Assert.IsTrue(ok);
            Assert.AreEqual(50f, amount, 0.001f);
        }

        // ═══════════════════════════════════════════════════════════════
        // TryCollect (no auto-restart under new model)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void TryCollect_DoesNotAutoRestartCountdown()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            for (int i = 0; i < 10; i++) GameEvents.RaiseTick(i + 1);
            GameEvents.RaiseDayEnd(1);

            _service.TryCollect("lot_A", out _);

            // After collect, bucket is empty and waits for next OnDayEnd to repeat.
            // No countdown to restart in the new model.
            Assert.AreEqual(0f, _service.Accumulators["lot_A"].DailyPayout);
            Assert.IsFalse(_service.Accumulators["lot_A"].IsReady);
        }

        [Test]
        public void TryCollect_UnknownId_ReturnsFalseAndWarns()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Unknown buildingId 'ghost'"));
            Assert.IsFalse(_service.TryCollect("ghost", out _));
        }

        [Test]
        public void TryCollect_NotReady_ReturnsFalse_NoSideEffects()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            GameEvents.RaiseTick(1);

            bool ok = _service.TryCollect("lot_A", out float amount);

            Assert.IsFalse(ok);
            Assert.AreEqual(0f, amount);
            Assert.AreEqual(5f, _service.Accumulators["lot_A"].DailyPayout, 0.001f);
        }

        // ═══════════════════════════════════════════════════════════════
        // GetCurrentAccumulated (lazy hover query)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void GetCurrentAccumulated_ReturnsRunningPayout()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            for (int i = 0; i < 3; i++) GameEvents.RaiseTick(i + 1);

            Assert.AreEqual(15f, _service.GetCurrentAccumulated("lot_A"), 0.001f);
        }

        [Test]
        public void GetCurrentAccumulated_UnknownId_ReturnsZero()
        {
            Assert.AreEqual(0f, _service.GetCurrentAccumulated("ghost"));
        }

        // ═══════════════════════════════════════════════════════════════
        // Ownership transitions
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void OwnershipLost_WithAccumulation_PaysOutViaStandardPath()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            for (int i = 0; i < 4; i++) GameEvents.RaiseTick(i + 1);

            string collectId = null;
            CollectReason reason = CollectReason.PlayerTap;
            GameEvents.OnIncomeCollectRequested += (id, r) => { collectId = id; reason = r; };

            _lots.SetOwner("lot_A", Owner.Rival);
            GameEvents.RaiseLotOwnershipChanged("lot_A", Owner.Player, Owner.Rival);

            Assert.AreEqual("lot_A", collectId);
            Assert.AreEqual(CollectReason.OwnershipLost, reason);
            Assert.IsFalse(_service.Accumulators.ContainsKey("lot_A"));
        }

        [Test]
        public void OwnershipLost_WithZeroAccumulation_StillRequestsCollect_ButNoPayout()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            string collectId = null;
            GameEvents.OnIncomeCollectRequested += (id, _) => collectId = id;

            _lots.SetOwner("lot_A", Owner.Rival);
            GameEvents.RaiseLotOwnershipChanged("lot_A", Owner.Player, Owner.Rival);

            Assert.AreEqual("lot_A", collectId);
            Assert.IsFalse(_service.Accumulators.ContainsKey("lot_A"));
        }

        [Test]
        public void PlayerGainsLot_CreatesEmptyBucket_StartsAccumulating()
        {
            _lots.RegisterLot("lot_B", Owner.None, 1, 8f);
            _lots.SetOwner("lot_B", Owner.Player);
            GameEvents.RaiseLotOwnershipChanged("lot_B", Owner.None, Owner.Player);

            Assert.IsTrue(_service.Accumulators.ContainsKey("lot_B"));
            Assert.AreEqual(0f, _service.Accumulators["lot_B"].DailyPayout);

            for (int i = 0; i < 5; i++) GameEvents.RaiseTick(i + 1);
            Assert.AreEqual(40f, _service.Accumulators["lot_B"].DailyPayout, 0.001f);
        }

        // ═══════════════════════════════════════════════════════════════
        // LateUpdate: total daily income (HUD)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void LateUpdate_RaisesTotalDailyIncomeChanged_OnFirstFrame()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            float? captured = null;
            GameEvents.OnTotalDailyIncomeChanged += t => captured = t;

            InvokePrivate(_service, "LateUpdate");

            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual(50f, captured.Value, 0.001f);
        }

        [Test]
        public void LateUpdate_CoalescesMultipleSameFrameDirtyTriggers()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            int emitCount = 0;
            GameEvents.OnTotalDailyIncomeChanged += _ => emitCount++;

            // Fire a flurry of rate-affecting events in the same frame.
            GameEvents.RaiseLotTierChanged("lot_A", 2);
            GameEvents.RaiseRestaurantUpgraded(2);
            GameEvents.RaiseLotTierChanged("lot_A", 3);

            InvokePrivate(_service, "LateUpdate");

            Assert.AreEqual(1, emitCount,
                "Multiple same-frame rate-affecting events must coalesce into one HUD update.");
        }

        [Test]
        public void LateUpdate_SkipsRaise_WhenRoundedTotalUnchanged()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            int emitCount = 0;
            GameEvents.OnTotalDailyIncomeChanged += _ => emitCount++;

            InvokePrivate(_service, "LateUpdate"); // initial emit
            // Mark dirty again; total hasn't actually changed.
            GameEvents.RaiseRestaurantUpgraded(_service != null ? 2 : 0);
            InvokePrivate(_service, "LateUpdate");

            Assert.AreEqual(1, emitCount,
                "Repeat dirty flips must not re-emit if the rounded total is identical.");
        }

        [Test]
        public void LateUpdate_NoBuckets_RaisesZeroOnce()
        {
            float? captured = null;
            GameEvents.OnTotalDailyIncomeChanged += t => captured = t;

            InvokePrivate(_service, "LateUpdate");

            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual(0f, captured.Value);
        }

        // ═══════════════════════════════════════════════════════════════
        // Snapshot / Hydrate roundtrip
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Snapshot_WritesAllBuckets_BumpsSchemaVersion()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            for (int i = 0; i < 4; i++) GameEvents.RaiseTick(i + 1);

            var dto = new GamePlayerStateDTO();
            _service.Snapshot(dto);

            Assert.AreEqual(1, dto.schema_version);
            Assert.AreEqual(1, dto.pending_incomes.Length);
            Assert.AreEqual("lot_A", dto.pending_incomes[0].building_id);
            Assert.AreEqual(20f, dto.pending_incomes[0].daily_payout, 0.001f);
        }

        [Test]
        public void Hydrate_CurrentSchema_RestoresDailyPayout_AndRemarksRateDirty()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            var dto = new GamePlayerStateDTO
            {
                schema_version = 1,
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO { building_id = "lot_A", daily_payout = 77f, ticks_remaining = 4, is_ready = false }
                }
            };
            _service.Hydrate(dto);

            var a = _service.Accumulators["lot_A"];
            Assert.AreEqual(77f, a.DailyPayout, 0.001f);
            Assert.IsTrue(a.RateDirty,
                "Hydrate must remark RateDirty so the per-tick rate recomputes against current world state.");
        }

        [Test]
        public void Hydrate_OrphanLot_DroppedWithWarning()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            var dto = new GamePlayerStateDTO
            {
                schema_version = 1,
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO { building_id = "lot_A", daily_payout = 50f, ticks_remaining = 10 },
                    new PendingIncomeEntryDTO { building_id = "lot_ghost", daily_payout = 7f, ticks_remaining = 5 }
                }
            };

            LogAssert.Expect(LogType.Warning, new Regex("Dropped 1 orphan bucket"));
            _service.Hydrate(dto);

            Assert.IsTrue(_service.Accumulators.ContainsKey("lot_A"));
            Assert.IsFalse(_service.Accumulators.ContainsKey("lot_ghost"));
        }

        // ═══════════════════════════════════════════════════════════════
        // Migration tests (decision 11C)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Hydrate_LegacyDailyPayout_PaidAlongsideNewAccumulationOnFirstDayEnd()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            var dto = new GamePlayerStateDTO
            {
                schema_version = 1,
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO { building_id = "lot_A", daily_payout = 1000f }
                }
            };
            _service.Hydrate(dto);

            // Half a day of new accumulation at 5/tick * 5 ticks = 25.
            for (int i = 0; i < 5; i++) GameEvents.RaiseTick(i + 1);

            float? deposited = null;
            GameEvents.OnIncomeCollectRequested += (id, _) =>
            {
                if (id == "lot_A" && _service.TryCollect(id, out float amount))
                {
                    deposited = amount;
                }
            };

            GameEvents.RaiseDayEnd(1);

            Assert.IsTrue(deposited.HasValue);
            Assert.AreEqual(1025f, deposited.Value, 0.001f,
                "Legacy carry of 1000 + half-day fresh accumulation of 25 should deposit together.");
        }

        [Test]
        public void Hydrate_ZeroLegacyDailyPayout_FreshAccumulationOnly()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            var dto = new GamePlayerStateDTO
            {
                schema_version = 1,
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO { building_id = "lot_A", daily_payout = 0f }
                }
            };
            _service.Hydrate(dto);

            for (int i = 0; i < 10; i++) GameEvents.RaiseTick(i + 1);

            float? deposited = null;
            GameEvents.OnIncomeCollectRequested += (id, _) =>
            {
                if (id == "lot_A" && _service.TryCollect(id, out float amount))
                {
                    deposited = amount;
                }
            };

            GameEvents.RaiseDayEnd(1);

            Assert.IsTrue(deposited.HasValue);
            Assert.AreEqual(50f, deposited.Value, 0.001f);
        }

        // ═══════════════════════════════════════════════════════════════
        // Restaurant bucket lifecycle vs starter-lot ownership
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void StarterLotOwned_RestaurantBucketNotCreated()
        {
            _lots.SetStarterLotId("lot_Starter");
            _lots.RegisterLot("lot_Starter", Owner.Player, 1, 5f);

            GameEvents.RaiseGameStart();

            Assert.IsFalse(
                _service.Accumulators.ContainsKey(DailyIncomeAccumulator.RestaurantBuildingId));
        }

        [Test]
        public void StarterLotLost_RestaurantBucketCreatedOnOwnershipChange()
        {
            _lots.SetStarterLotId("lot_Starter");
            _lots.RegisterLot("lot_Starter", Owner.Player, 1, 5f);
            GameEvents.RaiseGameStart();
            GameEvents.RaiseLotOwnershipChanged("lot_Starter", Owner.None, Owner.Player);
            Assert.IsTrue(_service.Accumulators.ContainsKey("lot_Starter"));
            Assert.IsFalse(_service.Accumulators.ContainsKey(DailyIncomeAccumulator.RestaurantBuildingId));

            _lots.SetOwner("lot_Starter", Owner.Rival);
            GameEvents.RaiseLotOwnershipChanged("lot_Starter", Owner.Player, Owner.Rival);

            Assert.IsTrue(
                _service.Accumulators.ContainsKey(DailyIncomeAccumulator.RestaurantBuildingId));
        }

        [Test]
        public void HandleTick_BucketMutationMidIteration_DoesNotCrash()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _lots.RegisterLot("lot_B", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            _service.EnsureBucket("lot_B");

            bool removed = false;
            GameEvents.OnCoinStateChanged += (id, _, _, _) =>
            {
                if (!removed && id == "lot_A")
                {
                    removed = true;
                    _service.RemoveBucket("lot_A");
                }
            };

            // RemoveBucket emits OnCoinStateChanged synchronously.
            _service.RemoveBucket("lot_A");

            // Per-tick path must not crash even with concurrent dictionary
            // mutations; uses scratch list snapshot.
            Assert.DoesNotThrow(() => GameEvents.RaiseTick(1));
            Assert.IsFalse(_service.Accumulators.ContainsKey("lot_A"));
            Assert.AreEqual(5f, _service.Accumulators["lot_B"].DailyPayout, 0.001f);
        }
    }
}
