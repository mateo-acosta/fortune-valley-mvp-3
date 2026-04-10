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
    public class InsuranceFilterLogicTests
    {
        // ─── helpers ────────────────────────────────────────────────────

        private List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
                Object.DestroyImmediate(obj);
            _created.Clear();
        }

        private static ActiveInsurancePolicy MakePolicy(
            string lotId, InsurancePolicyType type,
            string policyId = "test_policy", bool active = true)
        {
            var policy = new ActiveInsurancePolicy(
                policyId, lotId, type, 50f, 200f, 0.8f,
                new List<string> { "fire" }, 0);
            if (!active) policy.Deactivate();
            return policy;
        }

        private InsurancePolicyConfig MakeConfig(
            string policyId, InsurancePolicyType type)
        {
            var config = ScriptableObject.CreateInstance<InsurancePolicyConfig>();
            _created.Add(config);

            var flags = System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance;
            var t = typeof(InsurancePolicyConfig);
            t.GetField("_policyId", flags)?.SetValue(config, policyId);
            t.GetField("_displayName", flags)?.SetValue(config, policyId);
            t.GetField("_policyType", flags)?.SetValue(config, type);
            t.GetField("_monthlyPremium", flags)?.SetValue(config, 50f);
            t.GetField("_deductible", flags)?.SetValue(config, 200f);
            t.GetField("_coveragePercent", flags)?.SetValue(config, 0.8f);
            t.GetField("_coveredAccidents", flags)?.SetValue(config, new List<AccidentDefinition>());

            return config;
        }

        // ─── FilterActivePolicies ───────────────────────────────────────

        [Test]
        public void FilterActivePolicies_NoFilters_ReturnsAllActive()
        {
            var policies = new List<ActiveInsurancePolicy>
            {
                MakePolicy("lot_1", InsurancePolicyType.GeneralProtection),
                MakePolicy("lot_2", InsurancePolicyType.NonGeneralProtection, "p2")
            };

            var result = InsuranceFilterLogic.FilterActivePolicies(policies, null, null);

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void FilterActivePolicies_NullInput_ReturnsEmpty()
        {
            var result = InsuranceFilterLogic.FilterActivePolicies(null, null, null);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void FilterActivePolicies_ExcludesInactive()
        {
            var policies = new List<ActiveInsurancePolicy>
            {
                MakePolicy("lot_1", InsurancePolicyType.GeneralProtection, active: true),
                MakePolicy("lot_1", InsurancePolicyType.NonGeneralProtection, "p2", active: false)
            };

            var result = InsuranceFilterLogic.FilterActivePolicies(policies, null, null);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void FilterActivePolicies_ByPolicyType_FiltersCorrectly()
        {
            var policies = new List<ActiveInsurancePolicy>
            {
                MakePolicy("lot_1", InsurancePolicyType.GeneralProtection),
                MakePolicy("lot_2", InsurancePolicyType.NonGeneralProtection, "p2")
            };

            var result = InsuranceFilterLogic.FilterActivePolicies(
                policies, InsurancePolicyType.GeneralProtection, null);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(InsurancePolicyType.GeneralProtection, result[0].PolicyType);
        }

        [Test]
        public void FilterActivePolicies_ByLot_FiltersCorrectly()
        {
            var policies = new List<ActiveInsurancePolicy>
            {
                MakePolicy("lot_1", InsurancePolicyType.GeneralProtection),
                MakePolicy("lot_2", InsurancePolicyType.GeneralProtection, "p2")
            };

            var result = InsuranceFilterLogic.FilterActivePolicies(
                policies, null, "lot_1");

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("lot_1", result[0].LotId);
        }

        [Test]
        public void FilterActivePolicies_CombinedFilters_ANDed()
        {
            var policies = new List<ActiveInsurancePolicy>
            {
                MakePolicy("lot_1", InsurancePolicyType.GeneralProtection),
                MakePolicy("lot_1", InsurancePolicyType.NonGeneralProtection, "p2"),
                MakePolicy("lot_2", InsurancePolicyType.GeneralProtection, "p3")
            };

            var result = InsuranceFilterLogic.FilterActivePolicies(
                policies, InsurancePolicyType.GeneralProtection, "lot_1");

            Assert.AreEqual(1, result.Count);
        }

        // ─── FilterPolicyConfigs ────────────────────────────────────────

        [Test]
        public void FilterPolicyConfigs_NoFilters_ReturnsAll()
        {
            var configs = new List<InsurancePolicyConfig>
            {
                MakeConfig("general", InsurancePolicyType.GeneralProtection),
                MakeConfig("nongeneral", InsurancePolicyType.NonGeneralProtection)
            };
            var ownedLots = new List<string> { "lot_1" };
            var coverageMap = new Dictionary<string, HashSet<InsurancePolicyType>>();

            var result = InsuranceFilterLogic.FilterPolicyConfigs(
                configs, null, null, coverageMap, ownedLots);

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void FilterPolicyConfigs_FullyCovered_MarkedCorrectly()
        {
            var configs = new List<InsurancePolicyConfig>
            {
                MakeConfig("general", InsurancePolicyType.GeneralProtection)
            };
            var ownedLots = new List<string> { "lot_1", "lot_2" };
            var coverageMap = new Dictionary<string, HashSet<InsurancePolicyType>>
            {
                { "lot_1", new HashSet<InsurancePolicyType> { InsurancePolicyType.GeneralProtection } },
                { "lot_2", new HashSet<InsurancePolicyType> { InsurancePolicyType.GeneralProtection } }
            };

            var result = InsuranceFilterLogic.FilterPolicyConfigs(
                configs, null, null, coverageMap, ownedLots);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result[0].IsFullyCovered);
        }

        [Test]
        public void FilterPolicyConfigs_PartialCoverage_NotFullyCovered()
        {
            var configs = new List<InsurancePolicyConfig>
            {
                MakeConfig("general", InsurancePolicyType.GeneralProtection)
            };
            var ownedLots = new List<string> { "lot_1", "lot_2" };
            var coverageMap = new Dictionary<string, HashSet<InsurancePolicyType>>
            {
                { "lot_1", new HashSet<InsurancePolicyType> { InsurancePolicyType.GeneralProtection } }
                // lot_2 has no coverage
            };

            var result = InsuranceFilterLogic.FilterPolicyConfigs(
                configs, null, null, coverageMap, ownedLots);

            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[0].IsFullyCovered);
        }

        [Test]
        public void FilterPolicyConfigs_CoverageStatusFilter_Available_ExcludesFullyCovered()
        {
            var configs = new List<InsurancePolicyConfig>
            {
                MakeConfig("general", InsurancePolicyType.GeneralProtection),
                MakeConfig("nongeneral", InsurancePolicyType.NonGeneralProtection)
            };
            var ownedLots = new List<string> { "lot_1" };
            var coverageMap = new Dictionary<string, HashSet<InsurancePolicyType>>
            {
                { "lot_1", new HashSet<InsurancePolicyType> { InsurancePolicyType.GeneralProtection } }
            };

            var result = InsuranceFilterLogic.FilterPolicyConfigs(
                configs, null, InsuranceCoverageStatus.Available, coverageMap, ownedLots);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("nongeneral", result[0].Config.PolicyId);
        }

        [Test]
        public void FilterPolicyConfigs_NoOwnedLots_NoneFullyCovered()
        {
            var configs = new List<InsurancePolicyConfig>
            {
                MakeConfig("general", InsurancePolicyType.GeneralProtection)
            };

            var result = InsuranceFilterLogic.FilterPolicyConfigs(
                configs, null, null,
                new Dictionary<string, HashSet<InsurancePolicyType>>(),
                new List<string>());

            Assert.AreEqual(1, result.Count);
            Assert.IsFalse(result[0].IsFullyCovered);
        }

        // ─── FilterInsuranceTransactions ────────────────────────────────

        [Test]
        public void FilterTransactions_NoFilters_ReturnsAll()
        {
            var records = new List<TransactionRecord>
            {
                new TransactionRecord(TransactionType.InsurancePurchased, "test", 0f, 1, "lot_1"),
                new TransactionRecord(TransactionType.AccidentResolved, "test", 100f, 2, "lot_2")
            };

            var result = InsuranceFilterLogic.FilterInsuranceTransactions(records, null, null);

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void FilterTransactions_ByType_FiltersCorrectly()
        {
            var records = new List<TransactionRecord>
            {
                new TransactionRecord(TransactionType.InsurancePurchased, "test", 0f, 1, "lot_1"),
                new TransactionRecord(TransactionType.AccidentResolved, "test", 100f, 2, "lot_1")
            };

            var result = InsuranceFilterLogic.FilterInsuranceTransactions(
                records, TransactionType.AccidentResolved, null);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(TransactionType.AccidentResolved, result[0].Type);
        }

        [Test]
        public void FilterTransactions_ByEntityId_FiltersCorrectly()
        {
            var records = new List<TransactionRecord>
            {
                new TransactionRecord(TransactionType.PremiumCharged, "test", 50f, 1, "lot_1"),
                new TransactionRecord(TransactionType.PremiumCharged, "test", 50f, 2, "lot_2")
            };

            var result = InsuranceFilterLogic.FilterInsuranceTransactions(
                records, null, "lot_1");

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("lot_1", result[0].EntityId);
        }

        [Test]
        public void FilterTransactions_NullInput_ReturnsEmpty()
        {
            var result = InsuranceFilterLogic.FilterInsuranceTransactions(null, null, null);
            Assert.AreEqual(0, result.Count);
        }

        // ─── BuildCoverageMap ───────────────────────────────────────────

        [Test]
        public void BuildCoverageMap_EmptyList_ReturnsEmptyMap()
        {
            var map = InsuranceFilterLogic.BuildCoverageMap(new List<ActiveInsurancePolicy>());
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void BuildCoverageMap_NullList_ReturnsEmptyMap()
        {
            var map = InsuranceFilterLogic.BuildCoverageMap(null);
            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void BuildCoverageMap_InactivePolicies_Excluded()
        {
            var policies = new List<ActiveInsurancePolicy>
            {
                MakePolicy("lot_1", InsurancePolicyType.GeneralProtection, active: false)
            };

            var map = InsuranceFilterLogic.BuildCoverageMap(policies);

            Assert.AreEqual(0, map.Count);
        }

        [Test]
        public void BuildCoverageMap_MultiplePoliciesOnSameLot_BothInSet()
        {
            var policies = new List<ActiveInsurancePolicy>
            {
                MakePolicy("lot_1", InsurancePolicyType.GeneralProtection),
                MakePolicy("lot_1", InsurancePolicyType.NonGeneralProtection, "p2")
            };

            var map = InsuranceFilterLogic.BuildCoverageMap(policies);

            Assert.AreEqual(1, map.Count);
            Assert.IsTrue(map["lot_1"].Contains(InsurancePolicyType.GeneralProtection));
            Assert.IsTrue(map["lot_1"].Contains(InsurancePolicyType.NonGeneralProtection));
        }

        [Test]
        public void BuildCoverageMap_ActiveAndInactiveSameLot_OnlyActiveIncluded()
        {
            var active = MakePolicy("lot_1", InsurancePolicyType.GeneralProtection);
            var inactive = MakePolicy("lot_1", InsurancePolicyType.NonGeneralProtection, "p2", active: false);

            var policies = new List<ActiveInsurancePolicy> { active, inactive };
            var map = InsuranceFilterLogic.BuildCoverageMap(policies);

            Assert.AreEqual(1, map.Count);
            Assert.IsTrue(map["lot_1"].Contains(InsurancePolicyType.GeneralProtection));
            Assert.IsFalse(map["lot_1"].Contains(InsurancePolicyType.NonGeneralProtection));
        }
    }
}
