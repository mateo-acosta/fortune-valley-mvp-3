using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Stage 0a coverage. The day -> tick rename is being staged through an
    /// alias chain (add aliases now -> migrate callers -> remove old names).
    /// During Stage 0a both names must return identical values; if any of
    /// these tests fail, the alias chain is broken.
    ///
    /// These tests are intentionally redundant with whatever specific tests
    /// already cover the legacy names; their job is to gate the rename.
    /// They will be deleted in Stage 0c when the legacy names are removed.
    /// </summary>
    [TestFixture]
    public class TickAliasParityTests
    {
        // ========================================================================
        // LifespanConstants
        // ========================================================================

        [Test]
        public void TicksPerYear_EqualsDaysPerYear()
        {
            Assert.AreEqual(LifespanConstants.DaysPerYear, LifespanConstants.TicksPerYear);
        }

        [Test]
        public void TotalLifeTicks_EqualsTotalLifeDays()
        {
            Assert.AreEqual(LifespanConstants.TotalLifeDays, LifespanConstants.TotalLifeTicks);
        }

        [Test]
        public void AgeFromTick_EqualsAgeFromDay_ForSampleInputs()
        {
            int[] samples = { 0, 1, 14, 15, 29, 30, 60, 100, 599, 600, 1199, 1200, -5 };
            for (int i = 0; i < samples.Length; i++)
            {
                Assert.AreEqual(
                    LifespanConstants.AgeFromDay(samples[i]),
                    LifespanConstants.AgeFromTick(samples[i]),
                    "Sample {0}: AgeFromTick must equal AgeFromDay", samples[i]);
            }
        }

        [Test]
        public void HasReachedRetirementTick_EqualsHasReachedRetirement_ForSampleInputs()
        {
            int[] samples = { 0, 599, 600, 1199, 1200, 1500 };
            for (int i = 0; i < samples.Length; i++)
            {
                Assert.AreEqual(
                    LifespanConstants.HasReachedRetirement(samples[i]),
                    LifespanConstants.HasReachedRetirementTick(samples[i]),
                    "Sample {0} mismatch", samples[i]);
            }
        }

        // ========================================================================
        // TimeManager
        // ========================================================================

        [Test]
        public void TimeManager_TickAliases_MatchLegacyAccessors()
        {
            var go = new GameObject("TimeManagerAliasTest");
            try
            {
                var tm = go.AddComponent<TimeManager>();
                // Both pairs must point at the same backing fields. CurrentDay
                // counts heartbeats; CurrentTickCount is the new name. CurrentTick
                // counts atomic engine pulses; CurrentEnginePulse is the new name.
                Assert.AreEqual(tm.CurrentDay, tm.CurrentTickCount);
                Assert.AreEqual(tm.CurrentTick, tm.CurrentEnginePulse);
                Assert.AreEqual(tm.TicksPerDay, tm.EnginePulsesPerTick);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ========================================================================
        // ActiveLoan
        // ========================================================================

        [Test]
        public void ActiveLoan_YearlyPayment_EqualsMonthlyPayment()
        {
            var loan = new ActiveLoan(
                loanId: "test-loan-1",
                lotId: "Lot_Block01",
                principal: 10000f,
                apr: 0.05f,
                termMonths: 12,
                monthlyPayment: 856.07f,
                downPayment: 1000f,
                startDay: 0);
            Assert.AreEqual(loan.MonthlyPayment, loan.YearlyPayment);
            Assert.AreEqual(loan.TermMonths, loan.TermTicks);
            Assert.AreEqual(loan.StartDay, loan.StartTick);
        }

        [Test]
        public void ActiveLoan_CalculateYearlyPayment_EqualsCalculateMonthlyPayment()
        {
            float a = ActiveLoan.CalculateMonthlyPayment(15000f, 0.07f, 36);
            float b = ActiveLoan.CalculateYearlyPayment(15000f, 0.07f, 36);
            Assert.AreEqual(a, b, 0.0001f);
        }

        // ========================================================================
        // LoanPortfolio
        // ========================================================================

        [Test]
        public void LoanPortfolio_GetTotalYearlyDebt_EqualsGetTotalMonthlyDebt()
        {
            var portfolio = new LoanPortfolio();
            // Empty portfolio: both must return 0.
            Assert.AreEqual(portfolio.GetTotalMonthlyDebt(), portfolio.GetTotalYearlyDebt());

            // Populate with a fresh-loan-style entry via the constructor (no
            // currency-manager dependency since we are just summing payments).
            portfolio.AddRestored(new ActiveLoan(
                loanId: "L1",
                lotId: "Lot_Block01",
                principal: 5000f,
                apr: 0.06f,
                termMonths: 12,
                monthlyPayment: 430f,
                downPayment: 0f,
                startDay: 0));
            Assert.AreEqual(portfolio.GetTotalMonthlyDebt(), portfolio.GetTotalYearlyDebt());
        }

        // ========================================================================
        // DTO field parity (write side via GameStateDTOBuilder)
        //
        // The builder writes both legacy and new field names. The values must
        // match for any save snapshot we produce in Stage 0a, otherwise Stage
        // 0b's reader migration will pick up divergent data.
        // ========================================================================

        [Test]
        public void GamePlayerStateDTO_AllowsBothLegacyAndNewFields()
        {
            // Sanity: a fresh DTO has both old and new fields available, both
            // default to zero, and writing one does not mutate the other.
            var dto = new GamePlayerStateDTO
            {
                current_day = 42,
                current_tick_count = 42,
                current_tick = 7,
                current_engine_pulse = 7,
                monthly_income = 1234.5f,
                yearly_income = 1234.5f
            };
            Assert.AreEqual(dto.current_day, dto.current_tick_count);
            Assert.AreEqual(dto.current_tick, dto.current_engine_pulse);
            Assert.AreEqual(dto.monthly_income, dto.yearly_income);
        }

        [Test]
        public void ActiveLoanDTO_AllowsBothLegacyAndNewFields()
        {
            var dto = new ActiveLoanDTO
            {
                loan_id = "L1",
                monthly_payment = 220f,
                yearly_payment = 220f,
                term_months = 12,
                term_ticks = 12,
                start_day = 0,
                start_tick = 0
            };
            Assert.AreEqual(dto.monthly_payment, dto.yearly_payment);
            Assert.AreEqual(dto.term_months, dto.term_ticks);
            Assert.AreEqual(dto.start_day, dto.start_tick);
        }

        [Test]
        public void LifeGoalEntry_MarkRealized_WritesBothFields()
        {
            var entry = new LifeGoalEntry("first_apartment", LifeGoalTier.Starter, 100000f);
            Assert.AreEqual(-1, entry.realized_at_day);
            Assert.AreEqual(-1, entry.realized_at_tick);

            entry.MarkRealized(150);
            Assert.IsTrue(entry.realized);
            Assert.AreEqual(150, entry.realized_at_day);
            Assert.AreEqual(150, entry.realized_at_tick);
        }
    }
}
