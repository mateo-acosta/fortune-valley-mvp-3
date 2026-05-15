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
    /// Locks the acquisition_costs field round-trip end-to-end:
    ///   - JsonUtility serialize + deserialize preserves the entries.
    ///   - CityManager.Hydrate restores _acquisitionCost from the DTO.
    ///   - OwnedLotsAcquisitionTotal reflects hydrated state.
    ///   - GetAcquisitionCostEntries snapshots back faithfully.
    /// Rails-side serializer verification is in the manual checklist.
    /// </summary>
    [TestFixture]
    public class AcquisitionCostsRoundTripTests
    {
        [Test]
        public void JsonUtility_RoundTripsAcquisitionCosts()
        {
            var original = new GamePlayerStateDTO
            {
                game_mode = "homebase",
                acquisition_costs = new[]
                {
                    new AcquisitionCostEntry { lot_id = "lot_A", cost = 500f },
                    new AcquisitionCostEntry { lot_id = "lot_B", cost = 1500f },
                    new AcquisitionCostEntry { lot_id = "lot_C_rival_buyout", cost = 3000f }
                }
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<GamePlayerStateDTO>(json);

            Assert.IsNotNull(restored.acquisition_costs);
            Assert.AreEqual(3, restored.acquisition_costs.Length);
            Assert.AreEqual("lot_A", restored.acquisition_costs[0].lot_id);
            Assert.AreEqual(500f, restored.acquisition_costs[0].cost);
            Assert.AreEqual("lot_C_rival_buyout", restored.acquisition_costs[2].lot_id);
            Assert.AreEqual(3000f, restored.acquisition_costs[2].cost);
        }

        [Test]
        public void JsonUtility_NullAcquisitionCosts_RoundTripsAsNullOrEmpty()
        {
            // Legacy save shape (field omitted on server side): deserialize must
            // not throw. JsonUtility may turn null arrays into empty arrays; either
            // is acceptable as long as Hydrate handles both.
            var original = new GamePlayerStateDTO { game_mode = "homebase" };
            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<GamePlayerStateDTO>(json);

            // Either null OR empty is fine; Hydrate handles both.
            Assert.IsTrue(restored.acquisition_costs == null
                || restored.acquisition_costs.Length == 0);
        }

        [Test]
        public void CityManager_Hydrate_PopulatesAcquisitionCostsAndTotal()
        {
            var fx = new CityManagerFixture();
            try
            {
                var dto = new GamePlayerStateDTO
                {
                    lots_owned = new[] { "lot_a" },
                    franchise_levels = new[]
                    {
                        new FranchiseLevelDTO { lot_id = "lot_a", tier = 2 }
                    },
                    acquisition_costs = new[]
                    {
                        new AcquisitionCostEntry { lot_id = "lot_a", cost = 750f }
                    }
                };

                fx.City.Hydrate(dto);

                Assert.AreEqual(Owner.Player, fx.City.GetOwner("lot_a"));
                Assert.AreEqual(2, fx.City.GetTier("lot_a"));
                Assert.AreEqual(750f, fx.City.OwnedLotsAcquisitionTotal,
                    "Hydrate must restore _acquisitionCost so BusinessAssetValue is correct");
            }
            finally
            {
                fx.Dispose();
            }
        }

        [Test]
        public void CityManager_Hydrate_NullAcquisitionCosts_NoError()
        {
            // Legacy save: lots_owned + franchise_levels present, acquisition_costs missing.
            // Hydrate must succeed; OwnedLotsAcquisitionTotal falls back to 0 for those lots.
            var fx = new CityManagerFixture();
            try
            {
                var dto = new GamePlayerStateDTO
                {
                    lots_owned = new[] { "lot_a" },
                    franchise_levels = new[]
                    {
                        new FranchiseLevelDTO { lot_id = "lot_a", tier = 2 }
                    },
                    acquisition_costs = null
                };

                Assert.DoesNotThrow(() => fx.City.Hydrate(dto));
                Assert.AreEqual(0f, fx.City.OwnedLotsAcquisitionTotal);
            }
            finally
            {
                fx.Dispose();
            }
        }

        [Test]
        public void CityManager_Hydrate_DropsEntriesForUnknownLots()
        {
            var fx = new CityManagerFixture();
            try
            {
                var dto = new GamePlayerStateDTO
                {
                    lots_owned = new[] { "lot_a" },
                    franchise_levels = new[]
                    {
                        new FranchiseLevelDTO { lot_id = "lot_a", tier = 2 }
                    },
                    acquisition_costs = new[]
                    {
                        new AcquisitionCostEntry { lot_id = "lot_a", cost = 750f },
                        new AcquisitionCostEntry { lot_id = "ghost_lot", cost = 99_999f }
                    }
                };

                fx.City.Hydrate(dto);

                // Only the real lot contributed; ghost was dropped.
                Assert.AreEqual(750f, fx.City.OwnedLotsAcquisitionTotal);
            }
            finally
            {
                fx.Dispose();
            }
        }

        [Test]
        public void CityManager_GetAcquisitionCostEntries_RoundTripsHydratedState()
        {
            var fx = new CityManagerFixture();
            try
            {
                var dto = new GamePlayerStateDTO
                {
                    lots_owned = new[] { "lot_a", "lot_b" },
                    franchise_levels = new[]
                    {
                        new FranchiseLevelDTO { lot_id = "lot_a", tier = 2 },
                        new FranchiseLevelDTO { lot_id = "lot_b", tier = 1 }
                    },
                    acquisition_costs = new[]
                    {
                        new AcquisitionCostEntry { lot_id = "lot_a", cost = 750f },
                        new AcquisitionCostEntry { lot_id = "lot_b", cost = 1200f }
                    }
                };

                fx.City.Hydrate(dto);
                var roundTripped = fx.City.GetAcquisitionCostEntries();

                Assert.AreEqual(2, roundTripped.Length);

                float totalCost = 0f;
                var seenIds = new HashSet<string>();
                foreach (var entry in roundTripped)
                {
                    totalCost += entry.cost;
                    seenIds.Add(entry.lot_id);
                }
                Assert.AreEqual(1950f, totalCost);
                Assert.IsTrue(seenIds.Contains("lot_a"));
                Assert.IsTrue(seenIds.Contains("lot_b"));
            }
            finally
            {
                fx.Dispose();
            }
        }

        // ─── fixture ───

        private class CityManagerFixture
        {
            public GameObject Go;
            public CityManager City;
            public CurrencyManager Currency;
            public List<CityLotDefinition> AllLots;

            public CityManagerFixture()
            {
                GameEvents.ClearAllSubscriptions();
                Go = new GameObject("AcqCostsFx");
                Currency = Go.AddComponent<CurrencyManager>();
                SetField(Currency, "_startingCheckingBalance", 10000f);
                Currency.ResetBalance();

                AllLots = new List<CityLotDefinition>
                {
                    MakeLot("lot_a"),
                    MakeLot("lot_b")
                };
                City = Go.AddComponent<CityManager>();
                SetField(City, "_allLots", AllLots);
                SetField(City, "_currencyManager", Currency);
                SetField(City, "_currency", Currency);
            }

            public void Dispose()
            {
                foreach (var l in AllLots) Object.DestroyImmediate(l);
                Object.DestroyImmediate(Go);
                GameEvents.ClearAllSubscriptions();
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
}
