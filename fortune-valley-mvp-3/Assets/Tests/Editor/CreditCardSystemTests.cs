using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for CreditScoreSystem charge handling, statement generation,
    /// and payment processing.
    /// </summary>
    [TestFixture]
    public class CreditScoreSystemTests
    {
        private GameObject _rootGO;
        private CreditScoreSystem _system;
        private CreditCardConfig _config;
        private CreditScoringConfig _scoringConfig;

        private bool _ccFlagBeforeTest;

        [SetUp]
        public void SetUp()
        {
            // CC charge / statement / payment paths only fire when the
            // mechanic is enabled. Flip the flag on for the fixture.
            _ccFlagBeforeTest = FeatureFlags.CreditCardChargesEnabled;
            FeatureFlags.CreditCardChargesEnabled = true;

            GameEvents.ClearAllSubscriptions();

            _rootGO = new GameObject("TestRoot");

            _config = ScriptableObject.CreateInstance<CreditCardConfig>();
            SetField(_config, "_creditLimit", 5000f);
            SetField(_config, "_apr", 0.24f);
            SetField(_config, "_minimumPaymentPercent", 0.02f);
            SetField(_config, "_minimumPaymentFloor", 25f);
            SetField(_config, "_billingCycleDays", 30);

            _scoringConfig = ScriptableObject.CreateInstance<CreditScoringConfig>();
            // Uses defaults: startingScore=650, min=300, max=850

            _system = _rootGO.AddComponent<CreditScoreSystem>();
            SetField(_system, "_config", _config);
            SetField(_system, "_scoringConfig", _scoringConfig);

            // EditMode tests don't run lifecycle methods automatically
            var onEnable = typeof(CreditScoreSystem).GetMethod("OnEnable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnable.Invoke(_system, null);

            // Simulate game start to initialize card state
            GameEvents.RaiseGameStart();
        }

        [TearDown]
        public void TearDown()
        {
            var onDisable = typeof(CreditScoreSystem).GetMethod("OnDisable",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onDisable.Invoke(_system, null);

            Object.DestroyImmediate(_rootGO);
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_scoringConfig);
            GameEvents.ClearAllSubscriptions();

            FeatureFlags.CreditCardChargesEnabled = _ccFlagBeforeTest;
        }

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

        // ===============================================================
        // CHARGE TESTS
        // ===============================================================

        [Test]
        public void Charge_UnderLimit_Succeeds()
        {
            GameEvents.RaiseCreditCardChargeRequested(1000f, "test");

            Assert.AreEqual(1000f, _system.CurrentBalance, 0.01f);
        }

        [Test]
        public void Charge_OverLimit_IsDeclined()
        {
            GameEvents.RaiseCreditCardChargeRequested(6000f, "test");

            Assert.AreEqual(0f, _system.CurrentBalance, 0.01f);
        }

        [Test]
        public void Charge_AtExactLimit_Succeeds()
        {
            GameEvents.RaiseCreditCardChargeRequested(5000f, "test");

            Assert.AreEqual(5000f, _system.CurrentBalance, 0.01f);
        }

        [Test]
        public void Charge_MultipleUnderLimit_Accumulates()
        {
            GameEvents.RaiseCreditCardChargeRequested(2000f, "test1");
            GameEvents.RaiseCreditCardChargeRequested(2000f, "test2");

            Assert.AreEqual(4000f, _system.CurrentBalance, 0.01f);
        }

        [Test]
        public void Charge_WouldExceedLimit_IsDeclined()
        {
            GameEvents.RaiseCreditCardChargeRequested(4000f, "test1");
            GameEvents.RaiseCreditCardChargeRequested(2000f, "test2");

            Assert.AreEqual(4000f, _system.CurrentBalance, 0.01f,
                "Second charge should be declined, balance stays at first charge");
        }

        [Test]
        public void Charge_FiresChargedEvent()
        {
            float receivedAmount = 0f;
            GameEvents.OnCreditCardCharged += (amount) => receivedAmount = amount;

            GameEvents.RaiseCreditCardChargeRequested(500f, "test");

            Assert.AreEqual(500f, receivedAmount);
        }

        // ===============================================================
        // ADVERSARIAL CHARGE TESTS
        // ===============================================================

        [Test]
        public void Charge_ZeroAmount_DoesNothing()
        {
            GameEvents.RaiseCreditCardChargeRequested(0f, "test");
            Assert.AreEqual(0f, _system.CurrentBalance);
        }

        [Test]
        public void Charge_NegativeAmount_DoesNothing()
        {
            GameEvents.RaiseCreditCardChargeRequested(-100f, "test");
            Assert.AreEqual(0f, _system.CurrentBalance);
        }

        [Test]
        public void Charge_AtLimit_ThenAnotherCharge_Declined()
        {
            GameEvents.RaiseCreditCardChargeRequested(5000f, "fill");
            GameEvents.RaiseCreditCardChargeRequested(1f, "extra");
            Assert.AreEqual(5000f, _system.CurrentBalance);
        }

        // ===============================================================
        // STATEMENT TESTS
        // ===============================================================

        [Test]
        public void GenerateStatement_CalculatesInterest()
        {
            GameEvents.RaiseCreditCardChargeRequested(1200f, "test");

            _system.GenerateStatement();

            // Interest = 1200 * (0.24 / 12) = 1200 * 0.02 = 24
            // Statement balance = 1200 + 24 = 1224
            Assert.AreEqual(1224f, _system.StatementBalance, 0.01f);
            Assert.AreEqual(24f, _system.InterestAccrued, 0.01f);
        }

        [Test]
        public void GenerateStatement_CalculatesMinimumPayment()
        {
            GameEvents.RaiseCreditCardChargeRequested(3000f, "test");

            _system.GenerateStatement();

            // Balance after interest: 3000 + (3000 * 0.02) = 3060
            // 2% of 3060 = 61.20, floor is 25, so min = 61.20
            Assert.AreEqual(61.2f, _system.MinimumPaymentDue, 0.01f);
        }

        [Test]
        public void GenerateStatement_SmallBalance_UsesFloor()
        {
            GameEvents.RaiseCreditCardChargeRequested(500f, "test");

            _system.GenerateStatement();

            // Balance after interest: 500 + (500 * 0.02) = 510
            // 2% of 510 = 10.20, floor is 25, so min = 25
            Assert.AreEqual(25f, _system.MinimumPaymentDue, 0.01f);
        }

        [Test]
        public void GenerateStatement_FiresStatementReadyEvent()
        {
            bool eventFired = false;
            GameEvents.OnCreditCardStatementReady += (_, __, ___) => eventFired = true;

            GameEvents.RaiseCreditCardChargeRequested(100f, "test");
            _system.GenerateStatement();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void GenerateStatement_ZeroBalance_ZeroInterest()
        {
            _system.GenerateStatement();

            Assert.AreEqual(0f, _system.StatementBalance, 0.01f);
            Assert.AreEqual(0f, _system.InterestAccrued, 0.01f);
            Assert.AreEqual(0f, _system.MinimumPaymentDue, 0.01f);
        }

        // ===============================================================
        // PAYMENT TESTS
        // ===============================================================

        [Test]
        public void Payment_FullAmount_ZeroesBalance()
        {
            GameEvents.RaiseCreditCardChargeRequested(1000f, "test");
            _system.GenerateStatement();
            float balance = _system.StatementBalance;

            _system.ProcessPayment(balance);

            Assert.AreEqual(0f, _system.CurrentBalance, 0.01f);
        }

        [Test]
        public void Payment_PartialAmount_ReducesBalance()
        {
            GameEvents.RaiseCreditCardChargeRequested(1000f, "test");
            _system.GenerateStatement();

            _system.ProcessPayment(500f);

            // Balance was 1020 (1000 + interest), paid 500, remaining 520
            Assert.AreEqual(520f, _system.CurrentBalance, 0.01f);
        }

        [Test]
        public void Payment_ExceedingBalance_CappedToBalance()
        {
            GameEvents.RaiseCreditCardChargeRequested(100f, "test");
            _system.GenerateStatement();
            // Balance after interest: 100 + (100 * 0.02) = 102
            float balanceBeforePayment = _system.CurrentBalance;

            float completedAmount = 0f;
            GameEvents.OnCreditCardPaymentCompleted += (amount) => completedAmount = amount;

            _system.ProcessPayment(9999f);

            Assert.AreEqual(0f, _system.CurrentBalance, 0.01f);
            Assert.AreEqual(balanceBeforePayment, completedAmount, 0.01f,
                "Payment should be capped to actual balance");
        }

        [Test]
        public void Payment_FiresCompletedEvent()
        {
            GameEvents.RaiseCreditCardChargeRequested(500f, "test");
            _system.GenerateStatement();

            float receivedAmount = 0f;
            GameEvents.OnCreditCardPaymentCompleted += (amount) => receivedAmount = amount;

            _system.ProcessPayment(200f);

            Assert.AreEqual(200f, receivedAmount, 0.01f);
        }

        // ===============================================================
        // UTILIZATION TESTS
        // ===============================================================

        [Test]
        public void Utilization_CalculatesCorrectly()
        {
            GameEvents.RaiseCreditCardChargeRequested(2500f, "test");

            Assert.AreEqual(0.5f, _system.Utilization, 0.01f);
        }

        [Test]
        public void AvailableCredit_CalculatesCorrectly()
        {
            GameEvents.RaiseCreditCardChargeRequested(3000f, "test");

            Assert.AreEqual(2000f, _system.AvailableCredit, 0.01f);
        }

        // ===============================================================
        // CREDIT SCORE TESTS
        // ===============================================================

        [Test]
        public void InitialCreditScore_Is650()
        {
            Assert.AreEqual(650, _system.CreditScore);
        }
    }
}
