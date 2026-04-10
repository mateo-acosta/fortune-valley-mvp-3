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

            // Wire dependencies
            SetPrivateField(_currencyManager, "_startingCheckingBalance", 1000f);
            SetPrivateField(_restaurantSystem, "_config", _restaurantConfig);
            SetPrivateField(_restaurantSystem, "_currencyManager", _currencyManager);
            SetPrivateField(_investmentSystem, "_currencyManager", _currencyManager);
            SetPrivateField(_investmentSystem, "_timeManager", _timeManager);
            SetPrivateField(_investmentSystem, "_availableInvestments", _investmentDefs);
            SetPrivateField(_cityManager, "_allLots", _lotDefs);
            SetPrivateField(_cityManager, "_currencyManager", _currencyManager);

            // Start game
            GameEvents.RaiseGameStart();
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
            float startBalance = _currencyManager.CheckingBalance;

            SimulateTicks(10);

            // Should have earned 10 ticks * 10 income = 100
            Assert.AreEqual(startBalance + 100f, _currencyManager.CheckingBalance, 0.1f);
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
            // Start with 1000, lot costs 2000
            // Restaurant earns 10/tick, so need 100 ticks to earn 1000 more

            SimulateTicks(100);

            // Should now have 1000 + 1000 = 2000
            Assert.AreEqual(2000f, _currencyManager.CheckingBalance, 1f);

            // Buy the lot
            bool purchased = _cityManager.TryPurchaseLot("test_lot", 100);

            Assert.IsTrue(purchased);
            Assert.AreEqual(Owner.Player, _cityManager.GetOwner("test_lot"));
            Assert.AreEqual(0f, _currencyManager.CheckingBalance, 1f);
        }

        [Test]
        public void Scenario_InvestThenWithdrawToBuyLot()
        {
            // Buy directly from checking
            var investment = _investmentSystem.CreateInvestment(_investmentDefs[0], 800f);
            Assert.AreEqual(200f, _currencyManager.CheckingBalance, 1f);

            // Simulate time (restaurant income accumulates in checking)
            for (int i = 1; i <= 360; i++)
            {
                investment.IncrementTicksHeld();
                investment.TryCompound(i);
                GameEvents.RaiseTick(i);
            }

            // Withdraw investment (payout goes directly to checking)
            float payout = _investmentSystem.WithdrawInvestment(investment);
            Assert.Greater(payout, 800f);

            // Checking now has: starting 200 + restaurant income + sale payout
            Assert.Greater(_currencyManager.CheckingBalance, 2000f);

            bool purchased = _cityManager.TryPurchaseLot("test_lot", 360);
            Assert.IsTrue(purchased);
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
