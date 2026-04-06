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
    }
}
