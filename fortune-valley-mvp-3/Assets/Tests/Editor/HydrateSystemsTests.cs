using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.Tests.Common;
using FortuneValley.Tests.Fixtures;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Per-system Hydrate contract tests. Each system's public Hydrate
    /// method is exercised directly with composed DTO fixtures; null and
    /// empty-array paths are covered alongside happy paths. Heavyweight
    /// systems with ScriptableObject configs (Investment, Insurance,
    /// CreditCard) are covered through integration via the existing system
    /// test suites; the tests here lock down the read-side fields the
    /// bootstrapper actually surfaces.
    /// </summary>
    public class HydrateSystemsTests : SaveTestsBase
    {
        // ─────────────────────────────────────────────────────────────────
        // TimeManager
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void TimeManager_Hydrate_HappyPath_SetsDayAndTick()
        {
            var time = SpawnComponent<TimeManager>("TimeManager");
            var dto = GamePlayerStateDTOFixtures.Default().WithDay(day: 12, tick: 7);

            time.Hydrate(dto);

            Assert.AreEqual(12, time.CurrentDay);
            Assert.AreEqual(7, time.CurrentTick);
        }

        [Test]
        public void TimeManager_Hydrate_NullDto_DoesNothing()
        {
            var time = SpawnComponent<TimeManager>("TimeManager");
            int dayBefore = time.CurrentDay;
            int tickBefore = time.CurrentTick;

            time.Hydrate(null);

            Assert.AreEqual(dayBefore, time.CurrentDay);
            Assert.AreEqual(tickBefore, time.CurrentTick);
        }

        [Test]
        public void TimeManager_Hydrate_Idempotent_RefireMatchesSingleFire()
        {
            var time = SpawnComponent<TimeManager>("TimeManager");
            var dto = GamePlayerStateDTOFixtures.Default().WithDay(5, 3);

            time.Hydrate(dto);
            time.Hydrate(dto);

            Assert.AreEqual(5, time.CurrentDay);
            Assert.AreEqual(3, time.CurrentTick);
        }

        [Test]
        public void TimeManager_OnEnable_CatchUpFromLastLoadedSaveDto()
        {
            // Set the catch-up handle BEFORE the system instantiates, so
            // OnEnable's catch-up branch sees a non-null DTO.
            GameEvents.LastLoadedSaveDto = GamePlayerStateDTOFixtures.Default().WithDay(99, 1);

            var time = SpawnComponent<TimeManager>("TimeManager", invokeOnEnable: true);

            Assert.AreEqual(99, time.CurrentDay,
                "Late-joining TimeManager must hydrate from LastLoadedSaveDto in OnEnable");
            Assert.AreEqual(1, time.CurrentTick);
        }

        // ─────────────────────────────────────────────────────────────────
        // RestaurantSystem
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void RestaurantSystem_Hydrate_HappyPath_SetsLevelAndRaisesEvent()
        {
            var rest = SpawnComponent<RestaurantSystem>("RestaurantSystem");

            int observedLevel = -1;
            GameEvents.OnRestaurantUpgraded += lvl => observedLevel = lvl;

            rest.Hydrate(GamePlayerStateDTOFixtures.Default().WithRestaurantLevel(3));

            Assert.AreEqual(3, rest.CurrentLevel);
            Assert.AreEqual(3, observedLevel,
                "Hydrate must raise OnRestaurantUpgraded so visuals refresh");
        }

        [Test]
        public void RestaurantSystem_Hydrate_NullDto_DoesNothing()
        {
            var rest = SpawnComponent<RestaurantSystem>("RestaurantSystem");
            int observedLevel = -1;
            GameEvents.OnRestaurantUpgraded += lvl => observedLevel = lvl;

            rest.Hydrate(null);

            Assert.AreEqual(-1, observedLevel, "Null DTO must be a no-op");
        }

        [Test]
        public void RestaurantSystem_Hydrate_LevelZero_TreatedAsInvalid_DoesNothing()
        {
            var rest = SpawnComponent<RestaurantSystem>("RestaurantSystem");
            int observedLevel = -1;
            GameEvents.OnRestaurantUpgraded += lvl => observedLevel = lvl;

            // restaurant_level < 1 is a bad DTO; skip without raising.
            rest.Hydrate(GamePlayerStateDTOFixtures.Default().WithRestaurantLevel(0));

            Assert.AreEqual(-1, observedLevel, "Invalid level must skip");
        }

        // ─────────────────────────────────────────────────────────────────
        // CityManager
        // ─────────────────────────────────────────────────────────────────

        // CityManager.ResetOwnership iterates _allLots (a [SerializeField]
        // populated in the Inspector). In EditMode tests we set it to an
        // empty list so the iteration is a no-op; the test still exercises
        // Hydrate's dictionary writes + per-item event raises.
        private static CityManager SpawnCityManagerWithEmptyLotList(SaveTestsBase fixture)
        {
            var city = fixture.GetType()
                .GetMethod("SpawnComponent", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(typeof(CityManager))
                .Invoke(fixture, new object[] { "CityManager", false }) as CityManager;
            typeof(CityManager)
                .GetField("_allLots", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(city, new List<CityLotDefinition>());
            return city;
        }

        [Test]
        public void CityManager_Hydrate_HappyPath_SetsOwnershipAndTiers()
        {
            var city = SpawnCityManagerWithEmptyLotList(this);

            int playerLotEvents = 0, rivalLotEvents = 0, tierEvents = 0;
            GameEvents.OnLotPurchased += (_, owner) =>
            {
                if (owner == Owner.Player) playerLotEvents++;
                else if (owner == Owner.Rival) rivalLotEvents++;
            };
            GameEvents.OnLotTierChanged += (_, _) => tierEvents++;

            var dto = GamePlayerStateDTOFixtures.Default()
                .WithLots(playerLots: new[] { "Lot_Block01", "Lot_Block02" },
                          rivalLots: new[] { "Lot_Block03" })
                .WithFranchiseTiers(("Lot_Block01", 2), ("Lot_Block03", 3));

            city.Hydrate(dto);

            Assert.AreEqual(Owner.Player, city.LotOwnership["Lot_Block01"]);
            Assert.AreEqual(Owner.Player, city.LotOwnership["Lot_Block02"]);
            Assert.AreEqual(Owner.Rival, city.LotOwnership["Lot_Block03"]);
            Assert.AreEqual(2, city.LotTiers["Lot_Block01"]);
            Assert.AreEqual(3, city.LotTiers["Lot_Block03"]);

            Assert.AreEqual(2, playerLotEvents);
            Assert.AreEqual(1, rivalLotEvents);
            Assert.AreEqual(2, tierEvents);
        }

        [Test]
        public void CityManager_Hydrate_NullArrays_NoExceptionNoSpuriousEntries()
        {
            var city = SpawnCityManagerWithEmptyLotList(this);

            // Default DTO has empty arrays; explicitly null one to test the
            // null-array branches.
            var dto = GamePlayerStateDTOFixtures.Default();
            dto.lots_owned = null;
            dto.rival_lots_owned = null;
            dto.franchise_levels = null;

            Assert.DoesNotThrow(() => city.Hydrate(dto));
            Assert.IsEmpty(city.LotOwnership);
            Assert.IsEmpty(city.LotTiers);
        }

        [Test]
        public void CityManager_Hydrate_NullDto_DoesNothing()
        {
            var city = SpawnCityManagerWithEmptyLotList(this);
            Assert.DoesNotThrow(() => city.Hydrate(null));
        }

        // ─────────────────────────────────────────────────────────────────
        // LoanSystem (exercises ActiveLoan.FromSave + LoanPortfolio.AddRestored)
        // ─────────────────────────────────────────────────────────────────

        [Test]
        public void LoanSystem_Hydrate_HappyPath_RebuildsPortfolio()
        {
            var loanSys = SpawnComponent<LoanSystem>("LoanSystem");
            // Set _portfolio via reflection (no public setter; OnGameStart usually
            // does this, but EditMode skips OnEnable subscription chain).
            var portfolioField = typeof(LoanSystem).GetField("_portfolio",
                BindingFlags.NonPublic | BindingFlags.Instance);
            portfolioField.SetValue(loanSys, new LoanPortfolio());

            var dto = GamePlayerStateDTOFixtures.Default().WithLoans(
                new ActiveLoanDTO
                {
                    loan_id = "loan_a", lot_id = "Lot_Block01",
                    principal = 10000f, apr = 0.06f,
                    term_months = 60, monthly_payment = 200f,
                    down_payment = 0f, start_day = 5,
                    remaining_balance = 9500f, payments_made = 1
                });

            loanSys.Hydrate(dto);

            Assert.AreEqual(1, loanSys.Portfolio.AllLoans.Count);
            var loan = loanSys.Portfolio.AllLoans[0];
            Assert.AreEqual("loan_a", loan.LoanId);
            Assert.AreEqual(9500f, loan.RemainingBalance, 0.001f);
        }

        [Test]
        public void LoanSystem_Hydrate_NullActiveLoans_ClearsPortfolio()
        {
            var loanSys = SpawnComponent<LoanSystem>("LoanSystem");
            var portfolioField = typeof(LoanSystem).GetField("_portfolio",
                BindingFlags.NonPublic | BindingFlags.Instance);
            portfolioField.SetValue(loanSys, new LoanPortfolio());

            var dto = GamePlayerStateDTOFixtures.Default();
            dto.active_loans = null;

            loanSys.Hydrate(dto);

            Assert.AreEqual(0, loanSys.Portfolio.AllLoans.Count);
        }

        [Test]
        public void LoanSystem_Hydrate_Idempotent_RefireMatchesSingleFire()
        {
            var loanSys = SpawnComponent<LoanSystem>("LoanSystem");
            var portfolioField = typeof(LoanSystem).GetField("_portfolio",
                BindingFlags.NonPublic | BindingFlags.Instance);
            portfolioField.SetValue(loanSys, new LoanPortfolio());

            var dto = GamePlayerStateDTOFixtures.Default().WithLoans(
                new ActiveLoanDTO
                {
                    loan_id = "loan_a", lot_id = "Lot_Block01",
                    principal = 5000f, apr = 0.05f,
                    term_months = 36, monthly_payment = 150f,
                    down_payment = 0f, start_day = 0,
                    remaining_balance = 5000f, payments_made = 0
                });

            loanSys.Hydrate(dto);
            loanSys.Hydrate(dto);

            Assert.AreEqual(1, loanSys.Portfolio.AllLoans.Count,
                "Re-fire must not duplicate loans");
        }
    }
}
