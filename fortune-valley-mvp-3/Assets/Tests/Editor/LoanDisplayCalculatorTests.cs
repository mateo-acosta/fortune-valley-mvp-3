using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.UI;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LoanDisplayCalculatorTests
    {
        private LoanConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = CreateLoanConfig(
                apr: 0.08f,
                termMonths: 12,
                downPaymentPercent: 0.20f,
                minCreditScore: 650);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [Test]
        public void Calculate_NormalCase_PrincipalIsLotPriceMinusDownPayment()
        {
            float lotPrice = 10000f;

            var result = LoanDisplayCalculator.Calculate(lotPrice, _config);

            // 20% down on 10000 = 2000 down, 8000 principal
            Assert.AreEqual(2000f, result.DownPayment, 0.01f);
            Assert.AreEqual(8000f, result.Principal, 0.01f);
        }

        [Test]
        public void Calculate_NormalCase_MonthlyPaymentMatchesAmortization()
        {
            float lotPrice = 10000f;

            var result = LoanDisplayCalculator.Calculate(lotPrice, _config);

            // Verify against ActiveLoan's amortization directly
            float expectedPayment = ActiveLoan.CalculateMonthlyPayment(
                8000f, 0.08f, 12);

            Assert.AreEqual(expectedPayment, result.MonthlyPayment, 0.01f);
        }

        [Test]
        public void Calculate_NormalCase_TotalCostIncludesDownPayment()
        {
            float lotPrice = 10000f;

            var result = LoanDisplayCalculator.Calculate(lotPrice, _config);

            float expectedTotal = (result.MonthlyPayment * 12) + 2000f;
            Assert.AreEqual(expectedTotal, result.TotalCost, 0.01f);
        }

        [Test]
        public void Calculate_NormalCase_APRPercentIsCorrect()
        {
            float lotPrice = 10000f;

            var result = LoanDisplayCalculator.Calculate(lotPrice, _config);

            Assert.AreEqual(8.0f, result.APRPercent, 0.01f);
        }

        [Test]
        public void Calculate_NormalCase_MinCreditScorePassedThrough()
        {
            float lotPrice = 10000f;

            var result = LoanDisplayCalculator.Calculate(lotPrice, _config);

            Assert.AreEqual(650, result.MinCreditScore);
        }

        [Test]
        public void Calculate_NormalCase_TermMonthsPassedThrough()
        {
            float lotPrice = 10000f;

            var result = LoanDisplayCalculator.Calculate(lotPrice, _config);

            Assert.AreEqual(12, result.TermMonths);
        }

        [Test]
        public void Calculate_ZeroAPR_MonthlyPaymentIsEqualDivision()
        {
            var zeroAprConfig = CreateLoanConfig(
                apr: 0f,
                termMonths: 10,
                downPaymentPercent: 0.20f,
                minCreditScore: 600);

            float lotPrice = 10000f;
            var result = LoanDisplayCalculator.Calculate(lotPrice, zeroAprConfig);

            // 8000 principal / 10 months = 800 per month
            Assert.AreEqual(800f, result.MonthlyPayment, 0.01f);

            // Total cost = (800 * 10) + 2000 = 10000, no interest
            Assert.AreEqual(10000f, result.TotalCost, 0.01f);

            Object.DestroyImmediate(zeroAprConfig);
        }

        [Test]
        public void Calculate_ZeroLotPrice_AllValuesZero()
        {
            var result = LoanDisplayCalculator.Calculate(0f, _config);

            Assert.AreEqual(0f, result.Principal, 0.01f);
            Assert.AreEqual(0f, result.DownPayment, 0.01f);
            Assert.AreEqual(0f, result.MonthlyPayment, 0.01f);
        }

        [Test]
        public void Calculate_FullDownPayment_NoPrincipalNoPayment()
        {
            var fullDownConfig = CreateLoanConfig(
                apr: 0.08f,
                termMonths: 12,
                downPaymentPercent: 1.0f,
                minCreditScore: 600);

            float lotPrice = 10000f;
            var result = LoanDisplayCalculator.Calculate(lotPrice, fullDownConfig);

            Assert.AreEqual(10000f, result.DownPayment, 0.01f);
            Assert.AreEqual(0f, result.Principal, 0.01f);
            Assert.AreEqual(0f, result.MonthlyPayment, 0.01f);

            Object.DestroyImmediate(fullDownConfig);
        }

        [Test]
        public void Calculate_HighAPR_MonthlyPaymentIsHigher()
        {
            var highAprConfig = CreateLoanConfig(
                apr: 0.25f,
                termMonths: 12,
                downPaymentPercent: 0.20f,
                minCreditScore: 600);

            float lotPrice = 10000f;
            var normalResult = LoanDisplayCalculator.Calculate(lotPrice, _config);
            var highResult = LoanDisplayCalculator.Calculate(lotPrice, highAprConfig);

            // Higher APR means higher monthly payment for same principal
            Assert.Greater(highResult.MonthlyPayment, normalResult.MonthlyPayment);
            Assert.Greater(highResult.TotalCost, normalResult.TotalCost);

            Object.DestroyImmediate(highAprConfig);
        }

        // ===============================================================
        // HELPER
        // ===============================================================

        private static LoanConfig CreateLoanConfig(
            float apr, int termMonths, float downPaymentPercent, int minCreditScore)
        {
            var config = ScriptableObject.CreateInstance<LoanConfig>();
            config.name = "TestLoan";

            var so = new UnityEditor.SerializedObject(config);
            so.FindProperty("_apr").floatValue = apr;
            so.FindProperty("_termMonths").intValue = termMonths;
            so.FindProperty("_downPaymentPercent").floatValue = downPaymentPercent;
            so.FindProperty("_minimumCreditScore").intValue = minCreditScore;
            so.FindProperty("_displayName").stringValue = "Test Loan";
            so.FindProperty("_loanId").stringValue = "test_loan";
            so.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }
    }
}
