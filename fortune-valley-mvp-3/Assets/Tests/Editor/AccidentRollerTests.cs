using NUnit.Framework;
using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Tests for AccidentRoller deterministic accident rolling.
    /// Pure C# class, no Unity lifecycle needed.
    /// </summary>
    [TestFixture]
    public class AccidentRollerTests
    {
        private List<LotInfo> _singleLot;
        private List<AccidentInfo> _singleAccident;

        [SetUp]
        public void SetUp()
        {
            _singleLot = new List<LotInfo>
            {
                new LotInfo("lot_1")
            };

            _singleAccident = new List<AccidentInfo>
            {
                new AccidentInfo("fire", "Fire", 1000f, 10, 1.0f)
            };
        }

        // ===============================================================
        // WINDOW TESTS
        // ===============================================================

        [Test]
        public void Roll_OnWindowDay_CanTrigger()
        {
            // Window interval = 10, probability = 100%, day 10 should trigger
            var results = AccidentRoller.Roll(10, _singleLot, _singleAccident);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("lot_1", results[0].LotId);
            Assert.AreEqual("fire", results[0].AccidentId);
        }

        [Test]
        public void Roll_OffWindowDay_NeverTriggers()
        {
            // Day 7 is not a multiple of 10
            var results = AccidentRoller.Roll(7, _singleLot, _singleAccident);

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Roll_DayZero_IsWindowDay()
        {
            // Day 0 % 10 == 0, so window is open
            var results = AccidentRoller.Roll(0, _singleLot, _singleAccident);

            Assert.AreEqual(1, results.Count);
        }

        // ===============================================================
        // PROBABILITY TESTS
        // ===============================================================

        [Test]
        public void Roll_ZeroProbability_NeverTriggers()
        {
            var neverAccident = new List<AccidentInfo>
            {
                new AccidentInfo("fire", "Fire", 1000f, 10, 0f)
            };

            var results = AccidentRoller.Roll(10, _singleLot, neverAccident);

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Roll_FullProbability_AlwaysTriggers()
        {
            // 100% probability on window day
            var alwaysAccident = new List<AccidentInfo>
            {
                new AccidentInfo("fire", "Fire", 1000f, 10, 1.0f)
            };

            var results = AccidentRoller.Roll(10, _singleLot, alwaysAccident);

            Assert.AreEqual(1, results.Count);
        }

        // ===============================================================
        // DETERMINISM TESTS
        // ===============================================================

        [Test]
        public void Roll_SameInputs_SameResults()
        {
            var results1 = AccidentRoller.Roll(10, _singleLot, _singleAccident);
            var results2 = AccidentRoller.Roll(10, _singleLot, _singleAccident);

            Assert.AreEqual(results1.Count, results2.Count);
            if (results1.Count > 0)
            {
                Assert.AreEqual(results1[0].LotId, results2[0].LotId);
                Assert.AreEqual(results1[0].AccidentId, results2[0].AccidentId);
            }
        }

        // ===============================================================
        // MULTIPLE LOTS TESTS
        // ===============================================================

        [Test]
        public void Roll_MultipleLots_ChecksEachIndependently()
        {
            var twoLots = new List<LotInfo>
            {
                new LotInfo("lot_1"),
                new LotInfo("lot_2")
            };

            // 100% probability, both lots should trigger
            var results = AccidentRoller.Roll(10, twoLots, _singleAccident);

            Assert.AreEqual(2, results.Count);
        }

        [Test]
        public void Roll_MultipleAccidentTypes_ChecksEachIndependently()
        {
            var twoAccidents = new List<AccidentInfo>
            {
                new AccidentInfo("fire", "Fire", 1000f, 10, 1.0f),
                new AccidentInfo("flood", "Flood", 2000f, 10, 1.0f)
            };

            var results = AccidentRoller.Roll(10, _singleLot, twoAccidents);

            Assert.AreEqual(2, results.Count);
        }

        // ===============================================================
        // EDGE CASES
        // ===============================================================

        [Test]
        public void Roll_NoLots_ReturnsEmpty()
        {
            var results = AccidentRoller.Roll(10, new List<LotInfo>(), _singleAccident);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Roll_NoAccidentDefs_ReturnsEmpty()
        {
            var results = AccidentRoller.Roll(10, _singleLot, new List<AccidentInfo>());
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Roll_NullLots_ReturnsEmpty()
        {
            var results = AccidentRoller.Roll(10, null, _singleAccident);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Roll_NullAccidentDefs_ReturnsEmpty()
        {
            var results = AccidentRoller.Roll(10, _singleLot, null);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Roll_ZeroWindowInterval_Skipped()
        {
            var badInterval = new List<AccidentInfo>
            {
                new AccidentInfo("fire", "Fire", 1000f, 0, 1.0f)
            };

            var results = AccidentRoller.Roll(10, _singleLot, badInterval);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Roll_NegativeWindowInterval_Skipped()
        {
            var badInterval = new List<AccidentInfo>
            {
                new AccidentInfo("fire", "Fire", 1000f, -5, 1.0f)
            };

            var results = AccidentRoller.Roll(10, _singleLot, badInterval);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Roll_ResultContainsCorrectDamageCost()
        {
            var results = AccidentRoller.Roll(10, _singleLot, _singleAccident);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1000f, results[0].DamageCost, 0.01f);
            Assert.AreEqual("Fire", results[0].AccidentName);
        }
    }
}
