using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using FortuneValley.UI.Components;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Unit tests for LineGraphGraphic.GraphLayout static helpers.
    /// These are pure math functions — no Canvas or MonoBehaviour setup needed.
    /// </summary>
    [TestFixture]
    public class LineGraphLayoutTests
    {
        // A shared rect for MapPoint tests: 0,0 to 100,50
        private static readonly Rect TestRect = new Rect(0f, 0f, 100f, 50f);

        // ═══════════════════════════════════════════════════════════════
        // ComputeYRange tests
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void ComputeYRange_AllZeroData_ReturnsNonZeroRange()
        {
            // Flat-zero data must not produce a zero-height Y range (no divide-by-zero)
            var data = new List<float> { 0f, 0f, 0f, 0f };
            var (paddedMin, paddedMax) = LineGraphGraphic.GraphLayout.ComputeYRange(data);

            Assert.Greater(paddedMax, paddedMin,
                "paddedMax must exceed paddedMin even when all data is zero");
        }

        [Test]
        public void ComputeYRange_NegativeMin_PadsCorrectly()
        {
            // When min is negative the padding should extend further below, not shrink it
            var data = new List<float> { -100f, 0f, 50f };
            var (paddedMin, paddedMax) = LineGraphGraphic.GraphLayout.ComputeYRange(data);

            float range = 50f - (-100f); // 150
            float expectedMin = -100f - range * 0.08f;
            float expectedMax = 50f  + range * 0.08f;

            Assert.AreEqual(expectedMin, paddedMin, 0.001f, "paddedMin should be below -100");
            Assert.AreEqual(expectedMax, paddedMax, 0.001f, "paddedMax should be above 50");
        }

        [Test]
        public void ComputeYRange_AllEqualValues_UsesRangeFloor()
        {
            // All-equal values → raw range == 0 → should clamp to minimum range of 1f
            var data = new List<float> { 42f, 42f, 42f };
            var (paddedMin, paddedMax) = LineGraphGraphic.GraphLayout.ComputeYRange(data);

            // range = 1f (floor), padding = 1f * 0.08f = 0.08
            float expectedMin = 42f - 1f * 0.08f;
            float expectedMax = 42f + 1f * 0.08f;

            Assert.AreEqual(expectedMin, paddedMin, 0.001f);
            Assert.AreEqual(expectedMax, paddedMax, 0.001f);
        }

        // ═══════════════════════════════════════════════════════════════
        // MapPoint tests
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void MapPoint_IndexZero_ReturnsLeftEdge()
        {
            Vector2 pt = LineGraphGraphic.GraphLayout.MapPoint(
                value: 25f, index: 0, count: 5, rect: TestRect, paddedMin: 0f, paddedMax: 50f);

            Assert.AreEqual(TestRect.xMin, pt.x, 0.001f, "Index 0 should map to left edge");
        }

        [Test]
        public void MapPoint_LastIndex_ReturnsRightEdge()
        {
            Vector2 pt = LineGraphGraphic.GraphLayout.MapPoint(
                value: 25f, index: 4, count: 5, rect: TestRect, paddedMin: 0f, paddedMax: 50f);

            Assert.AreEqual(TestRect.xMax, pt.x, 0.001f, "Last index should map to right edge");
        }

        [Test]
        public void MapPoint_ValueAtPaddedMin_ReturnsBottomOfRect()
        {
            Vector2 pt = LineGraphGraphic.GraphLayout.MapPoint(
                value: 0f, index: 2, count: 5, rect: TestRect, paddedMin: 0f, paddedMax: 50f);

            Assert.AreEqual(TestRect.yMin, pt.y, 0.001f, "Value == paddedMin should map to bottom");
        }

        [Test]
        public void MapPoint_ValueAtPaddedMax_ReturnsTopOfRect()
        {
            Vector2 pt = LineGraphGraphic.GraphLayout.MapPoint(
                value: 50f, index: 2, count: 5, rect: TestRect, paddedMin: 0f, paddedMax: 50f);

            Assert.AreEqual(TestRect.yMax, pt.y, 0.001f, "Value == paddedMax should map to top");
        }
    }
}
