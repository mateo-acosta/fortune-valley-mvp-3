using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for InsuranceSystem event handling and accident resolution.
    /// Skipped while FeatureFlags.InsuranceEnabled is false (POC scope).
    /// Remove the [Ignore] attribute when flipping the flag back on.
    /// </summary>
    [TestFixture]
    [Ignore("Insurance disabled for POC. Re-enable FeatureFlags.InsuranceEnabled and remove this Ignore to run.")]
    public class InsuranceSystemTests
    {
        private GameObject _rootGO;
        private InsuranceSystem _system;
        private InsurancePolicyConfig _generalConfig;
        private InsurancePolicyConfig _nonGeneralConfig;
        private AccidentDefinition _fireAccident;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();

            _rootGO = new GameObject("TestRoot");

            // Create accident definition
            _fireAccident = ScriptableObject.CreateInstance<AccidentDefinition>();
            SetField(_fireAccident, "_accidentId", "fire");
            SetField(_fireAccident, "_displayName", "Fire");
            SetField(_fireAccident, "_baseDamageCost", 1000f);
            SetField(_fireAccident, "_category", AccidentCategory.General);

            // Create general policy config
            _generalConfig = ScriptableObject.CreateInstance<InsurancePolicyConfig>();
            SetField(_generalConfig, "_policyId", "general_1");
            SetField(_generalConfig, "_displayName", "General Protection");
            SetField(_generalConfig, "_policyType", InsurancePolicyType.GeneralProtection);
            SetField(_generalConfig, "_monthlyPremium", 50f);
            SetField(_generalConfig, "_deductible", 200f);
            SetField(_generalConfig, "_coveragePercent", 0.8f);
            SetField(_generalConfig, "_coveredAccidents", new List<AccidentDefinition> { _fireAccident });

            // Create non-general policy config
            _nonGeneralConfig = ScriptableObject.CreateInstance<InsurancePolicyConfig>();
            SetField(_nonGeneralConfig, "_policyId", "nongeneral_1");
            SetField(_nonGeneralConfig, "_displayName", "Non-General Protection");
            SetField(_nonGeneralConfig, "_policyType", InsurancePolicyType.NonGeneralProtection);
            SetField(_nonGeneralConfig, "_monthlyPremium", 100f);
            SetField(_nonGeneralConfig, "_deductible", 500f);
            SetField(_nonGeneralConfig, "_coveragePercent", 0.9f);
            SetField(_nonGeneralConfig, "_coveredAccidents", new List<AccidentDefinition>());

            // Create system
            _system = _rootGO.AddComponent<InsuranceSystem>();
            var policyList = new List<InsurancePolicyConfig> { _generalConfig, _nonGeneralConfig };
            SetField(_system, "_availablePolicies", policyList);

            // Manually invoke OnEnable
            InvokePrivate(_system, "OnEnable");

            // Initialize via game start
            GameEvents.RaiseGameStart();
        }

        [TearDown]
        public void TearDown()
        {
            InvokePrivate(_system, "OnDisable");
            Object.DestroyImmediate(_rootGO);
            Object.DestroyImmediate(_generalConfig);
            Object.DestroyImmediate(_nonGeneralConfig);
            Object.DestroyImmediate(_fireAccident);
            GameEvents.ClearAllSubscriptions();
        }

        // ===============================================================
        // PURCHASE TESTS
        // ===============================================================

        [Test]
        public void PurchaseInsurance_ValidPolicy_Succeeds()
        {
            bool eventFired = false;
            GameEvents.OnInsurancePurchased += (lotId, policyId) => eventFired = true;

            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "general_1");

            Assert.IsTrue(eventFired);
            Assert.IsTrue(_system.Portfolio.HasPolicy("lot_1", InsurancePolicyType.GeneralProtection));
        }

        [Test]
        public void PurchaseInsurance_DuplicatePolicy_Rejected()
        {
            int eventCount = 0;
            GameEvents.OnInsurancePurchased += (lotId, policyId) => eventCount++;

            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "general_1");
            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "general_1");

            Assert.AreEqual(1, eventCount, "Second purchase should be rejected");
        }

        [Test]
        public void PurchaseInsurance_UnknownPolicyId_NoOp()
        {
            bool eventFired = false;
            GameEvents.OnInsurancePurchased += (lotId, policyId) => eventFired = true;

            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "nonexistent_policy");

            Assert.IsFalse(eventFired);
        }

        // ===============================================================
        // CANCEL TESTS
        // ===============================================================

        [Test]
        public void CancelInsurance_ActivePolicy_Succeeds()
        {
            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "general_1");

            bool cancelFired = false;
            GameEvents.OnInsuranceCanceled += (lotId, type) => cancelFired = true;

            GameEvents.RaiseCancelInsuranceRequested("lot_1", InsurancePolicyType.GeneralProtection);

            Assert.IsTrue(cancelFired);
            Assert.IsFalse(_system.Portfolio.HasPolicy("lot_1", InsurancePolicyType.GeneralProtection));
        }

        [Test]
        public void CancelInsurance_NoPolicy_NoEvent()
        {
            bool cancelFired = false;
            GameEvents.OnInsuranceCanceled += (lotId, type) => cancelFired = true;

            GameEvents.RaiseCancelInsuranceRequested("lot_1", InsurancePolicyType.GeneralProtection);

            Assert.IsFalse(cancelFired);
        }

        // ===============================================================
        // ACCIDENT RESOLUTION TESTS
        // ===============================================================

        [Test]
        public void AccidentResolved_Insured_PaysDeductible()
        {
            // Purchase insurance first
            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "general_1");

            float resolvedCost = 0f;
            bool wasCovered = false;
            GameEvents.OnAccidentResolved += (lotId, accName, totalDamage, covered, cost) =>
            {
                resolvedCost = cost;
                wasCovered = covered;
            };

            // Trigger accident
            var accident = new AccidentRollResult("lot_1", "fire", "Fire", 1000f);
            GameEvents.RaiseAccidentOccurred(accident);

            Assert.IsTrue(wasCovered);
            Assert.AreEqual(200f, resolvedCost, 0.01f, "Should pay deductible (200), not full cost");
        }

        [Test]
        public void AccidentResolved_Uninsured_PaysFullCost()
        {
            float resolvedCost = 0f;
            bool wasCovered = false;
            GameEvents.OnAccidentResolved += (lotId, accName, totalDamage, covered, cost) =>
            {
                resolvedCost = cost;
                wasCovered = covered;
            };

            var accident = new AccidentRollResult("lot_1", "fire", "Fire", 1000f);
            GameEvents.RaiseAccidentOccurred(accident);

            Assert.IsFalse(wasCovered);
            Assert.AreEqual(1000f, resolvedCost, 0.01f);
        }

        [Test]
        public void AccidentResolved_ChargesCreditCard()
        {
            float chargedAmount = 0f;
            GameEvents.OnCreditCardChargeRequested += (amount, reason) => chargedAmount = amount;

            var accident = new AccidentRollResult("lot_1", "fire", "Fire", 1000f);
            GameEvents.RaiseAccidentOccurred(accident);

            Assert.AreEqual(1000f, chargedAmount, 0.01f);
        }

        [Test]
        public void AccidentResolved_Insured_ChargesDeductibleToCC()
        {
            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "general_1");

            float chargedAmount = 0f;
            GameEvents.OnCreditCardChargeRequested += (amount, reason) => chargedAmount = amount;

            var accident = new AccidentRollResult("lot_1", "fire", "Fire", 1000f);
            GameEvents.RaiseAccidentOccurred(accident);

            Assert.AreEqual(200f, chargedAmount, 0.01f, "Should charge deductible to CC");
        }

        // ===============================================================
        // PREMIUM CHARGING TESTS
        // ===============================================================

        [Test]
        public void ChargePremiums_ChargesAllActivePolicies()
        {
            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "general_1");
            GameEvents.RaisePurchaseInsuranceRequested("lot_2", "nongeneral_1");

            float totalCharged = 0f;
            GameEvents.OnCreditCardChargeRequested += (amount, reason) => totalCharged += amount;

            _system.ChargePremiums();

            // general = 50, nongeneral = 100
            Assert.AreEqual(150f, totalCharged, 0.01f);
        }

        [Test]
        public void ChargePremiums_SkipsCanceledPolicies()
        {
            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "general_1");
            GameEvents.RaiseCancelInsuranceRequested("lot_1", InsurancePolicyType.GeneralProtection);

            float totalCharged = 0f;
            GameEvents.OnCreditCardChargeRequested += (amount, reason) => totalCharged += amount;

            _system.ChargePremiums();

            Assert.AreEqual(0f, totalCharged, 0.01f);
        }

        // ===============================================================
        // CANCELLATION FEE TESTS
        // ===============================================================

        [Test]
        public void CancelInsurance_ChargesCancellationFeeToCC()
        {
            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "general_1");

            float feeCharged = 0f;
            string feeReason = null;
            GameEvents.OnCreditCardChargeRequested += (amount, reason) =>
            {
                feeCharged = amount;
                feeReason = reason;
            };

            GameEvents.RaiseCancelInsuranceRequested("lot_1", InsurancePolicyType.GeneralProtection);

            // General policy premium is 50, fee is 50% = 25
            Assert.AreEqual(25f, feeCharged, 0.01f);
            Assert.IsTrue(feeReason.Contains("cancellation fee"));
        }

        [Test]
        public void CancelInsurance_FiresCanceledEvent_AfterFee()
        {
            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "general_1");

            bool cancelFired = false;
            string canceledLotId = null;
            InsurancePolicyType canceledType = InsurancePolicyType.GeneralProtection;

            GameEvents.OnInsuranceCanceled += (lotId, type) =>
            {
                cancelFired = true;
                canceledLotId = lotId;
                canceledType = type;
            };

            GameEvents.RaiseCancelInsuranceRequested("lot_1", InsurancePolicyType.GeneralProtection);

            Assert.IsTrue(cancelFired);
            Assert.AreEqual("lot_1", canceledLotId);
            Assert.AreEqual(InsurancePolicyType.GeneralProtection, canceledType);
            Assert.IsFalse(_system.Portfolio.HasPolicy("lot_1", InsurancePolicyType.GeneralProtection));
        }

        [Test]
        public void CancelInsurance_NoPolicy_NoCCCharge()
        {
            float chargedAmount = 0f;
            GameEvents.OnCreditCardChargeRequested += (amount, reason) => chargedAmount = amount;

            GameEvents.RaiseCancelInsuranceRequested("lot_1", InsurancePolicyType.GeneralProtection);

            Assert.AreEqual(0f, chargedAmount, 0.01f);
        }

        // ===============================================================
        // PREMIUM CHARGED EVENT TESTS
        // ===============================================================

        [Test]
        public void ChargePremiums_FiresPremiumChargedEventPerPolicy()
        {
            GameEvents.RaisePurchaseInsuranceRequested("lot_1", "general_1");
            GameEvents.RaisePurchaseInsuranceRequested("lot_2", "nongeneral_1");

            var premiumEvents = new List<(string lotId, string policyId, float amount)>();
            GameEvents.OnInsurancePremiumCharged += (lotId, policyId, amount) =>
                premiumEvents.Add((lotId, policyId, amount));

            _system.ChargePremiums();

            Assert.AreEqual(2, premiumEvents.Count);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            throw new System.Exception($"Field '{fieldName}' not found on {target.GetType().Name}");
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(target, null);
        }
    }
}
