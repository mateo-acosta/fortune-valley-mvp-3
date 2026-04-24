using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for RestaurantSystem, including intent event handling.
    /// </summary>
    [TestFixture]
    public class RestaurantSystemTests
    {
        private GameObject _rootGO;
        private RestaurantSystem _system;
        private CurrencyManager _currency;
        private RestaurantConfig _config;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();

            _rootGO = new GameObject("TestRoot");

            _currency = _rootGO.AddComponent<CurrencyManager>();
            SetField(_currency, "_startingCheckingBalance", 10000f);
            _currency.ResetBalance();

            _config = ScriptableObject.CreateInstance<RestaurantConfig>();

            _system = _rootGO.AddComponent<RestaurantSystem>();
            SetField(_system, "_config", _config);
            SetField(_system, "_currencyManager", _currency);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rootGO);
            Object.DestroyImmediate(_config);
            GameEvents.ClearAllSubscriptions();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            throw new System.Exception($"Field '{fieldName}' not found on {target.GetType().Name}");
        }

        [Test]
        public void RaiseUpgradeRestaurantRequested_UpgradesRestaurant()
        {
            // Re-subscribe after SetUp's ClearAllSubscriptions wiped OnEnable's subscription
            var onEnable = typeof(RestaurantSystem).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnable.Invoke(_system, null);

            int levelBefore = _system.CurrentLevel;

            GameEvents.RaiseUpgradeRestaurantRequested();

            Assert.AreEqual(levelBefore + 1, _system.CurrentLevel,
                "RestaurantSystem should upgrade when OnUpgradeRestaurantRequested fires");

            var onDisable = typeof(RestaurantSystem).GetMethod("OnDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisable.Invoke(_system, null);
        }

        [Test]
        public void RaiseUpgradeRestaurantRequested_NoFunds_DoesNotUpgrade()
        {
            var onEnable = typeof(RestaurantSystem).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnable.Invoke(_system, null);

            _currency.SetCheckingBalance(0f);
            int levelBefore = _system.CurrentLevel;

            GameEvents.RaiseUpgradeRestaurantRequested();

            Assert.AreEqual(levelBefore, _system.CurrentLevel,
                "RestaurantSystem should not upgrade when player cannot afford it");

            var onDisable = typeof(RestaurantSystem).GetMethod("OnDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisable.Invoke(_system, null);
        }

        [Test]
        public void OnTick_NoLongerMutatesPendingBuckets()
        {
            // Post-redesign: RestaurantSystem does not subscribe to OnTick.
            // PendingIncomeService owns the daily-locked coin cycle. A tick
            // here must not touch any pending-income state; balances, totals,
            // and external state all stay put.
            float balanceBefore = _currency.CheckingBalance;
            float earnedBefore = _system.TotalEarned;

            GameEvents.RaiseTick(1);

            Assert.AreEqual(balanceBefore, _currency.CheckingBalance);
            Assert.AreEqual(earnedBefore, _system.TotalEarned);
        }

        [Test]
        public void OnIncomeCollected_IncrementsTotalEarned()
        {
            // Re-subscribe OnEnable after SetUp cleared events.
            var onEnable = typeof(RestaurantSystem).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnable.Invoke(_system, null);

            float before = _system.TotalEarned;
            GameEvents.RaiseIncomeCollected("lot_A", 77f);
            Assert.AreEqual(before + 77f, _system.TotalEarned, 0.01f);

            GameEvents.RaiseIncomeCollected("restaurant", 23f);
            Assert.AreEqual(before + 100f, _system.TotalEarned, 0.01f);

            var onDisable = typeof(RestaurantSystem).GetMethod("OnDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisable.Invoke(_system, null);
        }
    }
}
