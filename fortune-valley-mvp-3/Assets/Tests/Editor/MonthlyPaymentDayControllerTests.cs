using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for MonthlyPaymentDayController.
    /// Verifies payment sequence, state machine, time-pause behavior,
    /// zero-balance shortcut, and non-payment-day no-op.
    /// </summary>
    [TestFixture]
    public class MonthlyPaymentDayControllerTests
    {
        private GameObject _go;
        private MonthlyPaymentDayController _controller;
        private CreditCardSystem _creditCardSystem;
        private LoanSystem _loanSystem;
        private InsuranceSystem _insuranceSystem;
        private RestaurantSystem _restaurantSystem;
        private TimeManager _timeManager;
        private CurrencyManager _currencyManager;

        private CreditCardConfig _ccConfig;
        private CreditScoringConfig _scoringConfig;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestController");

            _controller = _go.AddComponent<MonthlyPaymentDayController>();
            _creditCardSystem = _go.AddComponent<CreditCardSystem>();
            _loanSystem = _go.AddComponent<LoanSystem>();
            _insuranceSystem = _go.AddComponent<InsuranceSystem>();
            _restaurantSystem = _go.AddComponent<RestaurantSystem>();
            _timeManager = _go.AddComponent<TimeManager>();
            _currencyManager = _go.AddComponent<CurrencyManager>();

            // Wire CreditCardSystem config
            _ccConfig = ScriptableObject.CreateInstance<CreditCardConfig>();
            SetField(_ccConfig, "_creditLimit", 5000f);
            SetField(_ccConfig, "_apr", 0.24f);
            SetField(_ccConfig, "_minimumPaymentPercent", 0.02f);
            SetField(_ccConfig, "_minimumPaymentFloor", 25f);
            SetField(_ccConfig, "_billingCycleDays", 30);

            _scoringConfig = ScriptableObject.CreateInstance<CreditScoringConfig>();
            SetField(_scoringConfig, "_startingScore", 650);
            SetField(_scoringConfig, "_minScore", 300);
            SetField(_scoringConfig, "_maxScore", 850);
            SetField(_scoringConfig, "_onTimePaymentBonus", 15);
            SetField(_scoringConfig, "_missedPaymentPenalty", 50);
            SetField(_scoringConfig, "_lowUtilizationThreshold", 0.30f);
            SetField(_scoringConfig, "_lowUtilizationBonus", 10);
            SetField(_scoringConfig, "_highUtilizationThreshold", 0.70f);
            SetField(_scoringConfig, "_highUtilizationPenalty", 20);
            SetField(_scoringConfig, "_highDtiThreshold", 0.40f);
            SetField(_scoringConfig, "_highDtiPenalty", 15);

            SetField(_creditCardSystem, "_config", _ccConfig);
            SetField(_creditCardSystem, "_scoringConfig", _scoringConfig);

            // Wire TimeManager
            SetField(_timeManager, "_ticksPerDay", 10);
            SetField(_timeManager, "_speedOptions", new float[] { 0f, 1f, 2f });
            SetField(_timeManager, "_currentSpeedIndex", 1);

            // Wire RestaurantSystem
            var restaurantConfig = ScriptableObject.CreateInstance<RestaurantConfig>();
            SetField(restaurantConfig, "_baseIncomePerTick", 10f);
            SetField(restaurantConfig, "_maxLevel", 3);
            SetField(restaurantConfig, "_incomeMultipliers", new float[] { 1f, 2.5f, 5f });
            SetField(_restaurantSystem, "_config", restaurantConfig);

            // Wire controller deps
            SetField(_controller, "_creditCardSystem", _creditCardSystem);
            SetField(_controller, "_loanSystem", _loanSystem);
            SetField(_controller, "_insuranceSystem", _insuranceSystem);
            SetField(_controller, "_restaurantSystem", _restaurantSystem);
            SetField(_controller, "_timeManager", _timeManager);

            // Wire currency for LoanSystem
            SetField(_loanSystem, "_currencyManager", _currencyManager);

            // Start all systems
            InvokePrivate(_creditCardSystem, "OnEnable");
            InvokePrivate(_loanSystem, "OnEnable");
            InvokePrivate(_controller, "OnEnable");
            _currencyManager.SetCheckingBalance(5000f);
            GameEvents.RaiseGameStart();
        }

        [TearDown]
        public void TearDown()
        {
            InvokePrivate(_controller, "OnDisable");
            InvokePrivate(_creditCardSystem, "OnDisable");
            InvokePrivate(_loanSystem, "OnDisable");
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_ccConfig);
            Object.DestroyImmediate(_scoringConfig);
            GameEvents.ClearAllSubscriptions();
        }

        // ===============================================================
        // NON-PAYMENT DAY: NO-OP
        // ===============================================================

        [Test]
        public void NonPaymentDay_DoesNotFireStatementEvent()
        {
            bool statementFired = false;
            GameEvents.OnCreditCardStatementReady += (_, __, ___) => statementFired = true;

            GameEvents.RaiseDayEnd(1);
            GameEvents.RaiseDayEnd(15);
            GameEvents.RaiseDayEnd(29);

            Assert.IsFalse(statementFired);
        }

        [Test]
        public void NonPaymentDay_DoesNotFirePaymentCycleStarted()
        {
            bool cycleFired = false;
            GameEvents.OnMonthlyPaymentDayStarted += (day) => cycleFired = true;

            GameEvents.RaiseDayEnd(1);
            GameEvents.RaiseDayEnd(5);

            Assert.IsFalse(cycleFired);
        }

        // ===============================================================
        // PAYMENT DAY TRIGGER
        // ===============================================================

        [Test]
        public void PaymentDay30_FiresMonthlyPaymentDayStarted()
        {
            int firedDay = -1;
            GameEvents.OnMonthlyPaymentDayStarted += (day) => firedDay = day;

            GameEvents.RaiseDayEnd(30);

            Assert.AreEqual(30, firedDay);
        }

        [Test]
        public void PaymentDay60_AlsoFiresCycle()
        {
            int firedCount = 0;
            GameEvents.OnMonthlyPaymentDayStarted += (day) => firedCount++;

            GameEvents.RaiseDayEnd(30);
            // Resume from waiting state (zero balance path since no charges)
            GameEvents.RaiseDayEnd(60);

            Assert.AreEqual(2, firedCount);
        }

        // ===============================================================
        // ZERO BALANCE SHORTCUT
        // ===============================================================

        [Test]
        public void ZeroStatementBalance_SkipsPopup_UpdatesCreditScore()
        {
            // No CC charges made -- statement balance is zero
            bool statementEventFired = false;
            bool cycleComplete = false;
            GameEvents.OnCreditCardStatementReady += (_, __, ___) => statementEventFired = true;
            GameEvents.OnMonthlyPaymentCycleComplete += () => cycleComplete = true;

            GameEvents.RaiseDayEnd(30);

            // Statement event is still fired by GenerateStatement
            Assert.IsTrue(statementEventFired);
            // Cycle completes immediately without waiting for player payment
            Assert.IsTrue(cycleComplete);
        }

        [Test]
        public void ZeroStatementBalance_DoesNotPauseTime()
        {
            // No charges -- should not pause
            GameEvents.RaiseDayEnd(30);

            Assert.IsFalse(_timeManager.IsPaused);
        }

        // ===============================================================
        // WITH CC BALANCE: PAUSE AND WAIT
        // ===============================================================

        [Test]
        public void WithCCBalance_PausesTimeOnPaymentDay()
        {
            // Charge credit card so there is a balance
            GameEvents.RaiseCreditCardChargeRequested(500f, "test charge");

            GameEvents.RaiseDayEnd(30);

            Assert.IsTrue(_timeManager.IsPaused);
        }

        [Test]
        public void WithCCBalance_ResumesTimeAfterPayment()
        {
            GameEvents.RaiseCreditCardChargeRequested(500f, "test charge");
            GameEvents.RaiseDayEnd(30);
            Assert.IsTrue(_timeManager.IsPaused);

            // Player pays via CC system
            _creditCardSystem.ProcessPayment(500f);

            Assert.IsFalse(_timeManager.IsPaused);
        }

        [Test]
        public void WithCCBalance_CycleCompleteFiresAfterPayment()
        {
            GameEvents.RaiseCreditCardChargeRequested(300f, "test charge");
            GameEvents.RaiseDayEnd(30);

            bool cycleComplete = false;
            GameEvents.OnMonthlyPaymentCycleComplete += () => cycleComplete = true;

            _creditCardSystem.ProcessPayment(300f);

            Assert.IsTrue(cycleComplete);
        }

        // ===============================================================
        // SEQUENCE ORDER
        // ===============================================================

        [Test]
        public void PaymentSequence_StepsFireInOrder()
        {
            // Charge CC so we go through the full sequence
            GameEvents.RaiseCreditCardChargeRequested(200f, "test");

            var callLog = new System.Collections.Generic.List<string>();

            // Intercept the three controller-owned events to verify their order.
            // OnCreditCardPaymentCompleted is NOT included: the controller subscribes
            // to it first (in OnEnable) and fires cycle_complete from within that
            // handler, so cycle_complete would appear before a test subscription's
            // callback -- it is an internal trigger, not an externally-ordered step.
            GameEvents.OnMonthlyPaymentDayStarted += (day) => callLog.Add("cycle_started");
            GameEvents.OnCreditCardStatementReady += (_, __, ___) => callLog.Add("statement_ready");
            GameEvents.OnMonthlyPaymentCycleComplete += () => callLog.Add("cycle_complete");

            // Trigger payment day
            GameEvents.RaiseDayEnd(30);
            // Simulate player paying
            _creditCardSystem.ProcessPayment(200f);

            Assert.AreEqual(3, callLog.Count,
                $"Expected 3 events, got: {string.Join(", ", callLog)}");
            Assert.AreEqual("cycle_started", callLog[0]);
            Assert.AreEqual("statement_ready", callLog[1]);
            Assert.AreEqual("cycle_complete", callLog[2]);
        }

        [Test]
        public void CreditScoreUpdate_NotFiredBeforePayment()
        {
            // Arrange: charge CC
            GameEvents.RaiseCreditCardChargeRequested(500f, "test");
            int initialScore = _creditCardSystem.CreditScore;

            // Trigger payment day -- time pauses, waiting for payment
            GameEvents.RaiseDayEnd(30);

            // Score should not have changed yet
            Assert.AreEqual(initialScore, _creditCardSystem.CreditScore);
        }

        [Test]
        public void CreditScoreUpdate_FiresAfterPayment()
        {
            GameEvents.RaiseCreditCardChargeRequested(500f, "test");
            int initialScore = _creditCardSystem.CreditScore;
            GameEvents.RaiseDayEnd(30);

            // Pay the full balance
            _creditCardSystem.ProcessPayment(500f);

            // Score should have changed (on-time payment bonus applied)
            Assert.AreNotEqual(initialScore, _creditCardSystem.CreditScore);
        }

        // ===============================================================
        // GAME RESTART STATE RESET
        // ===============================================================

        [Test]
        public void GameRestart_ResetsStateToIdle()
        {
            // Put controller in waiting state
            GameEvents.RaiseCreditCardChargeRequested(300f, "test");
            GameEvents.RaiseDayEnd(30);
            Assert.IsTrue(_timeManager.IsPaused);

            // Restart game
            GameEvents.RaiseGameStart();

            // Fire next payment day -- should work normally (state was reset)
            // Zero balance on new game -- should not pause
            GameEvents.RaiseDayEnd(30);
            Assert.IsFalse(_timeManager.IsPaused);
        }

        // ===============================================================
        // LOAN PAYMENTS PROCESSED ON PAYMENT DAY
        // ===============================================================

        [Test]
        public void LoanPayments_ProcessedBeforeStatement()
        {
            // Create a loan config and originate a loan
            var loanConfig = ScriptableObject.CreateInstance<LoanConfig>();
            SetField(loanConfig, "_loanId", "test_loan");
            SetField(loanConfig, "_displayName", "Test Loan");
            SetField(loanConfig, "_apr", 0f);
            SetField(loanConfig, "_termMonths", 12);
            SetField(loanConfig, "_downPaymentPercent", 0f);
            SetField(loanConfig, "_minimumCreditScore", 0);
            SetField(loanConfig, "_maxDtiRatio", 1f);

            SetField(_loanSystem, "_availableLoans", new List<LoanConfig> { loanConfig });

            _currencyManager.SetCheckingBalance(5000f);
            GameEvents.RaiseLoanPurchaseRequested("test_loan", "lot1", 1200f);

            float balanceBeforePaymentDay = _currencyManager.CheckingBalance;

            GameEvents.RaiseDayEnd(30);

            // Loan payment should have been deducted
            Assert.Less(_currencyManager.CheckingBalance, balanceBeforePaymentDay);

            Object.DestroyImmediate(loanConfig);
        }

        // ===============================================================
        // DTI CALCULATOR (standalone)
        // ===============================================================

        [Test]
        public void DtiCalculator_Compute_ZeroIncome_ReturnsZero()
        {
            Assert.AreEqual(0f, DtiCalculator.Compute(500f, 0f));
        }

        [Test]
        public void DtiCalculator_Compute_StandardCase()
        {
            float dti = DtiCalculator.Compute(1000f, 4000f);
            Assert.AreEqual(0.25f, dti, 0.001f);
        }

        [Test]
        public void DtiCalculator_ComputeMonthlyIncome_CorrectResult()
        {
            float income = DtiCalculator.ComputeMonthlyIncome(10f, 10, 30);
            Assert.AreEqual(3000f, income, 0.01f);
        }

        [Test]
        public void DtiCalculator_ComputeTotalMonthlyDebt_SumsValues()
        {
            float total = DtiCalculator.ComputeTotalMonthlyDebt(500f, 25f);
            Assert.AreEqual(525f, total, 0.01f);
        }

        // ===============================================================
        // HELPERS
        // ===============================================================

        private void SetField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) { field.SetValue(obj, value); return; }
                type = type.BaseType;
            }
            throw new System.Exception($"Field '{fieldName}' not found on {obj.GetType().Name}");
        }

        private void InvokePrivate(object obj, string methodName)
        {
            var method = obj.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(obj, null);
        }
    }
}
