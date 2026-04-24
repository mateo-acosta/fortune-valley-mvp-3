using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class PendingIncomeServiceTests
    {
        private GameObject _rootGO;
        private PendingIncomeService _service;
        private FakeLotRegistry _lots;
        private FakeTickClock _clock;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _rootGO = new GameObject("TestRoot");
            _service = _rootGO.AddComponent<PendingIncomeService>();
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
        // StartNewDay + HandleTick drain semantics
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void StartNewDay_LocksCurrentRate_AsDailyPayout()
        {
            _lots.RegisterLot("lot_A", owner: Owner.Player, tier: 1, perTickAtTier1: 5f);

            _service.EnsureBucket("lot_A");

            var b = _service.Buckets["lot_A"];
            Assert.AreEqual(50f, b.DailyPayout); // 5/tick * 10 ticks
            Assert.AreEqual(10, b.TicksRemaining);
            Assert.IsFalse(b.IsReady);
        }

        [Test]
        public void HandleTick_DecrementsTicksRemaining_UntilReady()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            for (int i = 0; i < 9; i++) GameEvents.RaiseTick(i + 1);
            Assert.AreEqual(1, _service.Buckets["lot_A"].TicksRemaining);
            Assert.IsFalse(_service.Buckets["lot_A"].IsReady);

            GameEvents.RaiseTick(10);
            Assert.AreEqual(0, _service.Buckets["lot_A"].TicksRemaining);
            Assert.IsTrue(_service.Buckets["lot_A"].IsReady);
        }

        [Test]
        public void ProductionCaps_NoProductionWhileReady()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            for (int i = 0; i < 10; i++) GameEvents.RaiseTick(i + 1);
            Assert.IsTrue(_service.Buckets["lot_A"].IsReady);
            float payoutAtReady = _service.Buckets["lot_A"].DailyPayout;

            // Let 3 more "days" pass without collecting.
            for (int i = 0; i < 30; i++) GameEvents.RaiseTick(i + 11);

            Assert.IsTrue(_service.Buckets["lot_A"].IsReady);
            Assert.AreEqual(payoutAtReady, _service.Buckets["lot_A"].DailyPayout);
            Assert.AreEqual(0, _service.Buckets["lot_A"].TicksRemaining);
        }

        // ═══════════════════════════════════════════════════════════════
        // TryCollect
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void TryCollect_ReadyBucket_ReturnsDailyPayout_AndStartsNewDay()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            for (int i = 0; i < 10; i++) GameEvents.RaiseTick(i + 1);

            bool ok = _service.TryCollect("lot_A", out float amount);

            Assert.IsTrue(ok);
            Assert.AreEqual(50f, amount);
            var b = _service.Buckets["lot_A"];
            Assert.IsFalse(b.IsReady);
            Assert.AreEqual(50f, b.DailyPayout, "StartNewDay should relock tomorrow's payout.");
            Assert.AreEqual(10, b.TicksRemaining);
        }

        [Test]
        public void TryCollect_NotReady_ReturnsFalse_NoSideEffects()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            GameEvents.RaiseTick(1); // drained 1 tick

            bool ok = _service.TryCollect("lot_A", out float amount);

            Assert.IsFalse(ok);
            Assert.AreEqual(0f, amount);
            Assert.AreEqual(9, _service.Buckets["lot_A"].TicksRemaining);
            Assert.AreEqual(50f, _service.Buckets["lot_A"].DailyPayout);
        }

        [Test]
        public void TryCollect_UnknownId_ReturnsFalseAndWarns()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Unknown buildingId 'ghost'"));
            Assert.IsFalse(_service.TryCollect("ghost", out _));
        }

        // ═══════════════════════════════════════════════════════════════
        // Mid-day upgrade semantics (load-bearing for the whole redesign)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void MidDayUpgrade_DoesNotChangeTodayDailyPayout_NextDayReflectsUpgrade()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            Assert.AreEqual(50f, _service.Buckets["lot_A"].DailyPayout);

            // Mid-day upgrade: new tier pays 15/tick.
            for (int i = 0; i < 3; i++) GameEvents.RaiseTick(i + 1);
            _lots.UpgradeLotTier("lot_A", newTier: 2, perTickAtNewTier: 15f);

            // Today's coin still pays 50.
            Assert.AreEqual(50f, _service.Buckets["lot_A"].DailyPayout);

            // Drain the rest and collect.
            for (int i = 3; i < 10; i++) GameEvents.RaiseTick(i + 1);
            Assert.IsTrue(_service.Buckets["lot_A"].IsReady);
            _service.TryCollect("lot_A", out float amount);
            Assert.AreEqual(50f, amount, "Today's coin pays the pre-upgrade rate.");

            // Tomorrow's coin reflects the upgrade.
            Assert.AreEqual(150f, _service.Buckets["lot_A"].DailyPayout); // 15 * 10
        }

        // ═══════════════════════════════════════════════════════════════
        // Ownership transitions
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void OwnershipLost_WhileReady_PaysOut()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            for (int i = 0; i < 10; i++) GameEvents.RaiseTick(i + 1);
            Assert.IsTrue(_service.Buckets["lot_A"].IsReady);

            string collectId = null;
            CollectReason reason = CollectReason.PlayerTap;
            GameEvents.OnIncomeCollectRequested += (id, r) => { collectId = id; reason = r; };

            _lots.SetOwner("lot_A", Owner.Rival);
            GameEvents.RaiseLotOwnershipChanged("lot_A", Owner.Player, Owner.Rival);

            Assert.AreEqual("lot_A", collectId);
            Assert.AreEqual(CollectReason.OwnershipLost, reason);
            Assert.IsFalse(_service.Buckets.ContainsKey("lot_A"));
        }

        [Test]
        public void OwnershipLost_WhileNotReady_Forfeits()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            GameEvents.RaiseTick(1); // partial drain

            string collectId = null;
            GameEvents.OnIncomeCollectRequested += (id, r) => { collectId = id; };

            _lots.SetOwner("lot_A", Owner.Rival);
            GameEvents.RaiseLotOwnershipChanged("lot_A", Owner.Player, Owner.Rival);

            // Event fires but controller's TryCollect will return false; bucket removed.
            Assert.AreEqual("lot_A", collectId);
            Assert.IsFalse(_service.Buckets.ContainsKey("lot_A"));
        }

        [Test]
        public void PlayerGainsLot_CreatesFreshBucket_StartsDraining()
        {
            _lots.RegisterLot("lot_B", Owner.None, 1, 8f);
            _lots.SetOwner("lot_B", Owner.Player);
            GameEvents.RaiseLotOwnershipChanged("lot_B", Owner.None, Owner.Player);

            Assert.IsTrue(_service.Buckets.ContainsKey("lot_B"));
            Assert.AreEqual(80f, _service.Buckets["lot_B"].DailyPayout);
            Assert.AreEqual(10, _service.Buckets["lot_B"].TicksRemaining);
            Assert.IsFalse(_service.Buckets["lot_B"].IsReady);
        }

        // ═══════════════════════════════════════════════════════════════
        // Edge-case guards
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void StartNewDay_ZeroTicksPerDay_NoOpsWithWarning()
        {
            _clock.TicksPerDay = 0;
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            // EnsureBucket will call StartNewDay internally.
            LogAssert.Expect(LogType.Warning, new Regex("ticksPerDay <= 0"));
            _service.EnsureBucket("lot_A");

            Assert.IsTrue(_service.Buckets.ContainsKey("lot_A"));
            // Untouched by StartNewDay due to guard; default PendingBucket.
            Assert.AreEqual(0f, _service.Buckets["lot_A"].DailyPayout);
            Assert.AreEqual(0, _service.Buckets["lot_A"].TicksRemaining);
            Assert.IsFalse(_service.Buckets["lot_A"].IsReady);
        }

        [Test]
        public void StartNewDay_UnknownBuildingId_NoOpWithWarning()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Unknown buildingId 'ghost'"));
            _service.StartNewDay("ghost");
            Assert.IsFalse(_service.Buckets.ContainsKey("ghost"));
        }

        [Test]
        public void Hydrate_ClampsNegativeTicksRemaining()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);

            var dto = new GamePlayerStateDTO
            {
                schema_version = 1,
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO { building_id = "lot_A", daily_payout = 50f, ticks_remaining = -3, is_ready = false }
                }
            };
            _service.Hydrate(dto);

            Assert.AreEqual(0, _service.Buckets["lot_A"].TicksRemaining);
        }

        [Test]
        public void HandleTick_BucketMutationMidIteration_DoesNotCrash()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _lots.RegisterLot("lot_B", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            _service.EnsureBucket("lot_B");

            // Mutate _buckets mid-tick by removing lot_A from inside a
            // coin-state subscriber (fires during TickDrain's WriteAndRaise).
            bool removed = false;
            GameEvents.OnCoinStateChanged += (id, _, _, _) =>
            {
                if (!removed && id == "lot_A")
                {
                    removed = true;
                    _service.RemoveBucket("lot_A");
                }
            };

            Assert.DoesNotThrow(() => GameEvents.RaiseTick(1));
            Assert.IsTrue(removed);
            Assert.IsFalse(_service.Buckets.ContainsKey("lot_A"));
            Assert.AreEqual(9, _service.Buckets["lot_B"].TicksRemaining);
        }

        // ═══════════════════════════════════════════════════════════════
        // Event emission contracts
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void OnCoinStateChanged_FiresExactlyOncePerStateChange()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            var events = new List<(string id, float payout, float progress, bool ready)>();
            GameEvents.OnCoinStateChanged += (id, p, pr, r) => events.Add((id, p, pr, r));

            _service.EnsureBucket("lot_A"); // StartNewDay fires one event
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("lot_A", events[0].id);
            Assert.AreEqual(50f, events[0].payout);
            Assert.AreEqual(1f, events[0].progress);
            Assert.IsFalse(events[0].ready);

            GameEvents.RaiseTick(1); // one tick -> one event
            Assert.AreEqual(2, events.Count);
            Assert.AreEqual(0.9f, events[1].progress, 0.0001f);
        }

        [Test]
        public void OnIncomePendingQuery_ReEmitsCurrentState()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            (string id, float payout, float progress, bool ready)? captured = null;
            GameEvents.OnCoinStateChanged += (id, p, pr, r) => captured = (id, p, pr, r);

            GameEvents.RaiseIncomePendingQuery("lot_A");

            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual("lot_A", captured.Value.id);
            Assert.AreEqual(50f, captured.Value.payout);
            Assert.AreEqual(1f, captured.Value.progress);
            Assert.IsFalse(captured.Value.ready);
        }

        [Test]
        public void OnIncomePendingQuery_UnknownId_Silent()
        {
            bool emitted = false;
            GameEvents.OnCoinStateChanged += (_, _, _, _) => emitted = true;
            GameEvents.RaiseIncomePendingQuery("ghost");
            Assert.IsFalse(emitted);
        }

        // ═══════════════════════════════════════════════════════════════
        // Restaurant bucket lifecycle vs starter-lot ownership
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void StarterLotOwned_RestaurantBucketNotCreated()
        {
            _lots.SetStarterLotId("lot_Starter");
            _lots.RegisterLot("lot_Starter", Owner.Player, 1, 5f);

            // Fire game start flow.
            GameEvents.RaiseGameStart();

            Assert.IsFalse(
                _service.Buckets.ContainsKey(PendingIncomeService.RestaurantBuildingId),
                "Restaurant bucket must not exist while starter lot is player-owned.");
        }

        [Test]
        public void StarterLotLost_RestaurantBucketCreatedOnOwnershipChange()
        {
            _lots.SetStarterLotId("lot_Starter");
            _lots.RegisterLot("lot_Starter", Owner.Player, 1, 5f);
            GameEvents.RaiseGameStart();
            // Establish starter bucket via ownership event.
            GameEvents.RaiseLotOwnershipChanged("lot_Starter", Owner.None, Owner.Player);
            Assert.IsTrue(_service.Buckets.ContainsKey("lot_Starter"));
            Assert.IsFalse(_service.Buckets.ContainsKey(PendingIncomeService.RestaurantBuildingId));

            // Player loses starter to rival.
            _lots.SetOwner("lot_Starter", Owner.Rival);
            GameEvents.RaiseLotOwnershipChanged("lot_Starter", Owner.Player, Owner.Rival);

            Assert.IsTrue(
                _service.Buckets.ContainsKey(PendingIncomeService.RestaurantBuildingId),
                "Restaurant bucket must spawn when starter lot leaves player ownership.");
        }

        // ═══════════════════════════════════════════════════════════════
        // Snapshot / Hydrate roundtrip
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Snapshot_WritesAllBuckets_BumpsSchemaVersion()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");
            GameEvents.RaiseTick(1); // one drain

            var dto = new GamePlayerStateDTO();
            _service.Snapshot(dto);

            Assert.AreEqual(1, dto.schema_version);
            Assert.AreEqual(1, dto.pending_incomes.Length);
            Assert.AreEqual("lot_A", dto.pending_incomes[0].building_id);
            Assert.AreEqual(50f, dto.pending_incomes[0].daily_payout);
            Assert.AreEqual(9, dto.pending_incomes[0].ticks_remaining);
            Assert.IsFalse(dto.pending_incomes[0].is_ready);
        }

        [Test]
        public void Hydrate_CurrentSchema_RestoresExactState()
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

            var b = _service.Buckets["lot_A"];
            Assert.AreEqual(77f, b.DailyPayout);
            Assert.AreEqual(4, b.TicksRemaining);
            Assert.IsFalse(b.IsReady);
        }

        [Test]
        public void Hydrate_LegacySchema_ReclocksEveryBucketViaStartNewDay()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            var dto = new GamePlayerStateDTO
            {
                schema_version = 0,
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO { building_id = "lot_A" /* other fields zero */ }
                }
            };
            _service.Hydrate(dto);

            var b = _service.Buckets["lot_A"];
            Assert.AreEqual(50f, b.DailyPayout, "Legacy buckets relock via StartNewDay.");
            Assert.AreEqual(10, b.TicksRemaining);
            Assert.IsFalse(b.IsReady);
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

            Assert.IsTrue(_service.Buckets.ContainsKey("lot_A"));
            Assert.IsFalse(_service.Buckets.ContainsKey("lot_ghost"));
        }
    }

}
