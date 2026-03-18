using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using FortuneValley.UI.Components;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Unit tests for LineGraphGraphic.GraphLayout static helpers.
    /// These are pure math/data functions — no Canvas or MonoBehaviour setup needed.
    /// </summary>
    [TestFixture]
    public class LineGraphGraphicTests
    {
        private static readonly Rect TestRect = new Rect(0f, 0f, 100f, 50f);

        // ═══════════════════════════════════════════════════════════════
        // CopyToList tests
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void CopyToList_NullSource_TargetIsEmptyNoException()
        {
            // Null source must not throw — target should simply be cleared
            var target = new List<float> { 1f, 2f, 3f };
            Assert.DoesNotThrow(() =>
                LineGraphGraphic.GraphLayout.CopyToList(null, target));
            Assert.IsEmpty(target, "Target should be empty when source is null");
        }

        [Test]
        public void CopyToList_EmptySource_TargetIsEmpty()
        {
            var target = new List<float> { 1f, 2f };
            LineGraphGraphic.GraphLayout.CopyToList(new List<float>(), target);
            Assert.IsEmpty(target);
        }

        [Test]
        public void CopyToList_NormalSource_TargetMatchesSource()
        {
            var source = new List<float> { 10f, 20f, 30f };
            var target = new List<float>();
            LineGraphGraphic.GraphLayout.CopyToList(source, target);
            Assert.AreEqual(source.Count, target.Count);
            for (int i = 0; i < source.Count; i++)
                Assert.AreEqual(source[i], target[i], 0.001f);
        }

        // ═══════════════════════════════════════════════════════════════
        // ComputeYRange with combined primary+secondary data
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void ComputeYRange_BothListsPositive_RangeSpansBothAndPaddedMinPositive()
        {
            // When all values are positive the padded min should still be > 0
            var combined = new List<float> { 100f, 200f, 50f, 300f }; // primary + secondary merged
            var (paddedMin, paddedMax) = LineGraphGraphic.GraphLayout.ComputeYRange(combined);
            Assert.Greater(paddedMin, 0f,    "paddedMin should be > 0 when all values are positive");
            Assert.Greater(paddedMax, 300f,  "paddedMax should exceed highest value 300");
            Assert.Less(paddedMin,    50f,   "paddedMin should be below lowest value 50");
        }

        [Test]
        public void ComputeYRange_SecondaryHasNegativeValues_PaddedMinIsNegative()
        {
            // Losses in the secondary series must pull the floor below zero so they're visible
            var combined = new List<float> { 100f, 200f, -50f, 0f }; // primary then secondary
            var (paddedMin, _) = LineGraphGraphic.GraphLayout.ComputeYRange(combined);
            Assert.Less(paddedMin, 0f, "paddedMin should be < 0 when secondary has negative values");
        }

        [Test]
        public void ComputeYRange_SecondaryEmpty_RangeEqualsPrimaryOnly()
        {
            // When secondary is not added to the combined list, range must equal primary only
            var combined = new List<float> { 100f, 200f };
            var (paddedMin, paddedMax) = LineGraphGraphic.GraphLayout.ComputeYRange(combined);
            float range       = 200f - 100f; // 100
            float expectedMin = 100f - range * 0.08f;
            float expectedMax = 200f + range * 0.08f;
            Assert.AreEqual(expectedMin, paddedMin, 0.001f);
            Assert.AreEqual(expectedMax, paddedMax, 0.001f);
        }

        [Test]
        public void ComputeYRange_FlatCombinedData_MinimumRangeApplied()
        {
            // Flat combined data (all identical values) must apply the 1f range floor
            var combined = new List<float> { 50f, 50f, 50f, 50f };
            var (paddedMin, paddedMax) = LineGraphGraphic.GraphLayout.ComputeYRange(combined);
            float expectedMin = 50f - 1f * 0.08f;
            float expectedMax = 50f + 1f * 0.08f;
            Assert.AreEqual(expectedMin, paddedMin, 0.001f);
            Assert.AreEqual(expectedMax, paddedMax, 0.001f);
        }

        // ═══════════════════════════════════════════════════════════════
        // MapPoint — each series uses its own count so edges always align
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void MapPoint_Primary30Pts_Index0_ReturnsXMin()
        {
            var pt = LineGraphGraphic.GraphLayout.MapPoint(0f, 0, 30, TestRect, 0f, 100f);
            Assert.AreEqual(TestRect.xMin, pt.x, 0.001f,
                "Primary series index 0 should map to left edge");
        }

        [Test]
        public void MapPoint_Primary30Pts_LastIndex_ReturnsXMax()
        {
            var pt = LineGraphGraphic.GraphLayout.MapPoint(0f, 29, 30, TestRect, 0f, 100f);
            Assert.AreEqual(TestRect.xMax, pt.x, 0.001f,
                "Primary series last index should map to right edge");
        }

        [Test]
        public void MapPoint_Secondary15Pts_Index0_ReturnsXMin()
        {
            // Secondary series is shorter but must still span full width
            var pt = LineGraphGraphic.GraphLayout.MapPoint(0f, 0, 15, TestRect, 0f, 100f);
            Assert.AreEqual(TestRect.xMin, pt.x, 0.001f,
                "Secondary series index 0 should map to left edge");
        }

        [Test]
        public void MapPoint_Secondary15Pts_LastIndex_ReturnsXMax()
        {
            var pt = LineGraphGraphic.GraphLayout.MapPoint(0f, 14, 15, TestRect, 0f, 100f);
            Assert.AreEqual(TestRect.xMax, pt.x, 0.001f,
                "Secondary series last index should map to right edge");
        }
    }
}
