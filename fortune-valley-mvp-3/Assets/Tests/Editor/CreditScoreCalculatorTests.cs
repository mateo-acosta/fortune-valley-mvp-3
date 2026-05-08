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
            // highDtiThreshold=0.40, highDtiPenalty=15
            // (utilization factors removed when the CC mechanic was disabled)
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
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, dti: 0.2f);
            Assert.Greater(result, 650);
        }

        [Test]
        public void MissedPayment_DecreasesScore()
        {
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: false, dti: 0.2f);
            Assert.Less(result, 650);
        }

        // ===============================================================
        // DEBT-TO-INCOME
        // ===============================================================

        [Test]
        public void HighDTI_GivesPenalty()
        {
            int highDti = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, dti: 0.60f);
            int lowDti = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, dti: 0.20f);
            Assert.Less(highDti, lowDti, "High DTI should score lower");
        }

        [Test]
        public void ZeroIncomeDTI_Infinity_GivesPenalty()
        {
            // DTI of infinity (no income) should trigger high DTI penalty
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, dti: float.PositiveInfinity);
            int normal = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, dti: 0.2f);
            Assert.Less(result, normal);
        }

        // ===============================================================
        // CLAMPING
        // ===============================================================

        [Test]
        public void Score_NeverExceedsMax()
        {
            // Start at 840, on-time + low DTI = +15 only, should clamp at 850
            int result = CreditScoreCalculator.Recalculate(840, _config, paidOnTime: true, dti: 0.1f);
            Assert.AreEqual(850, result);
        }

        [Test]
        public void Score_NeverGoesBelowMin()
        {
            // Start at 310, missed payment = -50, should clamp at 300
            int result = CreditScoreCalculator.Recalculate(310, _config, paidOnTime: false, dti: 0.6f);
            Assert.AreEqual(300, result);
        }

        [Test]
        public void ScoreAtExactMin_AdditionalPenalty_StaysAtMin()
        {
            int result = CreditScoreCalculator.Recalculate(300, _config, paidOnTime: false, dti: 0.6f);
            Assert.AreEqual(300, result);
        }

        [Test]
        public void ScoreAtExactMax_AdditionalBonus_StaysAtMax()
        {
            int result = CreditScoreCalculator.Recalculate(850, _config, paidOnTime: true, dti: 0.1f);
            Assert.AreEqual(850, result);
        }

        // ===============================================================
        // COMBINED FACTORS
        // ===============================================================

        [Test]
        public void MissedPayment_PlusHighDTI_AllPenalties()
        {
            // Missed (-50) + high DTI (-15) = -65
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: false, dti: 0.60f);
            Assert.AreEqual(585, result);
        }

        [Test]
        public void OnTimePayment_PlusLowDTI_AllBonuses()
        {
            // On-time (+15) + no DTI penalty = +15
            int result = CreditScoreCalculator.Recalculate(650, _config, paidOnTime: true, dti: 0.20f);
            Assert.AreEqual(665, result);
        }
    }
}
