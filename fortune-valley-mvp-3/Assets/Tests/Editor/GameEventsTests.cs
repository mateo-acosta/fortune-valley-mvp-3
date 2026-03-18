using NUnit.Framework;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

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

            GameEvents.RaiseLotPurchased("lot_corner", Owner.Player);

            Assert.AreEqual("lot_corner", receivedLotId);
            Assert.AreEqual(Owner.Player, receivedOwner);
        }

        [Test]
        public void OnRivalTargetingLot_ReceivesLotId()
        {
            string receivedLotId = "";
            GameEvents.OnRivalTargetingLot += (lotId) => receivedLotId = lotId;

            GameEvents.RaiseRivalTargetingLot("lot_hotel");

            Assert.AreEqual("lot_hotel", receivedLotId);
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
    }
}
