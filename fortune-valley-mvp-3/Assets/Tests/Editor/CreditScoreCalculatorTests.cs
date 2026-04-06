using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class CreditScoreCalculatorTests
    {
        private CreditScoringConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<CreditScoringConfig>();
            // Use defaults from the ScriptableObject:
            // startingScore=650, min=300, max=850
            // onTimeBonus=15, missedPenalty=50
            // lowUtilThreshold=0.30, lowUtilBonus=10
            // highUtilThreshold=0.70, highUtilPenalty=20
            // highDtiThreshold=0.40, highDtiPenalty=15
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // ===============================================================
        // PAYMENT HISTORY
        // ===============================================================

        [Test]
        public void OnTimePayment_IncreasesScore()
        {
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0.5f, dti: 0.2f);
            Assert.Greater(result, 650);
        }

        [Test]
        public void MissedPayment_DecreasesScore()
        {
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: false, utilization: 0.5f, dti: 0.2f);
            Assert.Less(result, 650);
        }

        // ===============================================================
        // UTILIZATION
        // ===============================================================

        [Test]
        public void LowUtilization_GivesBonus()
        {
            int low = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0.10f, dti: 0.2f);
            int mid = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0.50f, dti: 0.2f);
            Assert.Greater(low, mid, "Low utilization should score higher than mid utilization");
        }

        [Test]
        public void HighUtilization_GivesPenalty()
        {
            int high = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0.90f, dti: 0.2f);
            int mid = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0.50f, dti: 0.2f);
            Assert.Less(high, mid, "High utilization should score lower than mid utilization");
        }

        [Test]
        public void ZeroUtilization_GivesBonus()
        {
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0f, dti: 0.2f);
            // On-time bonus (15) + low util bonus (10) = 675
            Assert.AreEqual(675, result);
        }

        // ===============================================================
        // DEBT-TO-INCOME
        // ===============================================================

        [Test]
        public void HighDTI_GivesPenalty()
        {
            int highDti = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0.5f, dti: 0.60f);
            int lowDti = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0.5f, dti: 0.20f);
            Assert.Less(highDti, lowDti, "High DTI should score lower");
        }

        [Test]
        public void ZeroIncomeDTI_Infinity_GivesPenalty()
        {
            // DTI of infinity (no income) should trigger high DTI penalty
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0.5f, dti: float.PositiveInfinity);
            int normal = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0.5f, dti: 0.2f);
            Assert.Less(result, normal);
        }

        // ===============================================================
        // CLAMPING
        // ===============================================================

        [Test]
        public void Score_NeverExceedsMax()
        {
            // Start at 840, on-time + low util = +25, should clamp at 850
            int result = CreditScoreCalculator.Recalculate(840, _config, paidOnTime: true, utilization: 0.1f, dti: 0.1f);
            Assert.AreEqual(850, result);
        }

        [Test]
        public void Score_NeverGoesBelowMin()
        {
            // Start at 310, missed payment = -50, should clamp at 300
            int result = CreditScoreCalculator.Recalculate(310, _config, paidOnTime: false, utilization: 0.9f, dti: 0.6f);
            Assert.AreEqual(300, result);
        }

        [Test]
        public void ScoreAtExactMin_AdditionalPenalty_StaysAtMin()
        {
            int result = CreditScoreCalculator.Recalculate(300, _config, paidOnTime: false, utilization: 0.9f, dti: 0.6f);
            Assert.AreEqual(300, result);
        }

        [Test]
        public void ScoreAtExactMax_AdditionalBonus_StaysAtMax()
        {
            int result = CreditScoreCalculator.Recalculate(850, _config, paidOnTime: true, utilization: 0.1f, dti: 0.1f);
            Assert.AreEqual(850, result);
        }

        // ===============================================================
        // COMBINED FACTORS
        // ===============================================================

        [Test]
        public void OnTimePayment_PlusHighUtilization_NetEffect()
        {
            // On-time (+15) + high util (-20) = net -5
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0.80f, dti: 0.2f);
            Assert.AreEqual(645, result);
        }

        [Test]
        public void MissedPayment_PlusHighUtil_PlusHighDTI_AllPenalties()
        {
            // Missed (-50) + high util (-20) + high DTI (-15) = -85
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: false, utilization: 0.80f, dti: 0.60f);
            Assert.AreEqual(565, result);
        }

        [Test]
        public void OnTimePayment_PlusLowUtil_PlusLowDTI_AllBonuses()
        {
            // On-time (+15) + low util (+10) + no DTI penalty = +25
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, utilization: 0.10f, dti: 0.20f);
            Assert.AreEqual(675, result);
        }
    }
}
