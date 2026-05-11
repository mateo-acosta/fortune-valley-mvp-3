using NUnit.Framework;
using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for InsurancePortfolio collection management.
    /// Pure C# class, no Unity lifecycle needed.
    /// </summary>
    [TestFixture]
    public class InsurancePortfolioTests
    {
        private InsurancePortfolio _portfolio;

        [SetUp]
        public void SetUp()
        {
            _portfolio = new InsurancePortfolio();
        }

        // ===============================================================
        // ADD TESTS
        // ===============================================================

        [Test]
        public void Add_NewPolicy_Succeeds()
        {
            var policy = MakePolicy("lot_1", InsurancePolicyType.GeneralProtection);

            bool result = _portfolio.Add(policy);

            Assert.IsTrue(result);
            Assert.AreEqual(1, _portfolio.AllPolicies.Count);
        }

        [Test]
        public void Add_DuplicatePolicyOnSameLot_Rejected()
        {
            var policy1 = MakePolicy("lot_1", InsurancePolicyType.GeneralProtection);
            var policy2 = MakePolicy("lot_1", InsurancePolicyType.GeneralProtection, "policy_dup");

            _portfolio.Add(policy1);
            bool result = _portfolio.Add(policy2);

            Assert.IsFalse(result);
            Assert.AreEqual(1, _portfolio.AllPolicies.Count);
        }

        [Test]
        public void Add_DifferentTypeOnSameLot_Succeeds()
        {
            var general = MakePolicy("lot_1", InsurancePolicyType.GeneralProtection);
            var nonGeneral = MakePolicy("lot_1", InsurancePolicyType.NonGeneralProtection, "ng_policy");

            _portfolio.Add(general);
            bool result = _portfolio.Add(nonGeneral);

            Assert.IsTrue(result);
            Assert.AreEqual(2, _portfolio.AllPolicies.Count);
        }

        [Test]
        public void Add_SameTypeOnDifferentLots_Succeeds()
        {
            var policy1 = MakePolicy("lot_1", InsurancePolicyType.GeneralProtection);
            var policy2 = MakePolicy("lot_2", InsurancePolicyType.GeneralProtection, "policy_2");

            _portfolio.Add(policy1);
            bool result = _portfolio.Add(policy2);

            Assert.IsTrue(result);
            Assert.AreEqual(2, _portfolio.AllPolicies.Count);
        }

        [Test]
        public void Add_NullPolicy_Rejected()
        {
            bool result = _portfolio.Add(null);
            Assert.IsFalse(result);
        }

        // ===============================================================
        // CANCEL TESTS
        // ===============================================================

        [Test]
        public void Cancel_ExistingPolicy_Succeeds()
        {
            var policy = MakePolicy("lot_1", InsurancePolicyType.GeneralProtection);
            _portfolio.Add(policy);

            bool result = _portfolio.Cancel("lot_1", InsurancePolicyType.GeneralProtection);

            Assert.IsTrue(result);
            Assert.IsFalse(policy.IsActive);
        }

        [Test]
        public void Cancel_NonExistentPolicy_ReturnsFalse()
        {
            bool result = _portfolio.Cancel("lot_1", InsurancePolicyType.GeneralProtection);
            Assert.IsFalse(result);
        }

        [Test]
        public void Cancel_AlreadyCanceled_ReturnsFalse()
        {
            var policy = MakePolicy("lot_1", InsurancePolicyType.GeneralProtection);
            _portfolio.Add(policy);
            _portfolio.Cancel("lot_1", InsurancePolicyType.GeneralProtection);

            bool result = _portfolio.Cancel("lot_1", InsurancePolicyType.GeneralProtection);
            Assert.IsFalse(result);
        }

        // ===============================================================
        // QUERY TESTS
        // ===============================================================

        [Test]
        public void GetForLot_ReturnsOnlyActivePoliciesForThatLot()
        {
            _portfolio.Add(MakePolicy("lot_1", InsurancePolicyType.GeneralProtection));
            _portfolio.Add(MakePolicy("lot_1", InsurancePolicyType.NonGeneralProtection, "ng"));
            _portfolio.Add(MakePolicy("lot_2", InsurancePolicyType.GeneralProtection, "other"));

            var result = _portfolio.GetForLot("lot_1");

            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void GetForLot_ExcludesCanceledPolicies()
        {
            _portfolio.Add(MakePolicy("lot_1", InsurancePolicyType.GeneralProtection));
            _portfolio.Cancel("lot_1", InsurancePolicyType.GeneralProtection);

            var result = _portfolio.GetForLot("lot_1");

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void HasPolicy_ActivePolicy_ReturnsTrue()
        {
            _portfolio.Add(MakePolicy("lot_1", InsurancePolicyType.GeneralProtection));

            Assert.IsTrue(_portfolio.HasPolicy("lot_1", InsurancePolicyType.GeneralProtection));
        }

        [Test]
        public void HasPolicy_NoPolicy_ReturnsFalse()
        {
            Assert.IsFalse(_portfolio.HasPolicy("lot_1", InsurancePolicyType.GeneralProtection));
        }

        // ===============================================================
        // COVERAGE TESTS
        // ===============================================================

        [Test]
        public void FindCoverage_CoveredAccident_ReturnsPolicy()
        {
            var policy = MakePolicy("lot_1", InsurancePolicyType.GeneralProtection,
                coveredAccidentIds: new List<string> { "fire", "flood" });
            _portfolio.Add(policy);

            var result = _portfolio.FindCoverage("lot_1", "fire");

            Assert.IsNotNull(result);
            Assert.AreEqual("fire", result.CoveredAccidentIds[0]);
        }

        [Test]
        public void FindCoverage_UncoveredAccident_ReturnsNull()
        {
            var policy = MakePolicy("lot_1", InsurancePolicyType.GeneralProtection,
                coveredAccidentIds: new List<string> { "fire" });
            _portfolio.Add(policy);

            var result = _portfolio.FindCoverage("lot_1", "earthquake");

            Assert.IsNull(result);
        }

        [Test]
        public void FindCoverage_WrongLot_ReturnsNull()
        {
            var policy = MakePolicy("lot_1", InsurancePolicyType.GeneralProtection,
                coveredAccidentIds: new List<string> { "fire" });
            _portfolio.Add(policy);

            var result = _portfolio.FindCoverage("lot_2", "fire");

            Assert.IsNull(result);
        }

        // ===============================================================
        // PREMIUM TESTS
        // ===============================================================

        [Test]
        public void GetTotalMonthlyPremiums_SumsActivePolicies()
        {
            _portfolio.Add(MakePolicy("lot_1", InsurancePolicyType.GeneralProtection, premium: 50f));
            _portfolio.Add(MakePolicy("lot_2", InsurancePolicyType.GeneralProtection, policyId: "p2", premium: 30f));

            Assert.AreEqual(80f, _portfolio.GetTotalMonthlyPremiums(), 0.01f);
        }

        [Test]
        public void GetTotalMonthlyPremiums_ExcludesCanceled()
        {
            _portfolio.Add(MakePolicy("lot_1", InsurancePolicyType.GeneralProtection, premium: 50f));
            _portfolio.Cancel("lot_1", InsurancePolicyType.GeneralProtection);

            Assert.AreEqual(0f, _portfolio.GetTotalMonthlyPremiums(), 0.01f);
        }

        // ===============================================================
        // CANCELLATION FEE TESTS
        // ===============================================================

        [Test]
        public void GetCancellationFee_ActivePolicy_Returns50Percent()
        {
            _portfolio.Add(MakePolicy("lot_1", InsurancePolicyType.GeneralProtection, premium: 100f));

            float fee = _portfolio.GetCancellationFee("lot_1", InsurancePolicyType.GeneralProtection);

            Assert.AreEqual(50f, fee, 0.01f);
        }

        [Test]
        public void GetCancellationFee_NoMatchingPolicy_ReturnsZero()
        {
            float fee = _portfolio.GetCancellationFee("lot_1", InsurancePolicyType.GeneralProtection);

            Assert.AreEqual(0f, fee, 0.01f);
        }

        [Test]
        public void GetCancellationFee_CanceledPolicy_ReturnsZero()
        {
            _portfolio.Add(MakePolicy("lot_1", InsurancePolicyType.GeneralProtection, premium: 100f));
            _portfolio.Cancel("lot_1", InsurancePolicyType.GeneralProtection);

            float fee = _portfolio.GetCancellationFee("lot_1", InsurancePolicyType.GeneralProtection);

            Assert.AreEqual(0f, fee, 0.01f);
        }

        [Test]
        public void GetCancellationFee_WrongLot_ReturnsZero()
        {
            _portfolio.Add(MakePolicy("lot_1", InsurancePolicyType.GeneralProtection, premium: 100f));

            float fee = _portfolio.GetCancellationFee("lot_2", InsurancePolicyType.GeneralProtection);

            Assert.AreEqual(0f, fee, 0.01f);
        }

        // ===============================================================
        // CLEAR TESTS
        // ===============================================================

        [Test]
        public void Clear_RemovesAllPolicies()
        {
            _portfolio.Add(MakePolicy("lot_1", InsurancePolicyType.GeneralProtection));
            _portfolio.Add(MakePolicy("lot_2", InsurancePolicyType.NonGeneralProtection, "ng"));

            _portfolio.Clear();

            Assert.AreEqual(0, _portfolio.AllPolicies.Count);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private static ActiveInsurancePolicy MakePolicy(
            string lotId,
            InsurancePolicyType type,
            string policyId = "test_policy",
            float premium = 100f,
            float deductible = 250f,
            float coveragePercent = 0.8f,
            List<string> coveredAccidentIds = null)
        {
            return new ActiveInsurancePolicy(
                policyId, lotId, type,
                premium, deductible, coveragePercent,
                coveredAccidentIds ?? new List<string> { "fire", "flood" },
                0
            );
        }
    }
}
