using System.Collections.Generic;
using NUnit.Framework;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.UI;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class InsuranceSummaryCalculatorTests
    {
        // ─── CalculateHomeSummary ────────────────────────────────────────

        [Test]
        public void HomeSummary_NullInput_ReturnsZeros()
        {
            InsuranceSummaryCalculator.CalculateHomeSummary(
                null, out int count, out float premium);

            Assert.AreEqual(0, count);
            Assert.AreEqual(0f, premium, 0.01f);
        }

        [Test]
        public void HomeSummary_EmptyList_ReturnsZeros()
        {
            InsuranceSummaryCalculator.CalculateHomeSummary(
                new List<ActiveInsurancePolicy>(), out int count, out float premium);

            Assert.AreEqual(0, count);
            Assert.AreEqual(0f, premium, 0.01f);
        }

        [Test]
        public void HomeSummary_ActivePolicies_CalculatesCorrectly()
        {
            var policies = new List<ActiveInsurancePolicy>
            {
                MakePolicy(50f),
                MakePolicy(30f)
            };

            InsuranceSummaryCalculator.CalculateHomeSummary(
                policies, out int count, out float premium);

            Assert.AreEqual(2, count);
            Assert.AreEqual(80f, premium, 0.01f);
        }

        [Test]
        public void HomeSummary_ExcludesInactivePolicies()
        {
            var active = MakePolicy(50f);
            var inactive = MakePolicy(30f);
            inactive.Deactivate();

            var policies = new List<ActiveInsurancePolicy> { active, inactive };

            InsuranceSummaryCalculator.CalculateHomeSummary(
                policies, out int count, out float premium);

            Assert.AreEqual(1, count);
            Assert.AreEqual(50f, premium, 0.01f);
        }

        // ─── CalculateHistorySummary ────────────────────────────────────

        [Test]
        public void HistorySummary_EmptyList_ReturnsAllZeros()
        {
            InsuranceSummaryCalculator.CalculateHistorySummary(
                new List<TransactionRecord>(),
                out float accidentCosts, out float premiumsPaid, out int accidentCount);

            Assert.AreEqual(0f, accidentCosts, 0.01f);
            Assert.AreEqual(0f, premiumsPaid, 0.01f);
            Assert.AreEqual(0, accidentCount);
        }

        [Test]
        public void HistorySummary_NullInput_ReturnsAllZeros()
        {
            InsuranceSummaryCalculator.CalculateHistorySummary(
                null,
                out float accidentCosts, out float premiumsPaid, out int accidentCount);

            Assert.AreEqual(0f, accidentCosts, 0.01f);
            Assert.AreEqual(0f, premiumsPaid, 0.01f);
            Assert.AreEqual(0, accidentCount);
        }

        [Test]
        public void HistorySummary_OnlyPremiums_AccidentFieldsAreZero()
        {
            var records = new List<TransactionRecord>
            {
                new TransactionRecord(TransactionType.PremiumCharged, "premium", 50f, 1),
                new TransactionRecord(TransactionType.PremiumCharged, "premium", 50f, 2)
            };

            InsuranceSummaryCalculator.CalculateHistorySummary(
                records,
                out float accidentCosts, out float premiumsPaid, out int accidentCount);

            Assert.AreEqual(0f, accidentCosts, 0.01f);
            Assert.AreEqual(100f, premiumsPaid, 0.01f);
            Assert.AreEqual(0, accidentCount);
        }

        [Test]
        public void HistorySummary_OnlyAccidents_PremiumFieldIsZero()
        {
            var records = new List<TransactionRecord>
            {
                new TransactionRecord(TransactionType.AccidentResolved, "fire", 500f, 1),
                new TransactionRecord(TransactionType.AccidentResolved, "flood", 1000f, 2)
            };

            InsuranceSummaryCalculator.CalculateHistorySummary(
                records,
                out float accidentCosts, out float premiumsPaid, out int accidentCount);

            Assert.AreEqual(1500f, accidentCosts, 0.01f);
            Assert.AreEqual(0f, premiumsPaid, 0.01f);
            Assert.AreEqual(2, accidentCount);
        }

        [Test]
        public void HistorySummary_MixedTypes_CalculatesCorrectly()
        {
            var records = new List<TransactionRecord>
            {
                new TransactionRecord(TransactionType.AccidentResolved, "fire", 200f, 1),
                new TransactionRecord(TransactionType.PremiumCharged, "premium", 50f, 2),
                new TransactionRecord(TransactionType.InsurancePurchased, "bought", 0f, 3),
                new TransactionRecord(TransactionType.AccidentResolved, "flood", 800f, 4),
                new TransactionRecord(TransactionType.PremiumCharged, "premium", 50f, 5)
            };

            InsuranceSummaryCalculator.CalculateHistorySummary(
                records,
                out float accidentCosts, out float premiumsPaid, out int accidentCount);

            Assert.AreEqual(1000f, accidentCosts, 0.01f);
            Assert.AreEqual(100f, premiumsPaid, 0.01f);
            Assert.AreEqual(2, accidentCount);
        }

        [Test]
        public void HistorySummary_ZeroAmountRecords_DoNotAffectTotals()
        {
            var records = new List<TransactionRecord>
            {
                new TransactionRecord(TransactionType.InsurancePurchased, "bought", 0f, 1),
                new TransactionRecord(TransactionType.InsuranceCanceled, "canceled", 0f, 2)
            };

            InsuranceSummaryCalculator.CalculateHistorySummary(
                records,
                out float accidentCosts, out float premiumsPaid, out int accidentCount);

            Assert.AreEqual(0f, accidentCosts, 0.01f);
            Assert.AreEqual(0f, premiumsPaid, 0.01f);
            Assert.AreEqual(0, accidentCount);
        }

        [Test]
        public void HistorySummary_NonInsuranceRecords_AreIgnored()
        {
            // Calculator receives pre-filtered input, but if non-insurance
            // records slip through, they should not affect results
            var records = new List<TransactionRecord>
            {
                new TransactionRecord(TransactionType.LoanPayment, "loan", 500f, 1),
                new TransactionRecord(TransactionType.CreditCardCharge, "cc", 200f, 2),
                new TransactionRecord(TransactionType.AccidentResolved, "fire", 100f, 3)
            };

            InsuranceSummaryCalculator.CalculateHistorySummary(
                records,
                out float accidentCosts, out float premiumsPaid, out int accidentCount);

            Assert.AreEqual(100f, accidentCosts, 0.01f);
            Assert.AreEqual(0f, premiumsPaid, 0.01f);
            Assert.AreEqual(1, accidentCount);
        }

        // ─── helpers ────────────────────────────────────────────────────

        private static ActiveInsurancePolicy MakePolicy(float premium)
        {
            return new ActiveInsurancePolicy(
                "test", "lot_1", InsurancePolicyType.GeneralProtection,
                premium, 200f, 0.8f,
                new List<string> { "fire" }, 0);
        }
    }
}
