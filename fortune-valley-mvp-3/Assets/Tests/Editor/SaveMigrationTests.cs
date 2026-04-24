using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// End-to-end save migration from the pre-change schema (no
    /// schema_version field, legacy accumulated/ready_amount/full_day_amount
    /// fields on PendingIncomeEntryDTO) to the new daily-locked schema.
    ///
    /// Validates JsonUtility's drop-unknown-keys behavior and the hydrate
    /// migration path (StartNewDay relock for every restored bucket).
    /// </summary>
    [TestFixture]
    public class SaveMigrationTests
    {
        private GameObject _rootGO;
        private PendingIncomeService _service;
        private FakeLotRegistry _lots;
        private FakeTickClock _clock;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _rootGO = new GameObject("MigrationTestRoot");
            _service = _rootGO.AddComponent<PendingIncomeService>();
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
        public void LegacyJson_Hydrate_RelocksEveryBucketToFreshDay()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _lots.RegisterLot("lot_B", Owner.Player, 1, 8f);

            // Literal pre-change JSON. Notable:
            // - No schema_version field (JsonUtility fills it with 0).
            // - Legacy fields accumulated/ready_amount/full_day_amount
            //   (unknown to the new DTO; JsonUtility silently drops them).
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

            // Sanity: legacy parse produces zero-valued new fields and schema 0.
            Assert.AreEqual(0, dto.schema_version, "Legacy JSON has no schema_version; must default to 0.");
            Assert.AreEqual(0f, dto.pending_incomes[0].daily_payout);
            Assert.AreEqual(0, dto.pending_incomes[0].ticks_remaining);
            Assert.IsFalse(dto.pending_incomes[0].is_ready);

            _service.Hydrate(dto);

            // Every restored bucket must be relocked to a fresh full day.
            var a = _service.Buckets["lot_A"];
            Assert.AreEqual(50f, a.DailyPayout, "Migration must relock via StartNewDay using current rates.");
            Assert.AreEqual(10, a.TicksRemaining);
            Assert.IsFalse(a.IsReady);

            var b = _service.Buckets["lot_B"];
            Assert.AreEqual(80f, b.DailyPayout);
            Assert.AreEqual(10, b.TicksRemaining);
            Assert.IsFalse(b.IsReady, "Legacy is_ready=true is discarded; migration forfeits in-progress coins.");
        }

        [Test]
        public void Snapshot_AfterMigration_PersistsNewSchema()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);

            // Start from legacy state.
            var legacy = new GamePlayerStateDTO
            {
                schema_version = 0,
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO { building_id = "lot_A" }
                }
            };
            _service.Hydrate(legacy);

            var fresh = new GamePlayerStateDTO();
            _service.Snapshot(fresh);

            Assert.AreEqual(1, fresh.schema_version, "Snapshot must bump schema_version to 1.");
            Assert.AreEqual(1, fresh.pending_incomes.Length);
            Assert.AreEqual("lot_A", fresh.pending_incomes[0].building_id);
            Assert.AreEqual(50f, fresh.pending_incomes[0].daily_payout);
            Assert.AreEqual(10, fresh.pending_incomes[0].ticks_remaining);
            Assert.IsFalse(fresh.pending_incomes[0].is_ready);
        }
    }
}
