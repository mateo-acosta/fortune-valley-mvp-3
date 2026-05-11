using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// End-to-end save migration coverage. The new automatic-deposit model
    /// preserves the existing pending_incomes DTO contract so legacy saves
    /// hydrate without code changes:
    /// - schema_version 0 (pre-pending-incomes): buckets reset to zero and
    ///   start fresh per-tick accumulation.
    /// - schema_version 1+ with non-zero daily_payout: carry-over preserved
    ///   and deposited alongside fresh accumulation on first day-end.
    /// </summary>
    [TestFixture]
    public class SaveMigrationTests
    {
        private GameObject _rootGO;
        private DailyIncomeAccumulator _service;
        private FakeLotRegistry _lots;
        private FakeTickClock _clock;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _rootGO = new GameObject("MigrationTestRoot");
            _service = _rootGO.AddComponent<DailyIncomeAccumulator>();
            _lots = new FakeLotRegistry();
            _clock = new FakeTickClock { TicksPerDay = 10 };
            _service.Initialize(_lots, _clock);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rootGO);
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void LegacyJson_Hydrate_ResetsBucketsToZero()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _lots.RegisterLot("lot_B", Owner.Player, 1, 8f);

            // Pre-change JSON shape. JsonUtility drops unknown fields silently.
            string legacyJson =
                "{" +
                "\"game_mode\":\"homebase\"," +
                "\"current_day\":3," +
                "\"pending_incomes\":[" +
                    "{\"building_id\":\"lot_A\",\"accumulated\":15.0,\"ready_amount\":0.0,\"full_day_amount\":50.0,\"is_ready\":false}," +
                    "{\"building_id\":\"lot_B\",\"accumulated\":0.0,\"ready_amount\":80.0,\"full_day_amount\":80.0,\"is_ready\":true}" +
                "]" +
                "}";

            var dto = JsonUtility.FromJson<GamePlayerStateDTO>(legacyJson);
            Assert.AreEqual(0, dto.schema_version);

            _service.Hydrate(dto);

            // Legacy schema buckets reset to zero; fresh accumulation begins
            // on subsequent ticks.
            var a = _service.Accumulators["lot_A"];
            Assert.AreEqual(0f, a.DailyPayout);
            Assert.IsFalse(a.IsReady);
            Assert.IsTrue(a.RateDirty);

            var b = _service.Accumulators["lot_B"];
            Assert.AreEqual(0f, b.DailyPayout);
            Assert.IsFalse(b.IsReady,
                "Legacy is_ready=true is discarded; migration forfeits in-progress coins for schema 0.");
        }

        [Test]
        public void CurrentSchemaWithCarriedDailyPayout_Hydrate_PreservesPayout()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);

            var dto = new GamePlayerStateDTO
            {
                schema_version = 1,
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO { building_id = "lot_A", daily_payout = 250f }
                }
            };
            _service.Hydrate(dto);

            // Non-legacy: carried payout preserved exactly so the next day-end
            // can deposit it alongside any fresh accumulation.
            Assert.AreEqual(250f, _service.Accumulators["lot_A"].DailyPayout, 0.001f);
            Assert.IsTrue(_service.Accumulators["lot_A"].RateDirty);
        }

        [Test]
        public void Snapshot_AfterAccumulation_PersistsCurrentSchema()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _service.EnsureBucket("lot_A");

            // Use reflection to invoke OnEnable so OnTick subscription is live.
            var onEnable = typeof(DailyIncomeAccumulator).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnable.Invoke(_service, null);

            for (int i = 0; i < 4; i++) GameEvents.RaiseTick(i + 1);

            var fresh = new GamePlayerStateDTO();
            _service.Snapshot(fresh);

            Assert.AreEqual(1, fresh.schema_version);
            Assert.AreEqual(1, fresh.pending_incomes.Length);
            Assert.AreEqual("lot_A", fresh.pending_incomes[0].building_id);
            Assert.AreEqual(20f, fresh.pending_incomes[0].daily_payout, 0.001f);
        }
    }
}
