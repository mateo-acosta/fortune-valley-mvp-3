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
    /// End-to-end scenarios for the automatic end-of-day deposit model. Real
    /// MonoBehaviour wiring (TimeManager, CityManager, RestaurantSystem,
    /// DailyIncomeAccumulator, IncomeCollectionController) so each test mirrors
    /// production flow.
    /// </summary>
    [TestFixture]
    public class AutoIncomeIntegrationTests
    {
        private const int TicksPerDay = 10;

        private GameObject _go;
        private TimeManager _time;
        private CurrencyManager _currency;
        private RestaurantConfig _config;
        private RestaurantSystem _restaurant;
        private CityManager _city;
        private DailyIncomeAccumulator _pending;
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

            _pending = _go.AddComponent<DailyIncomeAccumulator>();
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
        // Scenario 1: full day cycle accumulate -> day-end deposit
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void FullDayCycle_AccumulatesAndDepositsOnDayEnd()
        {
            float before = _currency.CheckingBalance;
            for (int i = 1; i <= TicksPerDay; i++) GameEvents.RaiseTick(i);

            // Starter lot folds restaurant base: (5 + 10) per tick * 10 ticks = 150.
            Assert.AreEqual(150f, _pending.Accumulators["starter_player"].DailyPayout, 0.01f);

            GameEvents.RaiseDayEnd(1);

            Assert.AreEqual(before + 150f, _currency.CheckingBalance, 0.01f);
            Assert.AreEqual(0f, _pending.Accumulators["starter_player"].DailyPayout);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 2: mid-day restaurant upgrade pro-rates
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void MidDayRestaurantUpgrade_ProRates()
        {
            for (int i = 1; i <= 3; i++) GameEvents.RaiseTick(i);
            float prePayout = _pending.Accumulators["starter_player"].DailyPayout;
            // 3 ticks * 15 (folded rate) = 45
            Assert.AreEqual(45f, prePayout, 0.01f);

            Assert.IsTrue(_restaurant.TryUpgrade());

            for (int i = 4; i <= TicksPerDay; i++) GameEvents.RaiseTick(i);
            // 3 old ticks @ 15 + 7 new ticks @ (5 + 20) = 45 + 175 = 220
            Assert.AreEqual(220f, _pending.Accumulators["starter_player"].DailyPayout, 0.01f);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 3: mid-day lot tier upgrade pro-rates
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void MidDayLotTierUpgrade_ProRates()
        {
            Assert.IsTrue(_city.TryPurchaseLot("lot_extra", _time.CurrentEnginePulse));
            // T1 fresh: 8 * 0.5 = 4/tick
            for (int i = 1; i <= 4; i++) GameEvents.RaiseTick(i);
            Assert.AreEqual(16f, _pending.Accumulators["lot_extra"].DailyPayout, 0.01f);

            Assert.IsTrue(_city.TryUpgradeLot("lot_extra"));
            // T2: 8/tick
            for (int i = 5; i <= TicksPerDay; i++) GameEvents.RaiseTick(i);

            // 4 ticks @ 4 + 6 ticks @ 8 = 16 + 48 = 64
            Assert.AreEqual(64f, _pending.Accumulators["lot_extra"].DailyPayout, 0.01f);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 4: save/reload preserves accumulated DailyPayout
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void SaveMidDay_ReloadPreservesAccumulation()
        {
            for (int i = 1; i <= 4; i++) GameEvents.RaiseTick(i);
            float beforeSave = _pending.Accumulators["starter_player"].DailyPayout;
            Assert.AreEqual(60f, beforeSave, 0.01f);

            var dto = new GamePlayerStateDTO();
            _pending.Snapshot(dto);
            _pending.Hydrate(dto);

            Assert.AreEqual(beforeSave, _pending.Accumulators["starter_player"].DailyPayout, 0.01f);
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 5: rival takeover with no accumulation forfeits cleanly
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void RivalTakeover_WithZeroAccumulation_NoPayoutNoCrash()
        {
            Assert.IsTrue(_city.TryPurchaseLot("lot_extra", _time.CurrentEnginePulse));
            float before = _currency.CheckingBalance;

            SimulateOwnershipLoss("lot_extra");

            Assert.AreEqual(before, _currency.CheckingBalance,
                "Zero-accumulation lot should produce no payout on ownership loss.");
            Assert.IsFalse(_pending.Accumulators.ContainsKey("lot_extra"));
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 6: rival takeover after accumulation pays the running total
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void RivalTakeover_WithAccumulation_PaysOutViaOwnershipLostPath()
        {
            Assert.IsTrue(_city.TryPurchaseLot("lot_extra", _time.CurrentEnginePulse));
            for (int i = 1; i <= 5; i++) GameEvents.RaiseTick(i);
            float before = _currency.CheckingBalance;

            SimulateOwnershipLoss("lot_extra");

            // 5 ticks @ 4/tick (T1 fresh) = 20
            Assert.AreEqual(before + 20f, _currency.CheckingBalance, 0.01f);
            Assert.IsFalse(_pending.Accumulators.ContainsKey("lot_extra"));
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 7: starter lot owned -> no restaurant bucket
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void StarterLotOwned_RestaurantBucketAbsent()
        {
            Assert.IsTrue(_pending.Accumulators.ContainsKey("starter_player"));
            Assert.IsFalse(_pending.Accumulators.ContainsKey(DailyIncomeAccumulator.RestaurantBuildingId));
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 8: starter lost -> restaurant bucket spawns
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void StarterLotLostToRival_RestaurantBucketCreated()
        {
            SimulateOwnershipLoss("starter_player");

            Assert.IsFalse(_pending.Accumulators.ContainsKey("starter_player"));
            Assert.IsTrue(_pending.Accumulators.ContainsKey(DailyIncomeAccumulator.RestaurantBuildingId));
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 9: buying a new lot creates a fresh accumulator at zero
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void PlayerBuysNewLot_FreshAccumulatorStartsAtZero()
        {
            Assert.IsFalse(_pending.Accumulators.ContainsKey("lot_extra"));
            Assert.IsTrue(_city.TryPurchaseLot("lot_extra", _time.CurrentEnginePulse));

            var bucket = _pending.Accumulators["lot_extra"];
            Assert.AreEqual(0f, bucket.DailyPayout,
                "Fresh accumulator starts at zero; income builds per-tick from this point.");
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 10: legacy save migration deposits carried payout +
        // fresh accumulation on first day-end (decision 5A + 11C)
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void LegacySaveMigration_FirstDayEnd_DepositsCarriedPayoutPlusFreshAccumulation()
        {
            // Force a non-zero pending balance into the existing starter bucket.
            var dto = new GamePlayerStateDTO
            {
                schema_version = 1,
                pending_incomes = new[]
                {
                    new PendingIncomeEntryDTO { building_id = "starter_player", daily_payout = 1000f }
                }
            };
            _pending.Hydrate(dto);

            for (int i = 1; i <= 5; i++) GameEvents.RaiseTick(i);
            // 5 ticks @ 15 (folded) = 75
            float expected = 1000f + 75f;

            float before = _currency.CheckingBalance;
            GameEvents.RaiseDayEnd(1);

            Assert.AreEqual(before + expected, _currency.CheckingBalance, 0.01f,
                "Legacy carry plus partial-day fresh accumulation should deposit together at first day-end.");
        }

        // ═══════════════════════════════════════════════════════════════
        // Scenario 11: day-end with multiple buckets fires exactly one
        // OnSaveRequested after AutoSaveController debounce (decision 12A).
        //
        // Note: we count the raw producer-side OnSaveRequested events; the
        // AutoSaveController consumer debounces these into one persistence
        // call. The contract here is that day-end raises N events that
        // collapse into one save downstream.
        // ═══════════════════════════════════════════════════════════════
        [Test]
        public void DayEnd_WithMultipleBuckets_RaisesOneSavePerCollect()
        {
            Assert.IsTrue(_city.TryPurchaseLot("lot_extra", _time.CurrentEnginePulse));
            for (int i = 1; i <= TicksPerDay; i++) GameEvents.RaiseTick(i);

            int saveRequests = 0;
            GameEvents.OnSaveRequested += () => saveRequests++;

            GameEvents.RaiseDayEnd(1);

            // Two buckets with non-zero payout; controller raises save per
            // deposit. AutoSaveController (not exercised here) debounces.
            Assert.AreEqual(2, saveRequests,
                "Producer side fires one save per deposit; consumer-side debounce handled by AutoSaveController.");
        }

        // ═══════════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════════

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
