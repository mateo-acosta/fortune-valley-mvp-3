using System;
using NUnit.Framework;
using FortuneValley.Core.Questions;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Edge + contract coverage for the QuestionMaster streak reward calculator.
    /// Anchors the published reward table: $25, 30, 35, 45, 55, 65, 75, 90, 105, 125...
    /// </summary>
    [TestFixture]
    public class StreakRewardCalculatorTests
    {
        private const float Base = 25f;
        private const float Mult = 1.15f;
        private const int Step = 5;

        [TestCase(0, 25)]
        [TestCase(1, 30)]
        [TestCase(2, 35)]
        [TestCase(3, 45)]
        [TestCase(4, 55)]
        [TestCase(5, 65)]
        [TestCase(6, 75)]
        [TestCase(7, 90)]
        [TestCase(8, 105)]
        [TestCase(9, 125)]
        public void RewardSequence_MatchesPublishedTable(int streak, int expected)
        {
            Assert.AreEqual(expected, StreakRewardCalculator.RewardForStreak(streak, Base, Mult, Step));
        }

        [Test]
        public void NegativeStreak_ClampsToZero()
        {
            Assert.AreEqual(25, StreakRewardCalculator.RewardForStreak(-5, Base, Mult, Step));
        }

        [Test]
        public void ZeroBaseReward_AlwaysReturnsZero()
        {
            Assert.AreEqual(0, StreakRewardCalculator.RewardForStreak(10, 0f, Mult, Step));
        }

        [Test]
        public void UnitMultiplier_RewardStaysAtBaseRounded()
        {
            // With multiplier = 1.0, every streak yields the same raw value.
            // Base 25 rounds up to 25 (already on the step boundary).
            for (int s = 0; s < 20; s++)
            {
                Assert.AreEqual(25, StreakRewardCalculator.RewardForStreak(s, Base, 1.0f, Step));
            }
        }

        [Test]
        public void ExactStepBoundary_DoesNotJumpToNextStep()
        {
            // base=30, multiplier=1.0, step=5 -> raw is exactly 30, should stay 30.
            Assert.AreEqual(30, StreakRewardCalculator.RewardForStreak(0, 30f, 1.0f, Step));
        }

        [Test]
        public void ValueBetweenZeroAndStep_RoundsUpToStep()
        {
            // base=3, mult=1, step=5 -> raw=3, ceiling-rounded to 5.
            Assert.AreEqual(5, StreakRewardCalculator.RewardForStreak(0, 3f, 1.0f, Step));
        }

        [Test]
        public void ZeroRoundingStep_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                StreakRewardCalculator.RewardForStreak(0, Base, Mult, 0));
        }

        [Test]
        public void NegativeRoundingStep_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                StreakRewardCalculator.RewardForStreak(0, Base, Mult, -1));
        }
    }
}
