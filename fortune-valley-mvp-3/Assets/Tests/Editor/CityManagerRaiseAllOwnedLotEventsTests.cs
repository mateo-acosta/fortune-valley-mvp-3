using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Pin the Phase 2 catch-up event emission contract: fire LotPurchased and
    /// LotTierChanged exactly once per owned lot; do NOT fire LotOwnershipChanged
    /// (live-transition subscribers use other paths to restore their state).
    /// </summary>
    [TestFixture]
    public class CityManagerRaiseAllOwnedLotEventsTests
    {
        private GameObject _go;
        private CityManager _city;
        private CurrencyManager _currency;
        private List<CityLotDefinition> _lots;

        private int _purchasedCalls;
        private int _tierCalls;
        private int _ownershipCalls;
        private readonly Dictionary<string, int> _purchasedPerLot = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _tierPerLot = new Dictionary<string, int>();

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _purchasedCalls = 0;
            _tierCalls = 0;
            _ownershipCalls = 0;
            _purchasedPerLot.Clear();
            _tierPerLot.Clear();

            _go = new GameObject("RaiseAllOwnedFx");
            _currency = _go.AddComponent<CurrencyManager>();
            SetField(_currency, "_startingCheckingBalance", 10000f);
            _currency.ResetBalance();

            _lots = new List<CityLotDefinition>
            {
                MakeLot("player_starter"),
                MakeLot("rival_starter"),
                MakeLot("player_bought"),
                MakeLot("unowned")
            };

            _city = _go.AddComponent<CityManager>();
            SetField(_city, "_allLots", _lots);
            SetField(_city, "_currencyManager", _currency);
            SetField(_city, "_currency", _currency);

            GameEvents.OnLotPurchased += HandlePurchased;
            GameEvents.OnLotTierChanged += HandleTier;
            GameEvents.OnLotOwnershipChanged += HandleOwnership;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var l in _lots) Object.DestroyImmediate(l);
            Object.DestroyImmediate(_go);
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void RaiseAllOwnedLotEvents_FiresLotPurchasedAndTierChangedPerOwnedLot()
        {
            // Stage state via Hydrate then reset counters; RaiseAllOwnedLotEvents
            // is the contract under test.
            var dto = new GamePlayerStateDTO
            {
                lots_owned = new[] { "player_starter", "player_bought" },
                rival_lots_owned = new[] { "rival_starter" },
                franchise_levels = new[]
                {
                    new FranchiseLevelDTO { lot_id = "player_starter", tier = 2 },
                    new FranchiseLevelDTO { lot_id = "player_bought", tier = 1 },
                    new FranchiseLevelDTO { lot_id = "rival_starter", tier = 3 }
                }
            };
            _city.Hydrate(dto);
            ResetCounts();

            _city.RaiseAllOwnedLotEvents();

            Assert.AreEqual(3, _purchasedCalls, "LotPurchased once per owned lot (player + rival)");
            Assert.AreEqual(3, _tierCalls, "LotTierChanged once per owned lot");
            Assert.AreEqual(0, _ownershipCalls,
                "LotOwnershipChanged must NOT fire on Phase 2 catch-up");

            Assert.AreEqual(1, _purchasedPerLot["player_starter"]);
            Assert.AreEqual(1, _purchasedPerLot["player_bought"]);
            Assert.AreEqual(1, _purchasedPerLot["rival_starter"]);
            Assert.IsFalse(_purchasedPerLot.ContainsKey("unowned"));

            Assert.AreEqual(1, _tierPerLot["player_starter"]);
            Assert.AreEqual(1, _tierPerLot["player_bought"]);
            Assert.AreEqual(1, _tierPerLot["rival_starter"]);
        }

        [Test]
        public void RaiseAllOwnedLotEvents_NoOwnedLots_FiresNothing()
        {
            // Empty state (fresh boot before any seeding).
            _city.RaiseAllOwnedLotEvents();

            Assert.AreEqual(0, _purchasedCalls);
            Assert.AreEqual(0, _tierCalls);
            Assert.AreEqual(0, _ownershipCalls);
        }

        // ─── helpers ───

        private void HandlePurchased(string lotId, Owner owner)
        {
            _purchasedCalls++;
            _purchasedPerLot.TryGetValue(lotId, out int n);
            _purchasedPerLot[lotId] = n + 1;
        }

        private void HandleTier(string lotId, int tier)
        {
            _tierCalls++;
            _tierPerLot.TryGetValue(lotId, out int n);
            _tierPerLot[lotId] = n + 1;
        }

        private void HandleOwnership(string lotId, Owner prev, Owner next) => _ownershipCalls++;

        private void ResetCounts()
        {
            _purchasedCalls = 0;
            _tierCalls = 0;
            _ownershipCalls = 0;
            _purchasedPerLot.Clear();
            _tierPerLot.Clear();
        }

        private static CityLotDefinition MakeLot(string id)
        {
            var lot = ScriptableObject.CreateInstance<CityLotDefinition>();
            SetField(lot, "_lotId", id);
            SetField(lot, "_displayName", id);
            SetField(lot, "_baseCost", 500f);
            return lot;
        }

        private static void SetField(object obj, string name, object value)
        {
            var f = obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            f?.SetValue(obj, value);
        }
    }
}
