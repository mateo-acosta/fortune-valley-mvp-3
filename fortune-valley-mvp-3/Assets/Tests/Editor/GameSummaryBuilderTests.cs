using NUnit.Framework;
using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Interfaces;
using FortuneValley.Managers;

namespace FortuneValley.Tests
{
    public class GameSummaryBuilderTests
    {
        // Minimal stub implementations for testing without a scene.

        private class StubCityService : ICityService
        {
            public int PlayerLotCount { get; set; }
            public int RivalLotCount { get; set; }
            public int TotalLots { get; set; }
            public Owner GetOwner(string lotId) => Owner.None;
            public string GetCitySummary() => string.Empty;
        }

        private class StubCurrencyService : ICurrencyService
        {
            public float Balance { get; set; }
            public void Add(float amount, string source = "Unknown") { }
            public bool TrySpend(float amount, string reason = "Unknown") => true;
            public bool CanAfford(float amount) => Balance >= amount;
        }

        private class StubInvestmentService : IInvestmentService
        {
            public float TotalPortfolioValue { get; set; }
            public float LifetimeTotalGain { get; set; }
            public float PeakPortfolioValue { get; set; }
            public float LifetimeTotalPrincipalInvested { get; set; }
            public int LifetimeTotalInvestmentsMade { get; set; }
            public string GetPortfolioSummary() => string.Empty;
        }

        private class StubRestaurantService : IRestaurantService
        {
            public float TotalEarned { get; set; }
            public int CurrentLevel { get; set; }
            public string GetPerformanceSummary() => string.Empty;
        }

        [Test]
        public void Build_WithNullCityManager_ReturnsZeroLots()
        {
            GameSummary summary = GameSummaryBuilder.Build(
                isPlayerWin: true,
                daysPlayed: 5,
                cityManager: null,
                currencyManager: null,
                investmentSystem: null,
                restaurantSystem: null);

            Assert.AreEqual(0, summary.PlayerLots);
            Assert.AreEqual(0, summary.RivalLots);
            Assert.AreEqual(0, summary.TotalLots);
        }

        [Test]
        public void Build_WithNoLotsOwned_ReturnsEmptyLotPurchases()
        {
            var city = new StubCityService { PlayerLotCount = 0, RivalLotCount = 3, TotalLots = 3 };

            GameSummary summary = GameSummaryBuilder.Build(
                isPlayerWin: false,
                daysPlayed: 10,
                cityManager: city,
                currencyManager: null,
                investmentSystem: null,
                restaurantSystem: null,
                lotPurchases: new List<LotPurchaseRecord>());

            Assert.AreEqual(0, summary.LotPurchases.Count);
        }

        [Test]
        public void Build_WithNegativeInvestmentGains_StillBuildsValidSummary()
        {
            var investments = new StubInvestmentService
            {
                LifetimeTotalGain = -500f,
                TotalPortfolioValue = 200f
            };

            GameSummary summary = GameSummaryBuilder.Build(
                isPlayerWin: false,
                daysPlayed: 20,
                cityManager: null,
                currencyManager: null,
                investmentSystem: investments,
                restaurantSystem: null);

            Assert.IsNotNull(summary);
            Assert.AreEqual(-500f, summary.TotalInvestmentGains);
        }

        [Test]
        public void Build_FinalNetWorth_IncludesPortfolioValue()
        {
            var currency = new StubCurrencyService { Balance = 1000f };
            var investments = new StubInvestmentService { TotalPortfolioValue = 500f };

            GameSummary summary = GameSummaryBuilder.Build(
                isPlayerWin: true,
                daysPlayed: 15,
                cityManager: null,
                currencyManager: currency,
                investmentSystem: investments,
                restaurantSystem: null);

            // Net worth = balance + portfolio value
            Assert.AreEqual(1500f, summary.FinalNetWorth);
        }
    }
}
