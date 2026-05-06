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
    /// Runtime tests for CityManager.
    /// Tests lot ownership and win/lose conditions.
    /// </summary>
    [TestFixture]
    public class CityManagerTests
    {
        private GameObject _testObject;
        private CityManager _cityManager;
        private CurrencyManager _currencyManager;
        private List<CityLotDefinition> _testLots;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();

            _testObject = new GameObject("TestCityManager");

            // Set up currency manager
            _currencyManager = _testObject.AddComponent<CurrencyManager>();
            SetPrivateField(_currencyManager, "_startingCheckingBalance", 10000f);
            _currencyManager.ResetBalance();

            // Create test lots
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

            // Set up city manager
            _cityManager = _testObject.AddComponent<CityManager>();
            SetPrivateField(_cityManager, "_allLots", _testLots);
            SetPrivateField(_cityManager, "_currencyManager", _currencyManager);

            // Simulate game start
            GameEvents.RaiseGameStart();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var lot in _testLots)
            {
                Object.DestroyImmediate(lot);
            }
            Object.Destroy(_testObject);
            GameEvents.ClearAllSubscriptions();
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        // ═══════════════════════════════════════════════════════════════
        // INITIAL STATE TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void InitialState_AllLotsUnowned()
        {
            Assert.AreEqual(0, _cityManager.PlayerLotCount);
            Assert.AreEqual(0, _cityManager.RivalLotCount);
            Assert.AreEqual(3, _cityManager.AvailableLotCount);
        }

        [Test]
        public void GetOwner_UnownedLot_ReturnsNone()
        {
            Assert.AreEqual(Owner.None, _cityManager.GetOwner("lot_0"));
        }

        // ═══════════════════════════════════════════════════════════════
        // PLAYER PURCHASE TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void TryPurchaseLot_WithFunds_Succeeds()
        {
            bool result = _cityManager.TryPurchaseLot("lot_0", 0);

            Assert.IsTrue(result);
            Assert.AreEqual(Owner.Player, _cityManager.GetOwner("lot_0"));
            Assert.AreEqual(1, _cityManager.PlayerLotCount);
        }

        [Test]
        public void TryPurchaseLot_SpendsMoney()
        {
            float before = _currencyManager.CheckingBalance;
            _cityManager.TryPurchaseLot("lot_0", 0);
            float after = _currencyManager.CheckingBalance;

            Assert.AreEqual(1000f, before - after); // lot_0 costs 1000
        }

        [Test]
        public void TryPurchaseLot_WithoutFunds_Fails()
        {
            _currencyManager.SetCheckingBalance(500f); // Not enough for any lot

            bool result = _cityManager.TryPurchaseLot("lot_0", 0);

            Assert.IsFalse(result);
            Assert.AreEqual(Owner.None, _cityManager.GetOwner("lot_0"));
        }

        [Test]
        public void TryPurchaseLot_AlreadyOwned_Fails()
        {
            _cityManager.TryPurchaseLot("lot_0", 0);
            float balanceAfterFirst = _currencyManager.CheckingBalance;

            bool result = _cityManager.TryPurchaseLot("lot_0", 1);

            Assert.IsFalse(result);
            Assert.AreEqual(balanceAfterFirst, _currencyManager.CheckingBalance);
        }

        // ═══════════════════════════════════════════════════════════════
        // RIVAL PURCHASE TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void RivalPurchaseLot_Succeeds()
        {
            bool result = _cityManager.RivalPurchaseLot("lot_1", 0);

            Assert.IsTrue(result);
            Assert.AreEqual(Owner.Rival, _cityManager.GetOwner("lot_1"));
            Assert.AreEqual(1, _cityManager.RivalLotCount);
        }

        [Test]
        public void RivalPurchaseLot_DoesNotAffectPlayerMoney()
        {
            float before = _currencyManager.CheckingBalance;
            _cityManager.RivalPurchaseLot("lot_1", 0);
            float after = _currencyManager.CheckingBalance;

            Assert.AreEqual(before, after);
        }

        // ═══════════════════════════════════════════════════════════════
        // WIN-CONDITION TESTS REMOVED IN LIFE GOALS REVISION
        // The CheckWinCondition method was deleted; the hard end is now
        // retirement (age 65) via LifespanController + RetirementEvaluator.
        // Bankruptcy is a soft mid-life reset.
        // ═══════════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════════
        // EVENT TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void TryPurchaseLot_FiresLotPurchasedEvent()
        {
            string receivedLotId = "";
            Owner receivedOwner = Owner.None;
            GameEvents.OnLotPurchased += (lotId, owner) =>
            {
                receivedLotId = lotId;
                receivedOwner = owner;
            };

            _cityManager.TryPurchaseLot("lot_0", 0);

            Assert.AreEqual("lot_0", receivedLotId);
            Assert.AreEqual(Owner.Player, receivedOwner);
        }

        [Test]
        public void WinCondition_FiresGameEndEvent()
        {
            Owner receivedWinner = Owner.None;
            GameEvents.OnGameEnd += (winner) => receivedWinner = winner;

            _cityManager.TryPurchaseLot("lot_0", 0);
            _cityManager.TryPurchaseLot("lot_1", 1);
            _cityManager.TryPurchaseLot("lot_2", 2);

            Assert.AreEqual(Owner.Player, receivedWinner);
        }

        // ═══════════════════════════════════════════════════════════════
        // AVAILABLE LOTS TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void GetAvailableLots_ReturnsUnownedLots()
        {
            _cityManager.TryPurchaseLot("lot_0", 0);

            var available = _cityManager.GetAvailableLots();

            Assert.AreEqual(2, available.Count);
            Assert.IsFalse(available.Exists(l => l.LotId == "lot_0"));
        }

        // ═══════════════════════════════════════════════════════════════
        // GAME PROGRESS TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void GetGameProgress_ReturnsCorrectRatio()
        {
            Assert.AreEqual(0f, _cityManager.GetGameProgress());

            _cityManager.TryPurchaseLot("lot_0", 0);
            Assert.AreEqual(1f / 3f, _cityManager.GetGameProgress(), 0.01f);

            _cityManager.RivalPurchaseLot("lot_1", 1);
            Assert.AreEqual(2f / 3f, _cityManager.GetGameProgress(), 0.01f);
        }

        // ═══════════════════════════════════════════════════════════════
        // INTENT EVENT HANDLER TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void RaisePurchaseLotRequested_PurchasesLot()
        {
            // CityManager is a runtime test -- OnEnable fires automatically,
            // so it is already subscribed to OnPurchaseLotRequested.
            GameEvents.RaisePurchaseLotRequested("lot_0", 5);

            Assert.AreEqual(Owner.Player, _cityManager.GetOwner("lot_0"),
                "CityManager should purchase lot when OnPurchaseLotRequested fires");
        }
    }
}
