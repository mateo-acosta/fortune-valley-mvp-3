using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// End-to-end scenarios for the daily-locked coin model. Real MonoBehaviour
    /// wiring (TimeManager, CityManager, RestaurantSystem, PendingIncomeService,
    /// IncomeCollectionController) so each test mirrors production flow.
    /// </summary>
    [TestFixture]
    public class TapToCollectIntegrationTests
    {
        private const int TicksPerDay = 10;

        private GameObject _go;
        private TimeManager _time;
        private CurrencyManager _currency;
        private RestaurantConfig _config;
        private RestaurantSystem _restaurant;
        private CityManager _city;
        private PendingIncomeService _pending;
        private IncomeCollectionController _controller;
        private CityLotDefinition _playerStarter;
        private CityLotDefinition _rivalStarter;
        private CityLotDefinition _extraLot;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _go = new GameObject("IntegrationTest");

            _config = ScriptableObject.CreateInstance<RestaurantConfig>();
            SetField(_config, "_baseIncomePerTick", 10f);
            SetField(_config, "_maxLevel", 3);
            SetField(_config, "_upgradeCosts", new float[] { 500f, 1500f });
            SetField(_config, "_incomeMultipliers", new float[] { 1f, 2f, 4f });

            _playerStarter = MakeLot("starter_player", 500f, 5f);
            _rivalStarter = MakeLot("starter_rival", 500f, 5f);
            _extraLot = MakeLot("lot_extra", 800f, 8f);

            _time = _go.AddComponent<TimeManager>();
            SetField(_time, "_ticksPerDay", TicksPerDay);

            _currency = _go.AddComponent<CurrencyManager>();
            SetField(_currency, "_startingCheckingBalance", 10000f);
            _currency.ResetBalance();

            _city = _go.AddComponent<CityManager>();
            SetField(_city, "_allLots", new List<CityLotDefinition> { _playerStarter, _rivalStarter, _extraLot });
            SetField(_city, "_currencyManager", _currency);
            SetField(_city, "_playerStarterLot", _playerStarter);
            SetField(_city, "_rivalStarterLot", _rivalStarter);
            SetField(_city, "_tierOnStart", 2);
            SetField(_city, "_currency", _currency);

            _pending = _go.AddComponent<PendingIncomeService>();
            SetField(_pending, "_cityManager", _city);
            SetField(_pending, "_timeManager", _time);

            _restaurant = _go.AddComponent<RestaurantSystem>();
            SetField(_restaurant, "_config", _config);
            SetField(_restaurant, "_currencyManager", _currency);
            SetField(_restaurant, "_cityManager", _city);
            SetField(_pending, "_restaurantSystem", _restaurant);

            _controller = _go.AddComponent<IncomeCollectionController>();
            SetField(_controller, "_currencyManager", _currency);
            SetField(_controller, "_pendingIncome", _pending);

            // AddComponent may have already auto-fired OnEnable in PlayMode.
            // Disable and re-enable to guarantee exactly one subscription.
            InvokePrivate(_city, "OnDisable");
            InvokePrivate(_pending, "OnDisable");
            InvokePrivate(_restaurant, "OnDisable");
            InvokePrivate(_controller, "OnDisable");
            InvokePrivate(_city, "OnEnable");
            InvokePrivate(_pending, "OnEnable");
            InvokePrivate(_restaurant, "OnEnable");
            InvokePrivate(_controller, "OnEnable");

            GameEvents.RaiseGameStart();
            InvokePrivate(_city, "HandleGameStart");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_playerStarter);
            Object.DestroyImmediate(_rivalStarter);
            Object.DestroyImmediate(_extraLot);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_go);
            GameEvents.ClearAllSubscriptions();
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 1: full day cycle — lock -> drain -> ready -> collect
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void FullDayCycle_LocksThenDrainsThenReadyThenCollects()
        {
            var starter = _pending.Buckets["starter_player"];
            // starter lot folds the restaurant base: (5 + 10) * 10 = 150
            Assert.AreEqual(150f, starter.DailyPayout, 0.01f);
            Assert.AreEqual(TicksPerDay, starter.TicksRemaining);
            Assert.IsFalse(starter.IsReady);

            for (int i = 1; i <= TicksPerDay; i++) GameEvents.RaiseTick(i);

            starter = _pending.Buckets["starter_player"];
            Assert.IsTrue(starter.IsReady);
            Assert.AreEqual(0, starter.TicksRemaining);

            float before = _currency.CheckingBalance;
            GameEvents.RaiseIncomeCollectRequested("starter_player", CollectReason.PlayerTap);
            Assert.AreEqual(before + 150f, _currency.CheckingBalance, 0.01f);

            // Collect should relock tomorrow's day automatically.
            starter = _pending.Buckets["starter_player"];
            Assert.IsFalse(starter.IsReady);
            Assert.AreEqual(150f, starter.DailyPayout, 0.01f);
            Assert.AreEqual(TicksPerDay, starter.TicksRemaining);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 2: production cap — ready bucket stops advancing
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void ProductionCap_NoProductionWhileReady()
        {
            for (int i = 1; i <= TicksPerDay; i++) GameEvents.RaiseTick(i);
            Assert.IsTrue(_pending.Buckets["starter_player"].IsReady);

            // Three more days' worth of ticks.
            for (int i = TicksPerDay + 1; i <= TicksPerDay * 4; i++) GameEvents.RaiseTick(i);

            Assert.IsTrue(_pending.Buckets["starter_player"].IsReady);
            Assert.AreEqual(0, _pending.Buckets["starter_player"].TicksRemaining);
            Assert.AreEqual(150f, _pending.Buckets["starter_player"].DailyPayout, 0.01f);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 3: mid-day restaurant upgrade — today unchanged, tomorrow reflects
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void MidDayRestaurantUpgrade_TodaysCoinStaysAtOldRate_TomorrowMatchesNew()
        {
            Assert.AreEqual(150f, _pending.Buckets["starter_player"].DailyPayout, 0.01f);

            for (int i = 1; i <= 3; i++) GameEvents.RaiseTick(i);
            Assert.IsTrue(_restaurant.TryUpgrade(), "Expected restaurant upgrade to succeed.");
            // Today's coin still locked at pre-upgrade rate.
            Assert.AreEqual(150f, _pending.Buckets["starter_player"].DailyPayout, 0.01f);

            for (int i = 4; i <= TicksPerDay; i++) GameEvents.RaiseTick(i);
            GameEvents.RaiseIncomeCollectRequested("starter_player", CollectReason.PlayerTap);

            // After collect, tomorrow locks at new rate: (5 + 20) * 10 = 250.
            Assert.AreEqual(250f, _pending.Buckets["starter_player"].DailyPayout, 0.01f);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 4: mid-day lot tier upgrade — today unchanged, tomorrow reflects
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void MidDayLotTierUpgrade_TodaysCoinStaysAtOldRate_TomorrowMatchesNew()
        {
            // Buy an extra lot (T1 after purchase). Rate at T1: 8 * 0.5 = 4/tick -> 40/day.
            Assert.IsTrue(_city.TryPurchaseLot("lot_extra", _time.CurrentTick));
            Assert.AreEqual(40f, _pending.Buckets["lot_extra"].DailyPayout, 0.01f);

            // Mid-day: upgrade to T2. New rate = 8/tick -> 80/day.
            for (int i = 1; i <= 4; i++) GameEvents.RaiseTick(i);
            Assert.IsTrue(_city.TryUpgradeLot("lot_extra"));
            Assert.AreEqual(40f, _pending.Buckets["lot_extra"].DailyPayout, 0.01f);

            for (int i = 5; i <= TicksPerDay; i++) GameEvents.RaiseTick(i);
            GameEvents.RaiseIncomeCollectRequested("lot_extra", CollectReason.PlayerTap);

            Assert.AreEqual(80f, _pending.Buckets["lot_extra"].DailyPayout, 0.01f);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 5: save/reload preserves mid-drain state
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void SaveMidDrain_ReloadPreservesTicksRemainingAndDailyPayout()
        {
            for (int i = 1; i <= 4; i++) GameEvents.RaiseTick(i);

            var dto = new GamePlayerStateDTO();
            _pending.Snapshot(dto);

            // Simulate fresh load: hydrate from the captured dto.
            _pending.Hydrate(dto);

            var starter = _pending.Buckets["starter_player"];
            Assert.AreEqual(150f, starter.DailyPayout, 0.01f);
            Assert.AreEqual(TicksPerDay - 4, starter.TicksRemaining);
            Assert.IsFalse(starter.IsReady);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 6: save/reload preserves ready coin
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void SaveReady_ReloadPreservesReadyCoin()
        {
            for (int i = 1; i <= TicksPerDay; i++) GameEvents.RaiseTick(i);
            Assert.IsTrue(_pending.Buckets["starter_player"].IsReady);

            var dto = new GamePlayerStateDTO();
            _pending.Snapshot(dto);
            _pending.Hydrate(dto);

            Assert.IsTrue(_pending.Buckets["starter_player"].IsReady);
            Assert.AreEqual(150f, _pending.Buckets["starter_player"].DailyPayout, 0.01f);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 7: rival takeover mid-drain forfeits
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void RivalTakeoverMidDrain_ForfeitsNoPayout()
        {
            Assert.IsTrue(_city.TryPurchaseLot("lot_extra", _time.CurrentTick));
            for (int i = 1; i <= 3; i++) GameEvents.RaiseTick(i);
            float before = _currency.CheckingBalance;

            SimulateOwnershipLoss("lot_extra");

            Assert.AreEqual(before, _currency.CheckingBalance,
                "Mid-drain ownership loss forfeits the pending coin.");
            Assert.IsFalse(_pending.Buckets.ContainsKey("lot_extra"));
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 8: rival takeover while ready pays out
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void RivalTakeoverWhileReady_PaysOutViaOwnershipLostPath()
        {
            Assert.IsTrue(_city.TryPurchaseLot("lot_extra", _time.CurrentTick));
            for (int i = 1; i <= TicksPerDay; i++) GameEvents.RaiseTick(i);
            Assert.IsTrue(_pending.Buckets["lot_extra"].IsReady);

            float before = _currency.CheckingBalance;
            SimulateOwnershipLoss("lot_extra");

            Assert.Greater(_currency.CheckingBalance, before,
                "Ready coin must pay out before the bucket is removed.");
            Assert.IsFalse(_pending.Buckets.ContainsKey("lot_extra"));
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 9: starter lot owned -> no restaurant bucket
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void StarterLotOwned_RestaurantBucketAbsent()
        {
            Assert.IsTrue(_pending.Buckets.ContainsKey("starter_player"));
            Assert.IsFalse(_pending.Buckets.ContainsKey(PendingIncomeService.RestaurantBuildingId),
                "While the starter is player-owned, it folds restaurant base income.");
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 10: starter lost -> restaurant bucket spawns
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void StarterLotLostToRival_RestaurantBucketCreated_ResumesOwnBase()
        {
            SimulateOwnershipLoss("starter_player");

            Assert.IsFalse(_pending.Buckets.ContainsKey("starter_player"));
            Assert.IsTrue(_pending.Buckets.ContainsKey(PendingIncomeService.RestaurantBuildingId),
                "When starter leaves player ownership, the restaurant bucket takes over.");
            // Restaurant-only payout = 10/tick * 10 ticks = 100.
            Assert.AreEqual(100f, _pending.Buckets[PendingIncomeService.RestaurantBuildingId].DailyPayout, 0.01f);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 11: legacy save migration
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void LegacySaveMigration_AllBucketsReset()
        {
            // Build a legacy-shaped DTO: no schema_version, zeroed new fields.
            var legacy = new GamePlayerStateDTO
            {
                schema_version = 0,
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO { building_id = "starter_player" }
                }
            };
            _pending.Hydrate(legacy);

            var starter = _pending.Buckets["starter_player"];
            Assert.AreEqual(150f, starter.DailyPayout, 0.01f);
            Assert.AreEqual(TicksPerDay, starter.TicksRemaining);
            Assert.IsFalse(starter.IsReady);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 12: buying a new lot creates a fresh draining bucket
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void PlayerBuysNewLot_FreshBucketStartsDraining_NotPreReady()
        {
            Assert.IsFalse(_pending.Buckets.ContainsKey("lot_extra"));
            Assert.IsTrue(_city.TryPurchaseLot("lot_extra", _time.CurrentTick));

            var bucket = _pending.Buckets["lot_extra"];
            Assert.AreEqual(40f, bucket.DailyPayout, 0.01f); // T1: 8 * 0.5 * 10
            Assert.AreEqual(TicksPerDay, bucket.TicksRemaining);
            Assert.IsFalse(bucket.IsReady, "Fresh bucket must drain from full, not arrive pre-ready.");
        }

        // ═══════════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Simulates a rival/None takeover of a player-owned lot. CityManager's
        /// RivalPurchaseLot rejects on owned lots, so tests drive the state
        /// change directly: mutate the ownership dictionary via reflection
        /// and raise OnLotOwnershipChanged so the service can react.
        /// </summary>
        private void SimulateOwnershipLoss(string lotId)
        {
            var ownership = (Dictionary<string, Owner>)typeof(CityManager)
                .GetField("_lotOwnership", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(_city);
            ownership[lotId] = Owner.Rival;
            GameEvents.RaiseLotOwnershipChanged(lotId, Owner.Player, Owner.Rival);
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            var t = target.GetType();
            while (t != null)
            {
                var m = t.GetMethod(methodName,
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (m != null) { m.Invoke(target, args); return; }
                t = t.BaseType;
            }
        }

        private static CityLotDefinition MakeLot(string id, float baseCost, float income)
        {
            var lot = ScriptableObject.CreateInstance<CityLotDefinition>();
            SetField(lot, "_lotId", id);
            SetField(lot, "_displayName", id);
            SetField(lot, "_baseCost", baseCost);
            SetField(lot, "_incomeBonus", income);
            SetField(lot, "_tier2UpgradeCost", 500f);
            SetField(lot, "_tier3UpgradeCost", 1500f);
            return lot;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (field != null) { field.SetValue(target, value); return; }
                type = type.BaseType;
            }
            throw new System.Exception($"Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }
}
