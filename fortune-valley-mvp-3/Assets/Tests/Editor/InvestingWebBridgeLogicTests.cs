using NUnit.Framework;
using FortuneValley.Domain.Entities.WebPanels;
using FortuneValley.Managers.WebPanels;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for InvestingWebBridgeLogic. Focuses on the pure
    /// logic that does not require constructing live MonoBehaviour
    /// systems. Full integration of PopulateDTO with real
    /// InvestmentSystem state is exercised by PlayMode tests.
    /// </summary>
    [TestFixture]
    public class InvestingWebBridgeLogicTests
    {
        [Test]
        public void PopulateDTO_WithNullTarget_ReturnsFalse()
        {
            var logic = new InvestingWebBridgeLogic();
            Assert.IsFalse(logic.PopulateDTO(null));
        }

        [Test]
        public void PopulateDTO_WithoutInitialize_ReturnsFalse()
        {
            // Dependencies never wired -> push must be skipped silently.
            var logic = new InvestingWebBridgeLogic();
            var dto = new InvestingPanelDTO();
            Assert.IsFalse(logic.PopulateDTO(dto));
        }

        [Test]
        public void StripRiskSuffix_LowRisk_ReturnsLow()
        {
            Assert.AreEqual("Low", InvestingWebBridgeLogic.StripRiskSuffix("Low Risk"));
        }

        [Test]
        public void StripRiskSuffix_MediumRisk_ReturnsMedium()
        {
            Assert.AreEqual("Medium", InvestingWebBridgeLogic.StripRiskSuffix("Medium Risk"));
        }

        [Test]
        public void StripRiskSuffix_HighRisk_ReturnsHigh()
        {
            Assert.AreEqual("High", InvestingWebBridgeLogic.StripRiskSuffix("High Risk"));
        }

        [Test]
        public void StripRiskSuffix_NoHoldings_PassesThrough()
        {
            // PortfolioPanelLogic returns "No Holdings" with no " Risk" suffix.
            // The HTML treats this as a special-case label.
            Assert.AreEqual("No Holdings", InvestingWebBridgeLogic.StripRiskSuffix("No Holdings"));
        }

        [Test]
        public void StripRiskSuffix_EmptyString_ReturnsEmpty()
        {
            Assert.AreEqual("", InvestingWebBridgeLogic.StripRiskSuffix(""));
        }

        [Test]
        public void StripRiskSuffix_NullInput_ReturnsNull()
        {
            Assert.IsNull(InvestingWebBridgeLogic.StripRiskSuffix(null));
        }

        [Test]
        public void StripRiskSuffix_LabelWithoutSuffix_PassesThrough()
        {
            // Defensive: if PortfolioPanelLogic ever returns a bare token,
            // we should not strip an arbitrary suffix.
            Assert.AreEqual("Low", InvestingWebBridgeLogic.StripRiskSuffix("Low"));
        }
    }
}
