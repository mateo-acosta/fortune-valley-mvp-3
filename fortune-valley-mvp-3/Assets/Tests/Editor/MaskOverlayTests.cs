using NUnit.Framework;
using UnityEngine;
using FortuneValley.UI.Tutorial;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class MaskOverlayTests
    {
        // 1920x1080 parent, hole centered-ish, no padding.
        [Test]
        public void ComputeDonut_FullCoverage_FourEdgesTileAroundHole()
        {
            var donut = MaskOverlay.ComputeDonut(1920f, 1080f,
                new Rect(800f, 400f, 200f, 100f), padding: 0f);

            // Hole spans X:[800,1000], Y:[400,500]
            Assert.AreEqual(Rect.MinMaxRect(0f, 500f, 1920f, 1080f), donut.top, "top");
            Assert.AreEqual(Rect.MinMaxRect(0f, 0f, 1920f, 400f), donut.bottom, "bottom");
            Assert.AreEqual(Rect.MinMaxRect(0f, 400f, 800f, 500f), donut.left, "left");
            Assert.AreEqual(Rect.MinMaxRect(1000f, 400f, 1920f, 500f), donut.right, "right");
        }

        [Test]
        public void ComputeDonut_PaddingExpandsHole()
        {
            var donut = MaskOverlay.ComputeDonut(1920f, 1080f,
                new Rect(800f, 400f, 200f, 100f), padding: 20f);

            // Hole padded to X:[780,1020], Y:[380,520]
            Assert.AreEqual(Rect.MinMaxRect(0f, 520f, 1920f, 1080f), donut.top, "top");
            Assert.AreEqual(Rect.MinMaxRect(0f, 0f, 1920f, 380f), donut.bottom, "bottom");
            Assert.AreEqual(Rect.MinMaxRect(0f, 380f, 780f, 520f), donut.left, "left");
            Assert.AreEqual(Rect.MinMaxRect(1020f, 380f, 1920f, 520f), donut.right, "right");
        }

        [Test]
        public void ComputeDonut_TargetHuggingTopEdge_ClipsToParent()
        {
            // Target starts 50px from top-right, with padding 30 it would run
            // off the top by 10px -- must clip to parent height.
            var donut = MaskOverlay.ComputeDonut(1920f, 1080f,
                new Rect(1700f, 1050f, 200f, 30f), padding: 30f);

            // Top rect collapses to zero height (hole reaches the top edge).
            Assert.AreEqual(1080f, donut.top.yMin, 0.01f, "top.yMin hits parent height");
            Assert.AreEqual(1080f, donut.top.yMax, 0.01f, "top.yMax also at parent height");
            Assert.AreEqual(0f, donut.top.height, 0.01f);

            // Right rect extends to the right edge (hole touches it with padding).
            Assert.AreEqual(1920f, donut.right.xMax, 0.01f);
        }

        [Test]
        public void ComputeDonut_NegativeTargetCoords_ClampToZero()
        {
            var donut = MaskOverlay.ComputeDonut(1000f, 800f,
                new Rect(-50f, -50f, 100f, 100f), padding: 0f);

            // Hole's lower-left gets clamped to (0,0).
            Assert.AreEqual(0f, donut.bottom.yMax, 0.01f, "bottom collapses");
            Assert.AreEqual(0f, donut.left.xMax, 0.01f, "left collapses");
        }

        [Test]
        public void ComputeDonut_FourRectsAreNonOverlappingPartition()
        {
            // Sum of 4 rect areas + hole area (padded) should equal parent area.
            var parentW = 1600f;
            var parentH = 900f;
            var target = new Rect(500f, 300f, 400f, 200f);
            var padding = 10f;

            var d = MaskOverlay.ComputeDonut(parentW, parentH, target, padding);

            float donutArea = d.top.width * d.top.height
                            + d.bottom.width * d.bottom.height
                            + d.left.width * d.left.height
                            + d.right.width * d.right.height;
            float holeW = (target.width + 2f * padding);
            float holeH = (target.height + 2f * padding);
            float holeArea = holeW * holeH;

            Assert.AreEqual(parentW * parentH, donutArea + holeArea, 0.5f,
                "donut + hole must tile the parent");
        }
    }
}
