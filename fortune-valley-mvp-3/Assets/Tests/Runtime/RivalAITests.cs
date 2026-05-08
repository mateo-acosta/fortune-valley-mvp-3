using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests the RivalAI upgrade-before-buy decision branch added with the
    /// rival handicap pass. Calls AttemptPurchase via reflection so we can
    /// exercise the decision logic without having to spin the tick clock.
    /// </summary>
    [TestFixture]
    public class RivalAITests
    {
        private GameObject _testObject;
        private CityManager _cityManager;
        private CurrencyManager _currencyManager;
        private RivalAI _rivalAI;
        private RivalConfig _rivalConfig;
        private List<CityLotDefinition> _testLots;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();

            _testObject = new GameObject("TestRivalAI");

            _currencyManager = _testObject.AddComponent<CurrencyManager>();
            SetPrivateField(_currencyManager, "_startingCheckingBalance", 10000f);
            _currencyManager.ResetBalance();

            _testLots = new List<CityLotDefinition>();
            for (int i = 0; i < 3; i++)
            {
                var lot = ScriptableObject.CreateInstance<CityLotDefinition>();
                SetPrivateField(lot, "_lotId", $"lot_{i}");
                SetPrivateField(lot, "_displayName", $"Lot {i}");
                SetPrivateField(lot, "_baseCost", 1000f + (i * 500f));
                SetPrivateField(lot, "_incomeBonus", 5f);
                _testLots.Add(lot);
            }

            _cityManager = _testObject.AddComponent<CityManager>();
            SetPrivateField(_cityManager, "_allLots", _testLots);
            SetPrivateField(_cityManager, "_currencyManager", _currencyManager);

            _rivalConfig = ScriptableObject.CreateInstance<RivalConfig>();
            SetPrivateField(_rivalConfig, "_startingMoney", 100000f);
            SetPrivateField(_rivalConfig, "_incomePerTick", 0f);
            SetPrivateField(_rivalConfig, "_purchaseInterval", 400);
            SetPrivateField(_rivalConfig, "_purchaseBuffer", 0f);
            SetPrivateField(_rivalConfig, "_warningTicks", 30);
            SetPrivateField(_rivalConfig, "_scaleByProgress", false);

            _rivalAI = _testObject.AddComponent<RivalAI>();
            SetPrivateField(_rivalAI, "_config", _rivalConfig);
            SetPrivateField(_rivalAI, "_cityManager", _cityManager);

            GameEvents.RaiseGameStart();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var lot in _testLots)
            {
                Object.DestroyImmediate(lot);
            }
            if (_rivalConfig != null)
            {
                Object.DestroyImmediate(_rivalConfig);
            }
            Object.Destroy(_testObject);
            GameEvents.ClearAllSubscriptions();
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private void InvokeAttemptPurchase(int tickNumber)
        {
            var method = typeof(RivalAI).GetMethod("AttemptPurchase",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(_rivalAI, new object[] { tickNumber });
        }

        [Test]
        public void AttemptPurchase_NoOwnedLots_BuysCheapestAffordable()
        {
            InvokeAttemptPurchase(0);

            Assert.AreEqual(Owner.Rival, _cityManager.GetOwner("lot_0"),
                "Should buy cheapest lot (lot_0 at $1000) when none owned");
        }

        [Test]
        public void AttemptPurchase_PrefersUpgradeOverBuy()
        {
            // Pre-seed the rival with one T1 lot.
            _cityManager.RivalPurchaseLot("lot_2", 0);
            Assert.AreEqual(1, _cityManager.GetTier("lot_2"));

            // Now run a decision cycle. With $100k cash and an owned T1 lot,
            // the rival should upgrade rather than buy a new lot.
            InvokeAttemptPurchase(100);

            Assert.AreEqual(2, _cityManager.GetTier("lot_2"),
                "Rival should upgrade owned T1 to T2 instead of buying a new lot");
            Assert.AreEqual(Owner.None, _cityManager.GetOwner("lot_0"),
                "Rival should NOT have bought a new lot when an upgrade was available");
            Assert.AreEqual(Owner.None, _cityManager.GetOwner("lot_1"));
        }

        [Test]
        public void AttemptPurchase_AllOwnedAtT3_FallsBackToBuy()
        {
            _cityManager.RivalPurchaseLot("lot_2", 0);
            _cityManager.TryRivalUpgradeLot("lot_2", out _); // T2
            _cityManager.TryRivalUpgradeLot("lot_2", out _); // T3
            Assert.AreEqual(3, _cityManager.GetTier("lot_2"));

            InvokeAttemptPurchase(100);

            Assert.AreEqual(Owner.Rival, _cityManager.GetOwner("lot_0"),
                "Rival should buy a new lot once all owned are at max tier");
        }

        [Test]
        public void AttemptPurchase_CannotAffordAnything_Skips()
        {
            // Drain the rival via reflection.
            var moneyField = typeof(RivalAI).GetField("_money",
                BindingFlags.NonPublic | BindingFlags.Instance);
            moneyField.SetValue(_rivalAI, 0f);

            InvokeAttemptPurchase(100);

            Assert.AreEqual(Owner.None, _cityManager.GetOwner("lot_0"));
            Assert.AreEqual(Owner.None, _cityManager.GetOwner("lot_1"));
            Assert.AreEqual(Owner.None, _cityManager.GetOwner("lot_2"));
        }

        [Test]
        public void AttemptPurchase_UpgradeDeductsRivalMoney()
        {
            _cityManager.RivalPurchaseLot("lot_2", 0);

            var moneyField = typeof(RivalAI).GetField("_money",
                BindingFlags.NonPublic | BindingFlags.Instance);
            float before = (float)moneyField.GetValue(_rivalAI);

            InvokeAttemptPurchase(100);

            float after = (float)moneyField.GetValue(_rivalAI);
            Assert.AreEqual(_testLots[2].Tier2UpgradeCost, before - after,
                "Rival money should drop by exactly the T2 upgrade cost");
        }
    }
}
