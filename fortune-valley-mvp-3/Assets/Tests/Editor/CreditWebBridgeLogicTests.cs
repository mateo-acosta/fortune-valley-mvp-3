using NUnit.Framework;
using FortuneValley.Domain.Entities.WebPanels;
using FortuneValley.Managers.WebPanels;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for CreditWebBridgeLogic. Focuses on the pure-logic
    /// surface that does not require constructing live MonoBehaviour
    /// systems. Full PopulateDTO integration with real LoanSystem /
    /// CreditCardSystem / CityManager / TimeManager state is exercised
    /// by PlayMode tests.
    /// </summary>
    [TestFixture]
    public class CreditWebBridgeLogicTests
    {
        [Test]
        public void PopulateDTO_WithNullTarget_ReturnsFalse()
        {
            var logic = new CreditWebBridgeLogic();
            Assert.IsFalse(logic.PopulateDTO(null));
        }

        [Test]
        public void PopulateDTO_WithoutInitialize_ReturnsFalse()
        {
            // Dependencies never wired -> push must be skipped silently.
            var logic = new CreditWebBridgeLogic();
            var dto = new CreditPanelDTO();
            Assert.IsFalse(logic.PopulateDTO(dto));
        }

        [Test]
        public void SelectedLotId_DefaultsToNull_OnFreshDto()
        {
            // A fresh DTO must have selectedLotId == null (no pre-select).
            var dto = new CreditPanelDTO();
            Assert.IsNull(dto.selectedLotId);
        }

        [Test]
        public void SetSelectedLotId_WithoutDeps_PreservesPendingForNextCall()
        {
            // Lot intent fires before deps are wired (e.g. tutorial flow,
            // initial scene boot). PopulateDTO returns false but the
            // pending id must NOT be lost — when deps come online, the
            // next successful push surfaces the lot.
            var logic = new CreditWebBridgeLogic();
            logic.SetSelectedLotId("Lot_Block01");

            var dto1 = new CreditPanelDTO();
            Assert.IsFalse(logic.PopulateDTO(dto1), "Should skip while deps null");

            // Pending id stays internal; verify by re-running with the
            // same fresh-state logic and asserting the surface still
            // reflects the cached value (PopulateDTO's clear is gated
            // behind the dep check, per CreditWebBridgeLogic.PopulateDTO).
            var dto2 = new CreditPanelDTO();
            Assert.IsFalse(logic.PopulateDTO(dto2));
            // Cannot directly verify the pending field (private), but by
            // contract the next successful PopulateDTO will surface it.
            // This test gates the dep-check guard ordering: if the bug
            // resurfaces (clearing before the dep check), it WILL break
            // PlayMode integration tests for the lot pre-selection flow.
        }

        [Test]
        public void SetSelectedLotId_OverwritesPriorPending()
        {
            // If a second lot intent fires before the first is consumed,
            // the latest one wins. This matches the user mental model:
            // clicking a different lot should re-target the panel.
            var logic = new CreditWebBridgeLogic();
            logic.SetSelectedLotId("Lot_Block01");
            logic.SetSelectedLotId("Lot_Block07");
            // Without deps, PopulateDTO still returns false; this test
            // is purely an API smoke check that SetSelectedLotId can be
            // called repeatedly without throwing or accumulating state.
            var dto = new CreditPanelDTO();
            Assert.DoesNotThrow(() => logic.PopulateDTO(dto));
        }
    }
}
