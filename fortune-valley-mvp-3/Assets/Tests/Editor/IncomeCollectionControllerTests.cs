using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class IncomeCollectionControllerTests
    {
        private GameObject _rootGO;
        private IncomeCollectionController _controller;
        private CurrencyManager _currency;
        private PendingIncomeService _pending;
        private FakeLotRegistry _lots;
        private FakeTickClock _clock;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _rootGO = new GameObject("TestRoot");

            _currency = _rootGO.AddComponent<CurrencyManager>();
            SetField(_currency, "_startingCheckingBalance", 1000f);
            _currency.ResetBalance();

            _pending = _rootGO.AddComponent<PendingIncomeService>();
            _lots = new FakeLotRegistry();
            _clock = new FakeTickClock { TicksPerDay = 10 };
            _pending.Initialize(_lots, _clock);
            InvokePrivateNoArgs(_pending, "OnEnable");

            _controller = _rootGO.AddComponent<IncomeCollectionController>();
            SetField(_controller, "_currencyManager", _currency);
            SetField(_controller, "_pendingIncome", _pending);
            InvokePrivateNoArgs(_controller, "OnEnable");
        }

        private static void InvokePrivateNoArgs(object target, string methodName)
        {
            var m = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (m != null) m.Invoke(target, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_rootGO);
            GameEvents.ClearAllSubscriptions();
        }

        private void DrainFullDay(string buildingId)
        {
            for (int i = 0; i < _clock.TicksPerDay; i++) GameEvents.RaiseTick(i + 1);
        }

        [Test]
        public void HandleCollect_PlayerTap_Deposits_RaisesFeedbackEvents()
        {
            // Restaurant bucket: ShouldHaveBucket true because starter lot is not set.
            _pending.EnsureBucket(PendingIncomeService.RestaurantBuildingId);
            // Force DailyPayout by reflection since ComputeDayRate needs RestaurantSystem.
            SetBucketState(PendingIncomeService.RestaurantBuildingId, dailyPayout: 100f, ticksRemaining: 0, isReady: true);

            bool incomeEvt = false, collectedEvt = false, saveEvt = false;
            GameEvents.OnIncomeGeneratedWithPosition += (_, __) => incomeEvt = true;
            GameEvents.OnIncomeCollected += (_, __) => collectedEvt = true;
            GameEvents.OnSaveRequested += () => saveEvt = true;

            InvokeHandler("HandleCollectRequested", PendingIncomeService.RestaurantBuildingId, CollectReason.PlayerTap);

            Assert.AreEqual(1100f, _currency.CheckingBalance);
            Assert.IsTrue(incomeEvt);
            Assert.IsTrue(collectedEvt);
            Assert.IsTrue(saveEvt);
        }

        [Test]
        public void HandleCollect_Lot_DepositsExactDailyPayout()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _pending.EnsureBucket("lot_A");
            DrainFullDay("lot_A");

            float totalCollected = 0f;
            GameEvents.OnIncomeCollected += (_, amt) => totalCollected = amt;

            InvokeHandler("HandleCollectRequested", "lot_A", CollectReason.PlayerTap);

            Assert.AreEqual(50f, totalCollected);
            Assert.AreEqual(1050f, _currency.CheckingBalance);
        }

        [Test]
        public void HandleCollect_NotReady_IsNoOp()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _pending.EnsureBucket("lot_A");
            GameEvents.RaiseTick(1); // one tick of drain, not ready

            bool anyEvent = false;
            GameEvents.OnIncomeCollected += (_, __) => anyEvent = true;
            GameEvents.OnIncomeGeneratedWithPosition += (_, __) => anyEvent = true;

            InvokeHandler("HandleCollectRequested", "lot_A", CollectReason.PlayerTap);

            Assert.AreEqual(1000f, _currency.CheckingBalance);
            Assert.IsFalse(anyEvent);
        }

        [Test]
        public void HandleCollect_MissingAnchor_WarnsAndDepositsAtOrigin()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _pending.EnsureBucket("lot_A");
            DrainFullDay("lot_A");

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Missing anchor for 'lot_A'"));

            Vector3 capturedPos = new Vector3(99, 99, 99);
            GameEvents.OnIncomeGeneratedWithPosition += (_, pos) => capturedPos = pos;

            InvokeHandler("HandleCollectRequested", "lot_A", CollectReason.PlayerTap);

            Assert.AreEqual(Vector3.zero, capturedPos);
            Assert.AreEqual(1050f, _currency.CheckingBalance);
        }

        [Test]
        public void HandleCollect_WithRegisteredAnchor_UsesAnchorPosition()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _pending.EnsureBucket("lot_A");
            DrainFullDay("lot_A");

            var anchorGO = new GameObject("Anchor");
            anchorGO.transform.position = new Vector3(10f, 5f, 0f);
            _controller.RegisterAnchor("lot_A", anchorGO.transform);

            Vector3 capturedPos = Vector3.zero;
            GameEvents.OnIncomeGeneratedWithPosition += (_, pos) => capturedPos = pos;

            InvokeHandler("HandleCollectRequested", "lot_A", CollectReason.PlayerTap);

            Assert.AreEqual(new Vector3(10f, 5f, 0f), capturedPos);

            Object.DestroyImmediate(anchorGO);
        }

        [Test]
        public void HandleCollect_OwnershipLost_WhileReady_PaysFinalCoin()
        {
            _lots.RegisterLot("lot_A", Owner.Player, 1, 5f);
            _pending.EnsureBucket("lot_A");
            DrainFullDay("lot_A"); // bucket is now ready with DailyPayout = 50

            bool collected = false;
            GameEvents.OnIncomeCollected += (id, amt) => { collected = id == "lot_A" && amt == 50f; };

            InvokeHandler("HandleCollectRequested", "lot_A", CollectReason.OwnershipLost);

            Assert.IsTrue(collected);
        }

        private void SetBucketState(string id, float dailyPayout, int ticksRemaining, bool isReady)
        {
            var bucketsField = typeof(PendingIncomeService).GetField("_buckets",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var buckets = (System.Collections.Generic.Dictionary<string, PendingBucket>)bucketsField.GetValue(_pending);
            buckets[id] = new PendingBucket
            {
                DailyPayout = dailyPayout,
                TicksRemaining = ticksRemaining,
                IsReady = isReady,
            };
        }

        private void InvokeHandler(string methodName, params object[] args)
        {
            var m = typeof(IncomeCollectionController).GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, $"Handler {methodName} not found");
            m.Invoke(_controller, args);
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
