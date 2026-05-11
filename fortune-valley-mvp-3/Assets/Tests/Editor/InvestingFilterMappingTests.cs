using NUnit.Framework;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.Panels.Investing;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Validates that the serialized mapping arrays in
    /// InvestingFilterableSubPanelBase correctly map button indices
    /// to domain enum values. Uses a concrete test subclass to
    /// access the protected mapping methods.
    /// </summary>
    [TestFixture]
    public class InvestingFilterMappingTests
    {
        private GameObject _testGo;
        private TestFilterablePanel _panel;

        private static readonly System.Reflection.BindingFlags Flags =
            System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance;

        [SetUp]
        public void SetUp()
        {
            _testGo = new GameObject("TestPanel");
            _panel = _testGo.AddComponent<TestFilterablePanel>();

            // Wire the mapping arrays via reflection (simulating Inspector)
            var baseType = typeof(InvestingFilterableSubPanelBase);

            baseType.GetField("_categoryMapping", Flags)?.SetValue(_panel,
                new[] {
                    InvestmentCategory.Stock,
                    InvestmentCategory.ETF,
                    InvestmentCategory.Bond,
                    InvestmentCategory.TBill
                });

            baseType.GetField("_industryMapping", Flags)?.SetValue(_panel,
                new[] {
                    Industry.Technology,
                    Industry.Financials,
                    Industry.Energy,
                    Industry.ConsumerGoods,
                    Industry.Healthcare,
                    Industry.Industrials
                });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_testGo);
        }

        // ─── Category mapping ───────────────────────────────────────────

        [Test]
        public void CategoryMapping_ZeroReturnsNull()
        {
            Assert.IsNull(_panel.TestMapCategory(0));
        }

        [Test]
        public void CategoryMapping_OneReturnsStock()
        {
            Assert.AreEqual(InvestmentCategory.Stock, _panel.TestMapCategory(1));
        }

        [Test]
        public void CategoryMapping_TwoReturnsETF()
        {
            Assert.AreEqual(InvestmentCategory.ETF, _panel.TestMapCategory(2));
        }

        [Test]
        public void CategoryMapping_ThreeReturnsBond()
        {
            Assert.AreEqual(InvestmentCategory.Bond, _panel.TestMapCategory(3));
        }

        [Test]
        public void CategoryMapping_FourReturnsTBill()
        {
            Assert.AreEqual(InvestmentCategory.TBill, _panel.TestMapCategory(4));
        }

        [Test]
        public void CategoryMapping_OutOfRange_ReturnsNull()
        {
            Assert.IsNull(_panel.TestMapCategory(99));
        }

        [Test]
        public void CategoryMapping_Negative_ReturnsNull()
        {
            Assert.IsNull(_panel.TestMapCategory(-1));
        }

        // ─── Industry mapping ───────────────────────────────────────────

        [Test]
        public void IndustryMapping_ZeroReturnsNull()
        {
            Assert.IsNull(_panel.TestMapIndustry(0));
        }

        [Test]
        public void IndustryMapping_OneReturnsTechnology()
        {
            Assert.AreEqual(Industry.Technology, _panel.TestMapIndustry(1));
        }

        [Test]
        public void IndustryMapping_TwoReturnsFinancials()
        {
            Assert.AreEqual(Industry.Financials, _panel.TestMapIndustry(2));
        }

        [Test]
        public void IndustryMapping_ThreeReturnsEnergy()
        {
            Assert.AreEqual(Industry.Energy, _panel.TestMapIndustry(3));
        }

        [Test]
        public void IndustryMapping_FourReturnsConsumerGoods()
        {
            Assert.AreEqual(Industry.ConsumerGoods, _panel.TestMapIndustry(4));
        }

        [Test]
        public void IndustryMapping_FiveReturnsHealthcare()
        {
            Assert.AreEqual(Industry.Healthcare, _panel.TestMapIndustry(5));
        }

        [Test]
        public void IndustryMapping_SixReturnsIndustrials()
        {
            Assert.AreEqual(Industry.Industrials, _panel.TestMapIndustry(6));
        }

        [Test]
        public void IndustryMapping_OutOfRange_ReturnsNull()
        {
            Assert.IsNull(_panel.TestMapIndustry(99));
        }

        // ─── Null array safety ──────────────────────────────────────────

        [Test]
        public void CategoryMapping_NullArray_ReturnsNull()
        {
            var baseType = typeof(InvestingFilterableSubPanelBase);
            baseType.GetField("_categoryMapping", Flags)?.SetValue(_panel, null);

            Assert.IsNull(_panel.TestMapCategory(1));
        }

        [Test]
        public void IndustryMapping_NullArray_ReturnsNull()
        {
            var baseType = typeof(InvestingFilterableSubPanelBase);
            baseType.GetField("_industryMapping", Flags)?.SetValue(_panel, null);

            Assert.IsNull(_panel.TestMapIndustry(1));
        }

        // ─── Concrete test subclass ─────────────────────────────────────

        /// <summary>
        /// Minimal concrete subclass exposing the protected mapping methods.
        /// </summary>
        private class TestFilterablePanel : InvestingFilterableSubPanelBase
        {
            protected override void Refresh() { }

            public InvestmentCategory? TestMapCategory(int index)
                => MapCategoryIndex(index);

            public Industry? TestMapIndustry(int index)
                => MapIndustryIndex(index);
        }
    }
}
