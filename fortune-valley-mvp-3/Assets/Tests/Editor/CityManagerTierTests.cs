using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tier / rival-buyout / upgrade coverage for CityManager.
    /// Uses a real CurrencyManager with a preloaded balance; reflection wires serialized fields.
    /// </summary>
    [TestFixture]
    public class CityManagerTierTests
    {
        private GameObject _go;
        private CityManager _city;
        private CurrencyManager _currency;
        private CityLotDefinition _starterPlayer;
        private CityLotDefinition _starterRival;
        private CityLotDefinition _unowned;
        private CityLotDefinition _unowned2;
        private List<CityLotDefinition> _allLots;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();

            _go = new GameObject("TierTest");
            _currency = _go.AddComponent<CurrencyManager>();
            SetField(_currency, "_startingCheckingBalance", 10000f);
            _currency.ResetBalance();

            _starterPlayer = MakeLot("starter_player", 500f, 500f, 1500f, 3f);
            _starterRival = MakeLot("starter_rival", 500f, 500f, 1500f, 3f);
            _unowned = MakeLot("unowned_1", 1000f, 500f, 1500f, 3f);
            _unowned2 = MakeLot("unowned_2", 800f, 400f, 1200f, 3f);

            _allLots = new List<CityLotDefinition> { _starterPlayer, _starterRival, _unowned, _unowned2 };

            _city = _go.AddComponent<CityManager>();
            SetField(_city, "_allLots", _allLots);
            SetField(_city, "_currencyManager", _currency);
            SetField(_city, "_playerStarterLot", _starterPlayer);
            SetField(_city, "_rivalStarterLot", _starterRival);
            // EditMode-safe seam wiring: AddComponent in EditMode does not reliably fire Awake/OnEnable.
            SetField(_city, "_currency", _currency);
            InvokePrivate(_city, "OnEnable");
            // Drive game-start. Direct call guarantees seeding even if event subscription didn't register
            // in this EditMode setup; prevents a whole class of flaky false negatives.
            InvokePrivate(_city, "HandleGameStart");
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var m = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            m?.Invoke(target, null);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var l in _allLots) Object.DestroyImmediate(l);
            Object.DestroyImmediate(_go);
            GameEvents.ClearAllSubscriptions();
        }

        private static CityLotDefinition MakeLot(string id, float baseCost, float t2, float t3, float rivalMult)
        {
            var lot = ScriptableObject.CreateInstance<CityLotDefinition>();
            SetField(lot, "_lotId", id);
            SetField(lot, "_displayName", id);
            SetField(lot, "_baseCost", baseCost);
            SetField(lot, "_incomeBonus", 5f);
            SetField(lot, "_tier2UpgradeCost", t2);
            SetField(lot, "_tier3UpgradeCost", t3);
            SetField(lot, "_rivalBuyoutMultiplier", rivalMult);
            return lot;
        }

        private static void SetField(object obj, string name, object value)
        {
            var f = obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            f?.SetValue(obj, value);
        }

        // ── starter seeding ──

        [Test]
        public void GameStart_SeedsStarterLotsAtTier2()
        {
            Assert.AreEqual(Owner.Player, _city.GetOwner("starter_player"));
            Assert.AreEqual(Owner.Rival, _city.GetOwner("starter_rival"));
            Assert.AreEqual(2, _city.GetTier("starter_player"));
            Assert.AreEqual(2, _city.GetTier("starter_rival"));
        }

        [Test]
        public void GameStart_UnownedLotsHaveTierZero()
        {
            Assert.AreEqual(0, _city.GetTier("unowned_1"));
        }

        // ── fresh purchase ──

        [Test]
        public void BuyUnownedLot_DeductsBaseCostAndSetsTier1()
        {
            float before = _currency.CheckingBalance;
            bool ok = _city.TryPurchaseLot("unowned_1", 0);
            Assert.IsTrue(ok);
            Assert.AreEqual(Owner.Player, _city.GetOwner("unowned_1"));
            Assert.AreEqual(1, _city.GetTier("unowned_1"));
            Assert.AreEqual(before - 1000f, _currency.CheckingBalance, 0.01f);
        }

        // ── rival buyout ──

        [Test]
        public void BuyRivalOwnedLot_DeductsBaseCostTimesMultiplierAndResetsToTier1()
        {
            float before = _currency.CheckingBalance;
            bool ok = _city.TryPurchaseLot("starter_rival", 10);
            Assert.IsTrue(ok, "buyout should succeed with sufficient funds");
            Assert.AreEqual(Owner.Player, _city.GetOwner("starter_rival"));
            Assert.AreEqual(1, _city.GetTier("starter_rival"), "buyout resets to T1");
            // starter_rival BaseCost=500, multiplier=3 -> 1500
            Assert.AreEqual(before - 1500f, _currency.CheckingBalance, 0.01f);
        }

        [Test]
        public void ResolvePurchaseCost_UsesMultiplierForRival()
        {
            Assert.AreEqual(1500f, _city.ResolvePurchaseCost("starter_rival"), 0.01f);
            Assert.AreEqual(1000f, _city.ResolvePurchaseCost("unowned_1"), 0.01f);
        }

        [Test]
        public void BuyPlayerOwnedLot_Rejected()
        {
            float before = _currency.CheckingBalance;
            bool ok = _city.TryPurchaseLot("starter_player", 0);
            Assert.IsFalse(ok);
            Assert.AreEqual(before, _currency.CheckingBalance, 0.01f);
        }

        // ── upgrades ──

        [Test]
        public void UpgradePlayerLotT1_ToT2_DeductsTier2Cost()
        {
            _city.TryPurchaseLot("unowned_1", 0); // T1
            float before = _currency.CheckingBalance;

            bool ok = _city.TryUpgradeLot("unowned_1");
            Assert.IsTrue(ok);
            Assert.AreEqual(2, _city.GetTier("unowned_1"));
            Assert.AreEqual(before - 500f, _currency.CheckingBalance, 0.01f);
        }

        [Test]
        public void UpgradeStarterT2_ToT3_DeductsTier3Cost()
        {
            float before = _currency.CheckingBalance;
            bool ok = _city.TryUpgradeLot("starter_player");
            Assert.IsTrue(ok);
            Assert.AreEqual(3, _city.GetTier("starter_player"));
            Assert.AreEqual(before - 1500f, _currency.CheckingBalance, 0.01f);
        }

        [Test]
        public void UpgradeAtMaxTier_Rejected()
        {
            // starter_player is T2; upgrade once to T3, then attempt again.
            Assert.IsTrue(_city.TryUpgradeLot("starter_player"));
            float before = _currency.CheckingBalance;
            bool ok = _city.TryUpgradeLot("starter_player");
            Assert.IsFalse(ok);
            Assert.AreEqual(before, _currency.CheckingBalance, 0.01f);
            Assert.AreEqual(3, _city.GetTier("starter_player"));
        }

        [Test]
        public void UpgradeRivalOwnedLot_Rejected()
        {
            float before = _currency.CheckingBalance;
            bool ok = _city.TryUpgradeLot("starter_rival");
            Assert.IsFalse(ok);
            Assert.AreEqual(before, _currency.CheckingBalance, 0.01f);
        }

        [Test]
        public void UpgradeUnownedLot_Rejected()
        {
            bool ok = _city.TryUpgradeLot("unowned_1");
            Assert.IsFalse(ok);
        }

        [Test]
        public void UpgradeWithInsufficientFunds_Rejected()
        {
            SetField(_currency, "_checkingBalance", 100f); // below any upgrade cost
            float before = _currency.CheckingBalance;
            bool ok = _city.TryUpgradeLot("starter_player"); // would need 1500
            Assert.IsFalse(ok);
            Assert.AreEqual(2, _city.GetTier("starter_player"));
            Assert.AreEqual(before, _currency.CheckingBalance, 0.01f);
        }

        // ── intent event wiring ──

        [Test]
        public void OnLotUpgradeRequested_InvokesUpgrade()
        {
            int observedTier = -1;
            GameEvents.OnLotTierChanged += (id, t) => { if (id == "starter_player") observedTier = t; };

            GameEvents.RaiseLotUpgradeRequested("starter_player");
            Assert.AreEqual(3, observedTier);
            Assert.AreEqual(3, _city.GetTier("starter_player"));
        }

        [Test]
        public void OnPurchaseLotRequested_ResolvesRivalBuyoutPrice()
        {
            float before = _currency.CheckingBalance;
            GameEvents.RaisePurchaseLotRequested("starter_rival", 0);
            Assert.AreEqual(Owner.Player, _city.GetOwner("starter_rival"));
            Assert.AreEqual(before - 1500f, _currency.CheckingBalance, 0.01f);
        }
    }
}
