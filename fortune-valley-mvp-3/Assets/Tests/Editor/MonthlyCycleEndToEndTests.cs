using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    /// <summary>
    /// End-to-end-ish coverage of the monthly cycle with the CC mechanic
    /// disabled (the production default). Verifies that:
    ///   - Score still updates from loan-payment behavior + DTI
    ///   - CC statement popup never raises
    ///   - Cycle still completes
    /// </summary>
    [TestFixture]
    public class MonthlyCycleEndToEndTests
    {
        private GameObject _go;
        private MonthlyPaymentDayController _controller;
        private CreditScoreSystem _scoreSystem;
        private LoanSystem _loanSystem;
        private InsuranceSystem _insuranceSystem;
        private RestaurantSystem _restaurantSystem;
        private TimeManager _timeManager;
        private CurrencyManager _currencyManager;
        private CreditCardConfig _ccConfig;
        private CreditScoringConfig _scoringConfig;
        private RestaurantConfig _restaurantConfig;

        private bool _ccFlagBeforeTest;

        [SetUp]
        public void SetUp()
        {
            _ccFlagBeforeTest = FeatureFlags.CreditCardChargesEnabled;
            // Production default for these tests: CC mechanic OFF.
            FeatureFlags.CreditCardChargesEnabled = false;

            GameEvents.ClearAllSubscriptions();

            _go = new GameObject("CycleE2E");
            _controller = _go.AddComponent<MonthlyPaymentDayController>();
            _scoreSystem = _go.AddComponent<CreditScoreSystem>();
            _loanSystem = _go.AddComponent<LoanSystem>();
            _insuranceSystem = _go.AddComponent<InsuranceSystem>();
            _restaurantSystem = _go.AddComponent<RestaurantSystem>();
            _timeManager = _go.AddComponent<TimeManager>();
            _currencyManager = _go.AddComponent<CurrencyManager>();

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
            SetField(_scoringConfig, "_highDtiThreshold", 0.40f);
            SetField(_scoringConfig, "_highDtiPenalty", 15);

            SetField(_scoreSystem, "_config", _ccConfig);
            SetField(_scoreSystem, "_scoringConfig", _scoringConfig);
            SetField(_scoreSystem, "_loanSystem", _loanSystem);

            SetField(_timeManager, "_ticksPerDay", 10);
            SetField(_timeManager, "_speedOptions", new float[] { 0f, 1f, 2f });
            SetField(_timeManager, "_currentSpeedIndex", 1);

            _restaurantConfig = ScriptableObject.CreateInstance<RestaurantConfig>();
            SetField(_restaurantConfig, "_baseIncomePerTick", 10f);
            SetField(_restaurantConfig, "_maxLevel", 3);
            SetField(_restaurantConfig, "_incomeMultipliers", new float[] { 1f, 2.5f, 5f });
            SetField(_restaurantSystem, "_config", _restaurantConfig);

            SetField(_controller, "_creditCardSystem", _scoreSystem);
            SetField(_controller, "_loanSystem", _loanSystem);
            SetField(_controller, "_insuranceSystem", _insuranceSystem);
            SetField(_controller, "_restaurantSystem", _restaurantSystem);
            SetField(_controller, "_timeManager", _timeManager);

            SetField(_loanSystem, "_currencyManager", _currencyManager);

            InvokePrivate(_scoreSystem, "OnEnable");
            InvokePrivate(_loanSystem, "OnEnable");
            InvokePrivate(_controller, "OnEnable");

            _currencyManager.SetCheckingBalance(50_000f);
            GameEvents.RaiseGameStart();
        }

        [TearDown]
        public void TearDown()
        {
            InvokePrivate(_controller, "OnDisable");
            InvokePrivate(_scoreSystem, "OnDisable");
            InvokePrivate(_loanSystem, "OnDisable");
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_ccConfig);
            Object.DestroyImmediate(_scoringConfig);
            Object.DestroyImmediate(_restaurantConfig);
            GameEvents.ClearAllSubscriptions();

            FeatureFlags.CreditCardChargesEnabled = _ccFlagBeforeTest;
        }

        [Test]
        public void CcDisabled_NoLoans_CycleCompletesAndScoreRises()
        {
            int scoreBefore = _scoreSystem.CreditScore;
            bool statementFired = false;
            bool cycleComplete = false;

            GameEvents.OnCreditCardStatementReady += (_, __, ___) => statementFired = true;
            GameEvents.OnMonthlyPaymentCycleComplete += () => cycleComplete = true;

            // Day 30 = payment day.
            GameEvents.RaiseDayEnd(30);

            Assert.IsFalse(statementFired, "Statement must not fire when CC mechanic is disabled.");
            Assert.IsTrue(cycleComplete, "Cycle should complete via the zero-balance shortcut.");
            Assert.AreEqual(scoreBefore + 15, _scoreSystem.CreditScore,
                "No active loans => paidOnTime=true (no missed loans) => +15 on-time bonus.");
        }

        [Test]
        public void CcDisabled_LoanPaidOnTime_ScoreRisesByOnTimeBonus()
        {
            // Originate a loan that we can afford.
            GameEvents.RaiseLoanPurchaseRequested(GetFirstLoanId(), "lot1", 6000f);
            _currencyManager.SetCheckingBalance(50_000f);

            int scoreBefore = _scoreSystem.CreditScore;

            GameEvents.RaiseDayEnd(30);

            Assert.AreEqual(scoreBefore + 15, _scoreSystem.CreditScore,
                "Paid-on-time loan bumps score by the OnTimePaymentBonus.");
        }

        [Test]
        public void CcDisabled_LoanMissed_ScoreFallsByMissedPenalty()
        {
            // Originate then drain checking so the cycle's payment misses.
            GameEvents.RaiseLoanPurchaseRequested(GetFirstLoanId(), "lot1", 6000f);
            _currencyManager.SetCheckingBalance(0f);

            int scoreBefore = _scoreSystem.CreditScore;

            GameEvents.RaiseDayEnd(30);

            Assert.AreEqual(scoreBefore - 50, _scoreSystem.CreditScore,
                "Missed loan payment drops score by the MissedPaymentPenalty.");
        }

        // -------- Helpers --------

        private string GetFirstLoanId()
        {
            // Build a single LoanConfig and wire it into LoanSystem so the
            // origination intent has something to target.
            var cfg = ScriptableObject.CreateInstance<LoanConfig>();
            SetField(cfg, "_loanId", "e2e_loan");
            SetField(cfg, "_displayName", "E2E Loan");
            SetField(cfg, "_apr", 0.10f);
            SetField(cfg, "_termYears", 12);
            SetField(cfg, "_downPaymentPercent", 0f);
            SetField(cfg, "_minimumCreditScore", 0);
            SetField(cfg, "_maxDtiRatio", 1.0f);

            var configs = new System.Collections.Generic.List<LoanConfig> { cfg };
            SetField(_loanSystem, "_availableLoans", configs);
            return "e2e_loan";
        }

        private static void SetField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) { field.SetValue(obj, value); return; }
                type = type.BaseType;
            }
        }

        private static void InvokePrivate(object obj, string methodName)
        {
            var method = obj.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(obj, null);
        }
    }
}
