using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.UI;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LoanEligibilityFilterTests
    {
        private LoanConfig _starterLoan;  // min score 550, max DTI 0.50
        private LoanConfig _standardLoan; // min score 650, max DTI 0.40
        private LoanConfig _premiumLoan;  // min score 750, max DTI 0.30

        [SetUp]
        public void SetUp()
        {
            _starterLoan = CreateLoanConfig("Starter", 550, 0.50f);
            _standardLoan = CreateLoanConfig("Standard", 650, 0.40f);
            _premiumLoan = CreateLoanConfig("Premium", 750, 0.30f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_starterLoan);
            Object.DestroyImmediate(_standardLoan);
            Object.DestroyImmediate(_premiumLoan);
        }

        [Test]
        public void HighScore_LowDTI_AllEligible()
        {
            var configs = new List<LoanConfig> { _starterLoan, _standardLoan, _premiumLoan };

            var results = LoanEligibilityFilter.Evaluate(configs, 800, 0.10f);

            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results[0].IsEligible);
            Assert.IsTrue(results[1].IsEligible);
            Assert.IsTrue(results[2].IsEligible);
        }

        [Test]
        public void BelowMinScore_Ineligible()
        {
            var configs = new List<LoanConfig> { _premiumLoan };

            var results = LoanEligibilityFilter.Evaluate(configs, 700, 0.10f);

            Assert.AreEqual(1, results.Count);
            Assert.IsFalse(results[0].IsEligible);
            Assert.IsTrue(results[0].Reason.Contains("750"));
        }

        [Test]
        public void ExactMinScore_IsEligible()
        {
            var configs = new List<LoanConfig> { _standardLoan };

            var results = LoanEligibilityFilter.Evaluate(configs, 650, 0.10f);

            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results[0].IsEligible);
        }

        [Test]
        public void HighDTI_Ineligible()
        {
            var configs = new List<LoanConfig> { _premiumLoan };

            var results = LoanEligibilityFilter.Evaluate(configs, 800, 0.50f);

            Assert.AreEqual(1, results.Count);
            Assert.IsFalse(results[0].IsEligible);
            Assert.IsTrue(results[0].Reason.Contains("DTI"));
        }

        [Test]
        public void MixedEligibility_CorrectPerConfig()
        {
            var configs = new List<LoanConfig> { _starterLoan, _standardLoan, _premiumLoan };

            // Score 660: qualifies for Starter (550) and Standard (650), not Premium (750)
            var results = LoanEligibilityFilter.Evaluate(configs, 660, 0.10f);

            Assert.AreEqual(3, results.Count);
            Assert.IsTrue(results[0].IsEligible);   // Starter
            Assert.IsTrue(results[1].IsEligible);   // Standard
            Assert.IsFalse(results[2].IsEligible);  // Premium
        }

        [Test]
        public void ZeroDTI_DoesNotDisqualify()
        {
            var configs = new List<LoanConfig> { _standardLoan };

            var results = LoanEligibilityFilter.Evaluate(configs, 700, 0f);

            Assert.IsTrue(results[0].IsEligible);
        }

        [Test]
        public void NullConfigs_ReturnsEmptyList()
        {
            var results = LoanEligibilityFilter.Evaluate(null, 700, 0.10f);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void EligibleResult_HasEmptyReason()
        {
            var configs = new List<LoanConfig> { _starterLoan };

            var results = LoanEligibilityFilter.Evaluate(configs, 700, 0.10f);

            Assert.IsTrue(results[0].IsEligible);
            Assert.AreEqual(string.Empty, results[0].Reason);
        }

        // Helper to create a LoanConfig ScriptableObject for testing
        private static LoanConfig CreateLoanConfig(string name, int minScore, float maxDti)
        {
            var config = ScriptableObject.CreateInstance<LoanConfig>();
            config.name = name;

            // Use SerializedObject to set private serialized fields
            var so = new UnityEditor.SerializedObject(config);
            so.FindProperty("_minimumCreditScore").intValue = minScore;
            so.FindProperty("_maxDtiRatio").floatValue = maxDti;
            so.FindProperty("_displayName").stringValue = name;
            so.FindProperty("_loanId").stringValue = name.ToLower();
            so.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }
    }
}
