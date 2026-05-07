using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Integration tests that verify multiple systems working together.
    /// These tests simulate actual gameplay scenarios.
    /// </summary>
    [TestFixture]
    public class IntegrationTests
    {
        private GameObject _gameObject;
        private TimeManager _timeManager;
        private CurrencyManager _currencyManager;
        private RestaurantSystem _restaurantSystem;
        private InvestmentSystem _investmentSystem;
        private CityManager _cityManager;
        private DailyIncomeAccumulator _pendingIncome;
        private IncomeCollectionController _collectionController;

        private RestaurantConfig _restaurantConfig;
        private List<InvestmentDefinition> _investmentDefs;
        private List<CityLotDefinition> _lotDefs;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _gameObject = new GameObject("IntegrationTest");

            // Create configs
            CreateConfigs();

            // Create managers
            _timeManager = _gameObject.AddComponent<TimeManager>();
            _currencyManager = _gameObject.AddComponent<CurrencyManager>();
            _restaurantSystem = _gameObject.AddComponent<RestaurantSystem>();
            _investmentSystem = _gameObject.AddComponent<InvestmentSystem>();
            _cityManager = _gameObject.AddComponent<CityManager>();
            _pendingIncome = _gameObject.AddComponent<DailyIncomeAccumulator>();
            _collectionController = _gameObject.AddComponent<IncomeCollectionController>();

            // Wire dependencies
            SetPrivateField(_currencyManager, "_startingCheckingBalance", 1000f);
            SetPrivateField(_restaurantSystem, "_config", _restaurantConfig);
            SetPrivateField(_restaurantSystem, "_currencyManager", _currencyManager);
            SetPrivateField(_restaurantSystem, "_cityManager", _cityManager);
            SetPrivateField(_restaurantSystem, "_pendingIncome", _pendingIncome);
            SetPrivateField(_investmentSystem, "_currencyManager", _currencyManager);
            SetPrivateField(_investmentSystem, "_timeManager", _timeManager);
            SetPrivateField(_investmentSystem, "_availableInvestments", _investmentDefs);
            SetPrivateField(_cityManager, "_allLots", _lotDefs);
            SetPrivateField(_cityManager, "_currencyManager", _currencyManager);
            SetPrivateField(_pendingIncome, "_cityManager", _cityManager);
            SetPrivateField(_pendingIncome, "_restaurantSystem", _restaurantSystem);
            SetPrivateField(_pendingIncome, "_timeManager", _timeManager);
            SetPrivateField(_collectionController, "_currencyManager", _currencyManager);
            SetPrivateField(_collectionController, "_pendingIncome", _pendingIncome);

            // Start game
            GameEvents.RaiseGameStart();
        }

        /// <summary>
        /// Simulates one day's worth of ticks then locks + collects the
        /// restaurant bucket, depositing a day's income into checking.
        /// Matches the tap-to-collect behavior the player experiences.
        /// </summary>
        private void SimulateDayAndCollect()
        {
            int ticks = _timeManager.EnginePulsesPerTick;
            for (int i = 1; i <= ticks; i++) GameEvents.RaiseTick(i);
            GameEvents.RaiseDayEnd(1);
            GameEvents.RaiseIncomeCollectRequested(DailyIncomeAccumulator.RestaurantBuildingId, CollectReason.PlayerTap);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_restaurantConfig);
            foreach (var inv in _investmentDefs)
                Object.DestroyImmediate(inv);
            foreach (var lot in _lotDefs)
                Object.DestroyImmediate(lot);
            Object.Destroy(_gameObject);
            GameEvents.ClearAllSubscriptions();
        }

        private void CreateConfigs()
        {
            // Restaurant config
            _restaurantConfig = ScriptableObject.CreateInstance<RestaurantConfig>();
            SetPrivateField(_restaurantConfig, "_baseIncomePerTick", 10f);
            SetPrivateField(_restaurantConfig, "_maxLevel", 3);
            SetPrivateField(_restaurantConfig, "_upgradeCosts", new float[] { 500f, 1500f });
            SetPrivateField(_restaurantConfig, "_incomeMultipliers", new float[] { 1f, 2f, 4f });

            // Investment definitions
            _investmentDefs = new List<InvestmentDefinition>();

            var savings = ScriptableObject.CreateInstance<InvestmentDefinition>();
            SetPrivateField(savings, "_displayName", "Savings");
            SetPrivateField(savings, "_riskLevel", RiskLevel.Low);
            SetPrivateField(savings, "_annualReturnRate", 0.05f);
            SetPrivateField(savings, "_compoundingFrequency", 30);
            SetPrivateField(savings, "_compoundsPerYear", 12);
            SetPrivateField(savings, "_minimumDeposit", 100f);
            SetPrivateField(savings, "_volatilityRange", new Vector2(1f, 1f));
            _investmentDefs.Add(savings);

            // Lot definitions
            _lotDefs = new List<CityLotDefinition>();
            var lot = ScriptableObject.CreateInstance<CityLotDefinition>();
            SetPrivateField(lot, "_lotId", "test_lot");
            SetPrivateField(lot, "_displayName", "Test Lot");
            SetPrivateField(lot, "_baseCost", 2000f);
            SetPrivateField(lot, "_incomeBonus", 5f);
            _lotDefs.Add(lot);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private void SimulateTicks(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                GameEvents.RaiseTick(i);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // RESTAURANT INCOME FLOW
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void RestaurantGeneratesIncomeOverTime()
        {
            // Under tap-to-collect, income accumulates during the day and is
            // deposited on collect. This helper runs a full day + collect.
            float startBalance = _currencyManager.CheckingBalance;

            SimulateDayAndCollect();

            // Restaurant earns 10/tick * ticksPerDay ticks per day.
            float expected = startBalance + 10f * _timeManager.EnginePulsesPerTick;
            Assert.AreEqual(expected, _currencyManager.CheckingBalance, 0.1f);
        }

        [Test]
        public void RestaurantUpgrade_IncreasesIncome()
        {
            // Earn enough for upgrade
            SimulateTicks(50); // 500 earned
            float balanceBeforeUpgrade = _currencyManager.CheckingBalance;

            bool upgraded = _restaurantSystem.TryUpgrade();

            Assert.IsTrue(upgraded);
            Assert.AreEqual(2, _restaurantSystem.CurrentLevel);
            Assert.AreEqual(20f, _restaurantSystem.IncomePerTick); // 2x multiplier
        }

        // ═══════════════════════════════════════════════════════════════
        // INVESTMENT FLOW
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void InvestmentCreation_ReducesCheckingBalance()
        {
            // Buying deducts from checking directly
            float before = _currencyManager.CheckingBalance;

            var investment = _investmentSystem.CreateInvestment(_investmentDefs[0], 500f);

            Assert.IsNotNull(investment);
            Assert.Less(_currencyManager.CheckingBalance, before);
        }

        [Test]
        public void InvestmentWithdrawal_ReturnsValueToChecking()
        {
            // Buy from checking directly
            var investment = _investmentSystem.CreateInvestment(_investmentDefs[0], 500f);
            float checkingAfterBuy = _currencyManager.CheckingBalance;

            float payout = _investmentSystem.WithdrawInvestment(investment);

            Assert.Greater(payout, 0f);
            // Sale proceeds go to checking
            Assert.Greater(_currencyManager.CheckingBalance, checkingAfterBuy);
        }

        // ═══════════════════════════════════════════════════════════════
        // END-TO-END SCENARIO: SAVE AND BUY LOT
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Scenario_SaveRestaurantIncomeAndBuyLot()
        {
            // Start with 1000, lot costs 2000. Under tap-to-collect, a day's
            // cap is one day's income (10 * ticksPerDay), so the player must
            // collect each day to keep accumulating toward the lot cost.
            int ticksPerDay = _timeManager.EnginePulsesPerTick;
            int daysNeeded = Mathf.CeilToInt(1000f / (10f * ticksPerDay)) + 1;

            for (int d = 0; d < daysNeeded; d++) SimulateDayAndCollect();

            // After enough collected days, the player can afford the lot.
            Assert.GreaterOrEqual(_currencyManager.CheckingBalance, 2000f,
                "Daily collects should accumulate enough checking balance to afford the lot.");
        }

        [Test]
        public void Scenario_InvestThenWithdrawToBuyLot()
        {
            // Buy directly from checking
            var investment = _investmentSystem.CreateInvestment(_investmentDefs[0], 800f);
            Assert.AreEqual(200f, _currencyManager.CheckingBalance, 1f);

            // Simulate time + daily collects so restaurant income compounds into checking.
            int ticksPerDay = _timeManager.EnginePulsesPerTick;
            int totalTicks = 360;
            int days = totalTicks / ticksPerDay;
            int tick = 0;
            for (int d = 0; d < days; d++)
            {
                for (int i = 0; i < ticksPerDay; i++)
                {
                    tick++;
                    investment.IncrementTicksHeld();
                    investment.TryCompound(tick);
                    GameEvents.RaiseTick(tick);
                }
                GameEvents.RaiseDayEnd(d + 1);
                GameEvents.RaiseIncomeCollectRequested(DailyIncomeAccumulator.RestaurantBuildingId, CollectReason.PlayerTap);
            }

            // Withdraw investment (payout goes directly to checking)
            float payout = _investmentSystem.WithdrawInvestment(investment);
            Assert.Greater(payout, 800f);

            // Checking now has: starting 200 + daily-collected restaurant income + sale payout,
            // enough to afford the test lot's 2000 cost.
            Assert.Greater(_currencyManager.CheckingBalance, 2000f);
        }

        // ═══════════════════════════════════════════════════════════════
        // LEARNING OUTCOME VERIFICATION
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void LearningOutcome_CompoundInterestVisibleInGains()
        {
            var investment = _investmentSystem.CreateInvestment(_investmentDefs[0], 1000f);

            // Simulate one compound period
            for (int i = 1; i <= 30; i++)
            {
                investment.IncrementTicksHeld();
            }
            investment.TryCompound(30);
            float gainAfterOneCompound = investment.TotalGain;

            // Simulate another compound period
            for (int i = 31; i <= 60; i++)
            {
                investment.IncrementTicksHeld();
            }
            investment.TryCompound(60);
            float gainAfterSecondCompound = investment.TotalGain - gainAfterOneCompound;

            // Second period should earn more (compounding on larger base)
            Assert.Greater(gainAfterSecondCompound, gainAfterOneCompound * 0.99f);
        }

        [Test]
        public void LearningOutcome_InvestmentExplanationIsReadable()
        {
            var investment = _investmentSystem.CreateInvestment(_investmentDefs[0], 500f);

            // Before compounding
            string explanation = investment.GetPerformanceExplanation();
            Assert.IsTrue(explanation.Contains("hasn't compounded"));

            // After compounding
            for (int i = 1; i <= 30; i++)
            {
                investment.IncrementTicksHeld();
            }
            investment.TryCompound(30);
            explanation = investment.GetPerformanceExplanation();

            Assert.IsTrue(explanation.Contains("gained") || explanation.Contains("lost"));
            Assert.IsTrue(explanation.Contains("compound"));
        }
    }
}
