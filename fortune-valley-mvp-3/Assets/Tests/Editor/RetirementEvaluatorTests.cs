using NUnit.Framework;
using FortuneValley.Core;
using FortuneValley.Domain;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class RetirementEvaluatorTests
    {
        private LifeGoalSelectionService _selection;
        private GoalScorecard _capturedScorecard;
        private int _evaluatedFireCount;

        [SetUp]
        public void SetUp()
        {
            _selection = new LifeGoalSelectionService();
            _capturedScorecard = null;
            _evaluatedFireCount = 0;
            GameEvents.OnGoalsEvaluated += s =>
            {
                _capturedScorecard = s;
                _evaluatedFireCount++;
            };
        }

        [TearDown]
        public void TearDown()
        {
            _selection?.Dispose();
            GameEvents.ClearAllSubscriptions();
        }

        private static LifeGoalEntry[] StandardTrio()
        {
            return new[]
            {
                new LifeGoalEntry("first_apartment", LifeGoalTier.Starter, 100_000f),
                new LifeGoalEntry("family_home", LifeGoalTier.Mid, 500_000f),
                new LifeGoalEntry("retire_early", LifeGoalTier.Ambitious, 2_000_000f),
            };
        }

        [Test]
        public void OnRetirementReached_FiresGoalsEvaluated_WithRetirementAge()
        {
            _selection.HydrateFromDto(StandardTrio());

            using (var eval = new RetirementEvaluator(_selection))
            {
                GameEvents.RaiseRetirementReached();

                Assert.AreEqual(1, _evaluatedFireCount);
                Assert.IsNotNull(_capturedScorecard);
                Assert.AreEqual(LifespanConstants.RetirementAge, _capturedScorecard.retirement_age);
            }
        }

        [Test]
        public void Scorecard_AllRealized_RealizedCountEqualsTotal()
        {
            var entries = StandardTrio();
            entries[0].MarkRealized(80);
            entries[1].MarkRealized(360);
            entries[2].MarkRealized(900);
            _selection.HydrateFromDto(entries);

            using (var eval = new RetirementEvaluator(_selection))
            {
                GameEvents.RaiseRetirementReached();

                Assert.AreEqual(3, _capturedScorecard.RealizedCount);
                Assert.AreEqual(3, _capturedScorecard.TotalGoalCount);
                Assert.AreEqual(0, _capturedScorecard.missed.Length);
            }
        }

        [Test]
        public void Scorecard_NoneRealized_AllMissed()
        {
            _selection.HydrateFromDto(StandardTrio());

            using (var eval = new RetirementEvaluator(_selection))
            {
                GameEvents.RaiseRetirementReached();

                Assert.AreEqual(0, _capturedScorecard.RealizedCount);
                Assert.AreEqual(3, _capturedScorecard.missed.Length);
                Assert.AreEqual(3, _capturedScorecard.TotalGoalCount);
            }
        }

        [Test]
        public void Scorecard_PartiallyRealized_SplitsCorrectly()
        {
            var entries = StandardTrio();
            entries[0].MarkRealized(80); // Starter realized
            _selection.HydrateFromDto(entries);

            using (var eval = new RetirementEvaluator(_selection))
            {
                GameEvents.RaiseRetirementReached();

                Assert.AreEqual(1, _capturedScorecard.RealizedCount);
                Assert.AreEqual(2, _capturedScorecard.missed.Length);
                Assert.AreEqual("first_apartment", _capturedScorecard.realized[0].goal_id);
            }
        }

        [Test]
        public void Scorecard_BankruptcyFlag_PropagatesFromFunc()
        {
            _selection.HydrateFromDto(StandardTrio());

            using (var eval = new RetirementEvaluator(_selection, () => true))
            {
                GameEvents.RaiseRetirementReached();

                Assert.IsTrue(_capturedScorecard.bankruptcy_flag);
            }
        }

        [Test]
        public void Scorecard_BankruptcyFlag_FalseByDefault()
        {
            _selection.HydrateFromDto(StandardTrio());

            using (var eval = new RetirementEvaluator(_selection))
            {
                GameEvents.RaiseRetirementReached();

                Assert.IsFalse(_capturedScorecard.bankruptcy_flag);
            }
        }

        [Test]
        public void Scorecard_NoSelection_StillFires_WithEmptyArrays()
        {
            // No HydrateFromDto -> selection is null
            using (var eval = new RetirementEvaluator(_selection))
            {
                GameEvents.RaiseRetirementReached();

                Assert.AreEqual(1, _evaluatedFireCount);
                Assert.AreEqual(0, _capturedScorecard.RealizedCount);
                Assert.AreEqual(0, _capturedScorecard.TotalGoalCount);
            }
        }

        [Test]
        public void Dispose_UnsubscribesFromRetirementEvent()
        {
            var eval = new RetirementEvaluator(_selection);
            eval.Dispose();

            GameEvents.RaiseRetirementReached();

            Assert.AreEqual(0, _evaluatedFireCount);
        }
    }
}
