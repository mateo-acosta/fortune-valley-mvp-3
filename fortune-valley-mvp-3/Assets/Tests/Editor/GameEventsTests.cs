using NUnit.Framework;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;
using FortuneValley.Core;
using UnityEngine;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Unit tests for the GameEvents system.
    /// Verifies event publishing and subscription work correctly.
    /// </summary>
    [TestFixture]
    public class GameEventsTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clear all subscriptions before each test
            GameEvents.ClearAllSubscriptions();
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAllSubscriptions();
        }

        // ═══════════════════════════════════════════════════════════════
        // TICK EVENT TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void OnTick_SubscribersReceiveTick()
        {
            int receivedTick = -1;
            GameEvents.OnTick += (tick) => receivedTick = tick;

            GameEvents.RaiseTick(42);

            Assert.AreEqual(42, receivedTick);
        }

        [Test]
        public void OnTick_MultipleSubscribers_AllReceive()
        {
            int count = 0;
            GameEvents.OnTick += (tick) => count++;
            GameEvents.OnTick += (tick) => count++;
            GameEvents.OnTick += (tick) => count++;

            GameEvents.RaiseTick(1);

            Assert.AreEqual(3, count);
        }

        // ═══════════════════════════════════════════════════════════════
        // CURRENCY EVENT TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void OnCurrencyChanged_ReceivesBalanceAndDelta()
        {
            float receivedBalance = 0f;
            float receivedDelta = 0f;
            GameEvents.OnCurrencyChanged += (balance, delta) =>
            {
                receivedBalance = balance;
                receivedDelta = delta;
            };

            GameEvents.RaiseCurrencyChanged(1500f, 500f);

            Assert.AreEqual(1500f, receivedBalance);
            Assert.AreEqual(500f, receivedDelta);
        }

        [Test]
        public void OnIncomeGenerated_ReceivesAmountAndSource()
        {
            float receivedAmount = 0f;
            string receivedSource = "";
            GameEvents.OnIncomeGenerated += (amount, source) =>
            {
                receivedAmount = amount;
                receivedSource = source;
            };

            GameEvents.RaiseIncomeGenerated(100f, "Restaurant");

            Assert.AreEqual(100f, receivedAmount);
            Assert.AreEqual("Restaurant", receivedSource);
        }

        // ═══════════════════════════════════════════════════════════════
        // LOT PURCHASE EVENT TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void OnLotPurchased_ReceivesLotIdAndOwner()
        {
            string receivedLotId = "";
            Owner receivedOwner = Owner.None;
            GameEvents.OnLotPurchased += (lotId, owner) =>
            {
                receivedLotId = lotId;
                receivedOwner = owner;
            };

            GameEvents.RaiseLotPurchased("lot_block04", Owner.Player);

            Assert.AreEqual("lot_block04", receivedLotId);
            Assert.AreEqual(Owner.Player, receivedOwner);
        }

        [Test]
        public void OnRivalTargetingLot_ReceivesLotId()
        {
            string receivedLotId = "";
            GameEvents.OnRivalTargetingLot += (lotId) => receivedLotId = lotId;

            GameEvents.RaiseRivalTargetingLot("lot_block06");

            Assert.AreEqual("lot_block06", receivedLotId);
        }

        // ═══════════════════════════════════════════════════════════════
        // GAME STATE EVENT TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void OnGameEnd_ReceivesWinner()
        {
            Owner receivedWinner = Owner.None;
            GameEvents.OnGameEnd += (winner) => receivedWinner = winner;

            GameEvents.RaiseGameEnd(Owner.Player);

            Assert.AreEqual(Owner.Player, receivedWinner);
        }

        [Test]
        public void OnGameStart_IsFired()
        {
            bool received = false;
            GameEvents.OnGameStart += () => received = true;

            GameEvents.RaiseGameStart();

            Assert.IsTrue(received);
        }

        // ═══════════════════════════════════════════════════════════════
        // CLEANUP TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void ClearAllSubscriptions_RemovesAllListeners()
        {
            int tickCount = 0;
            GameEvents.OnTick += (tick) => tickCount++;

            GameEvents.ClearAllSubscriptions();
            GameEvents.RaiseTick(1);

            Assert.AreEqual(0, tickCount);
        }

        // ═══════════════════════════════════════════════════════════════
        // INTENT EVENT TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void OnPurchaseLotRequested_ReceivesLotIdAndTick()
        {
            string receivedLotId = "";
            int receivedTick = -1;
            GameEvents.OnPurchaseLotRequested += (lotId, tick) =>
            {
                receivedLotId = lotId;
                receivedTick = tick;
            };

            GameEvents.RaisePurchaseLotRequested("lot_block04", 42);

            Assert.AreEqual("lot_block04", receivedLotId);
            Assert.AreEqual(42, receivedTick);
        }

        [Test]
        public void OnUpgradeRestaurantRequested_Fires()
        {
            bool received = false;
            GameEvents.OnUpgradeRestaurantRequested += () => received = true;

            GameEvents.RaiseUpgradeRestaurantRequested();

            Assert.IsTrue(received);
        }

        [Test]
        public void OnBuySharesRequested_ReceivesDefinitionAndQuantity()
        {
            var testDef = ScriptableObject.CreateInstance<InvestmentDefinition>();
            InvestmentDefinition receivedDef = null;
            int receivedQty = -1;
            GameEvents.OnBuySharesRequested += (def, qty) =>
            {
                receivedDef = def;
                receivedQty = qty;
            };

            GameEvents.RaiseBuySharesRequested(testDef, 5);

            Assert.AreEqual(testDef, receivedDef);
            Assert.AreEqual(5, receivedQty);
            Object.DestroyImmediate(testDef);
        }

        [Test]
        public void OnSellSharesRequested_ReceivesInvestmentAndQuantity()
        {
            var testDef = ScriptableObject.CreateInstance<InvestmentDefinition>();
            var testInv = new ActiveInvestment(testDef, 10, 100f, 0);
            ActiveInvestment receivedInv = null;
            int receivedQty = -1;
            GameEvents.OnSellSharesRequested += (inv, qty) =>
            {
                receivedInv = inv;
                receivedQty = qty;
            };

            GameEvents.RaiseSellSharesRequested(testInv, 3);

            Assert.AreEqual(testInv, receivedInv);
            Assert.AreEqual(3, receivedQty);
            Object.DestroyImmediate(testDef);
        }

        // ═══════════════════════════════════════════════════════════════
        // DAY CYCLE EVENT TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void OnDayEnd_ReceivesDayNumber()
        {
            int receivedDay = -1;
            GameEvents.OnDayEnd += (day) => receivedDay = day;

            GameEvents.RaiseDayEnd(7);

            Assert.AreEqual(7, receivedDay);
        }

        // ═══════════════════════════════════════════════════════════════
        // COMPREHENSIVE CLEAR TESTS
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void ClearAllSubscriptions_ClearsIntentAndDayEvents()
        {
            int callCount = 0;
            var testDef = ScriptableObject.CreateInstance<InvestmentDefinition>();
            var testInv = new ActiveInvestment(testDef, 1, 100f, 0);

            GameEvents.OnPurchaseLotRequested += (_, _) => callCount++;
            GameEvents.OnUpgradeRestaurantRequested += () => callCount++;
            GameEvents.OnBuySharesRequested += (_, _) => callCount++;
            GameEvents.OnSellSharesRequested += (_, _) => callCount++;
            GameEvents.OnDayEnd += (_) => callCount++;

            GameEvents.ClearAllSubscriptions();

            // Raise all events -- none should fire
            GameEvents.RaisePurchaseLotRequested("lot_0", 0);
            GameEvents.RaiseUpgradeRestaurantRequested();
            GameEvents.RaiseBuySharesRequested(testDef, 1);
            GameEvents.RaiseSellSharesRequested(testInv, 1);
            GameEvents.RaiseDayEnd(1);

            Assert.AreEqual(0, callCount, "No callbacks should fire after ClearAllSubscriptions");
            Object.DestroyImmediate(testDef);
        }
    }
}
