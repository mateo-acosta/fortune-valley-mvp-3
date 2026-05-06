using NUnit.Framework;
using FortuneValley.Core;
using FortuneValley.Domain;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LifespanControllerTests
    {
        private int _yearEndFireCount;
        private int _lastYearEndAge;
        private int _retirementFireCount;

        [SetUp]
        public void SetUp()
        {
            _yearEndFireCount = 0;
            _lastYearEndAge = 0;
            _retirementFireCount = 0;
            GameEvents.OnYearEnd += age =>
            {
                _yearEndFireCount++;
                _lastYearEndAge = age;
            };
            GameEvents.OnRetirementReached += () => _retirementFireCount++;
        }

        [TearDown]
        public void TearDown()
        {
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void StartingAge_Is25()
        {
            using (var ctrl = new LifespanController())
            {
                Assert.AreEqual(LifespanConstants.StartingAge, ctrl.CurrentAge);
                Assert.AreEqual(25, ctrl.CurrentAge);
                Assert.IsFalse(ctrl.HasRetired);
            }
        }

        [Test]
        public void DayEnd_BeforeYearBoundary_DoesNotFireYearEnd()
        {
            using (var ctrl = new LifespanController())
            {
                GameEvents.RaiseDayEnd(5);
                GameEvents.RaiseDayEnd(15);
                GameEvents.RaiseDayEnd(29);

                Assert.AreEqual(0, _yearEndFireCount);
                Assert.AreEqual(25, ctrl.CurrentAge);
            }
        }

        [Test]
        public void DayEnd_AtYearBoundary_FiresYearEnd_Age26()
        {
            using (var ctrl = new LifespanController())
            {
                // DaysPerYear = 30. At day 30, age becomes 26.
                GameEvents.RaiseDayEnd(30);

                Assert.AreEqual(1, _yearEndFireCount);
                Assert.AreEqual(26, _lastYearEndAge);
                Assert.AreEqual(26, ctrl.CurrentAge);
            }
        }

        [Test]
        public void DayEnd_MultipleYearsPassed_FiresOncePerObservedAge()
        {
            using (var ctrl = new LifespanController())
            {
                GameEvents.RaiseDayEnd(30);  // age 26 (fires)
                GameEvents.RaiseDayEnd(60);  // age 27 (fires)
                GameEvents.RaiseDayEnd(90);  // age 28 (fires)

                Assert.AreEqual(3, _yearEndFireCount);
                Assert.AreEqual(28, _lastYearEndAge);
            }
        }

        [Test]
        public void DayEnd_AtRetirementBoundary_FiresRetirementOnce()
        {
            using (var ctrl = new LifespanController())
            {
                // 40 years * 30 days = 1200 days. Age becomes 65 at day 1200.
                GameEvents.RaiseDayEnd(LifespanConstants.TotalLifeDays);

                Assert.AreEqual(1, _retirementFireCount);
                Assert.IsTrue(ctrl.HasRetired);
                Assert.AreEqual(LifespanConstants.RetirementAge, ctrl.CurrentAge);
            }
        }

        [Test]
        public void Retirement_DoesNotFireTwice_OnFurtherDayEnds()
        {
            using (var ctrl = new LifespanController())
            {
                GameEvents.RaiseDayEnd(LifespanConstants.TotalLifeDays);
                GameEvents.RaiseDayEnd(LifespanConstants.TotalLifeDays + 30);
                GameEvents.RaiseDayEnd(LifespanConstants.TotalLifeDays + 60);

                Assert.AreEqual(1, _retirementFireCount,
                    "OnRetirementReached must fire exactly once per life.");
            }
        }

        [Test]
        public void ResetForNewGame_RestoresStartingState()
        {
            using (var ctrl = new LifespanController())
            {
                GameEvents.RaiseDayEnd(LifespanConstants.TotalLifeDays);
                Assert.IsTrue(ctrl.HasRetired);

                ctrl.ResetForNewGame();

                Assert.IsFalse(ctrl.HasRetired);
                Assert.AreEqual(LifespanConstants.StartingAge, ctrl.CurrentAge);
            }
        }

        [Test]
        public void Dispose_UnsubscribesFromDayEnd()
        {
            var ctrl = new LifespanController();
            ctrl.Dispose();

            GameEvents.RaiseDayEnd(30);

            Assert.AreEqual(0, _yearEndFireCount);
        }

        [Test]
        public void LifespanConstants_AgeFromDay_BoundaryValues()
        {
            // Belt-and-suspenders: verifies the formula used by both
            // LifespanController and GameStateDTOBuilder.
            Assert.AreEqual(25, LifespanConstants.AgeFromDay(0));
            Assert.AreEqual(25, LifespanConstants.AgeFromDay(29));
            Assert.AreEqual(26, LifespanConstants.AgeFromDay(30));
            Assert.AreEqual(64, LifespanConstants.AgeFromDay(LifespanConstants.TotalLifeDays - 1));
            Assert.AreEqual(65, LifespanConstants.AgeFromDay(LifespanConstants.TotalLifeDays));
        }
    }
}
