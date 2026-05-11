using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.UI;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class InsuranceDetailFormatterTests
    {
        private List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                Object.DestroyImmediate(obj);
            _created.Clear();
        }

        // ─── CalculateBestCoverageComparison ────────────────────────────

        [Test]
        public void Comparison_NoPolicies_HasComparisonFalse()
        {
            var result = InsuranceDetailFormatter.CalculateBestCoverageComparison(
                1000f, 1000f, false, null);

            Assert.IsFalse(result.HasComparison);
        }

        [Test]
        public void Comparison_EmptyPolicies_HasComparisonFalse()
        {
            var result = InsuranceDetailFormatter.CalculateBestCoverageComparison(
                1000f, 1000f, false, new List<InsurancePolicyConfig>());

            Assert.IsFalse(result.HasComparison);
        }

        [Test]
        public void Comparison_OnePolicy_ReturnsThatPolicysDeductible()
        {
            var configs = new List<InsurancePolicyConfig>
            {
                MakeConfig("policy_1", 200f)
            };

            var result = InsuranceDetailFormatter.CalculateBestCoverageComparison(
                1000f, 1000f, false, configs);

            Assert.IsTrue(result.HasComparison);
            Assert.AreEqual("$200.00", result.WouldHavePaid);
            Assert.AreEqual("policy_1", result.BestPolicyName);
        }

        [Test]
        public void Comparison_MultiplePolicies_PicksCheapestDeductible()
        {
            var configs = new List<InsurancePolicyConfig>
            {
                MakeConfig("expensive", 500f),
                MakeConfig("cheap", 100f),
                MakeConfig("medium", 300f)
            };

            var result = InsuranceDetailFormatter.CalculateBestCoverageComparison(
                1000f, 1000f, false, configs);

            Assert.IsTrue(result.HasComparison);
            Assert.AreEqual("cheap", result.BestPolicyName);
            Assert.AreEqual("$100.00", result.WouldHavePaid);
        }

        [Test]
        public void Comparison_DamageLessThanDeductible_CappedAtDamage()
        {
            var configs = new List<InsurancePolicyConfig>
            {
                MakeConfig("policy_1", 500f)
            };

            // Damage is only $100, deductible is $500
            var result = InsuranceDetailFormatter.CalculateBestCoverageComparison(
                100f, 100f, false, configs);

            Assert.IsTrue(result.HasComparison);
            Assert.AreEqual("$100.00", result.WouldHavePaid);
        }

        [Test]
        public void Comparison_WasCovered_ShowsInsuredMessage()
        {
            var configs = new List<InsurancePolicyConfig>
            {
                MakeConfig("policy_1", 200f)
            };

            var result = InsuranceDetailFormatter.CalculateBestCoverageComparison(
                1000f, 200f, true, configs);

            Assert.IsTrue(result.HasComparison);
            Assert.IsTrue(result.WasCovered);
            Assert.IsTrue(result.ComparisonText.Contains("Without insurance"));
        }

        [Test]
        public void Comparison_WasNotCovered_ShowsUninsuredMessage()
        {
            var configs = new List<InsurancePolicyConfig>
            {
                MakeConfig("General Protection", 200f)
            };

            var result = InsuranceDetailFormatter.CalculateBestCoverageComparison(
                1000f, 1000f, false, configs);

            Assert.IsTrue(result.HasComparison);
            Assert.IsFalse(result.WasCovered);
            Assert.IsTrue(result.ComparisonText.Contains("General Protection"));
            Assert.IsTrue(result.ComparisonText.Contains("$200.00"));
        }

        // ─── FormatOwnedPolicy ──────────────────────────────────────────

        [Test]
        public void FormatOwnedPolicy_ProducesExpectedFields()
        {
            var policy = new ActiveInsurancePolicy(
                "gen_1", "lot_1", InsurancePolicyType.GeneralProtection,
                50f, 200f, 0.8f, new List<string> { "fire", "theft" }, 0);

            var details = InsuranceDetailFormatter.FormatOwnedPolicy(policy, "Lot Alpha");

            Assert.AreEqual("gen_1", details.PolicyName);
            Assert.AreEqual("Lot Alpha", details.LotName);
            Assert.AreEqual("$50.00/mo", details.Premium);
            Assert.AreEqual("$200.00", details.Deductible);
            Assert.AreEqual("80%", details.Coverage);
            Assert.AreEqual("Active", details.Status);
            Assert.AreEqual(2, details.CoveredAccidentIds.Count);
        }

        // ─── FormatTransaction ──────────────────────────────────────────

        [Test]
        public void FormatTransaction_AccidentResolved_TypeLabelIsAccident()
        {
            var record = new TransactionRecord(
                TransactionType.AccidentResolved, "Fire at lot_1", 500f, 1, "lot_1");

            var details = InsuranceDetailFormatter.FormatTransaction(record);

            Assert.AreEqual("Accident", details.TypeLabel);
            Assert.AreEqual("lot_1", details.LotId);
        }

        [Test]
        public void FormatTransaction_PremiumCharged_TypeLabelIsPremiumCharge()
        {
            var record = new TransactionRecord(
                TransactionType.PremiumCharged, "Premium charged", 50f, 1, "lot_1");

            var details = InsuranceDetailFormatter.FormatTransaction(record);

            Assert.AreEqual("Premium Charge", details.TypeLabel);
        }

        [Test]
        public void FormatTransaction_NullEntityId_ShowsNA()
        {
            var record = new TransactionRecord(
                TransactionType.InsurancePurchased, "test", 0f, 1);

            var details = InsuranceDetailFormatter.FormatTransaction(record);

            Assert.AreEqual("N/A", details.LotId);
        }

        // ─── helpers ────────────────────────────────────────────────────

        private InsurancePolicyConfig MakeConfig(string name, float deductible)
        {
            var config = ScriptableObject.CreateInstance<InsurancePolicyConfig>();
            _created.Add(config);

            var flags = System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance;
            var t = typeof(InsurancePolicyConfig);
            t.GetField("_policyId", flags)?.SetValue(config, name);
            t.GetField("_displayName", flags)?.SetValue(config, name);
            t.GetField("_policyType", flags)?.SetValue(config, InsurancePolicyType.GeneralProtection);
            t.GetField("_monthlyPremium", flags)?.SetValue(config, 50f);
            t.GetField("_deductible", flags)?.SetValue(config, deductible);
            t.GetField("_coveragePercent", flags)?.SetValue(config, 0.8f);
            t.GetField("_coveredAccidents", flags)?.SetValue(config, new List<AccidentDefinition>());

            return config;
        }
    }
}
