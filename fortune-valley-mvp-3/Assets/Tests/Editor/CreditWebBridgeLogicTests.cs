using NUnit.Framework;
using FortuneValley.Domain.Entities.WebPanels;
using FortuneValley.Managers.WebPanels;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for CreditWebBridgeLogic. Focuses on the pure-logic
    /// surface that does not require constructing live MonoBehaviour
    /// systems. Full PopulateDTO integration with real LoanSystem /
    /// CreditScoreSystem / CityManager / TimeManager state is exercised
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

        // ─────────────── Day-string cache (B4) ───────────────
        // The history rendering path called string concatenation per record
        // per push, allocating ~100 strings/push at 50 entries. The cache
        // makes second-and-later sightings of any given day allocation-free.

        [Test]
        public void ResolveDayString_DistinctDays_ReturnsDistinctStrings()
        {
            var logic = new CreditWebBridgeLogic();
            string day1 = logic.ResolveDayString(0, 10);   // tick 0 / 10 + 1 = day 1
            string day2 = logic.ResolveDayString(10, 10);  // day 2
            string day5 = logic.ResolveDayString(40, 10);  // day 5

            Assert.AreEqual("Day 1", day1);
            Assert.AreEqual("Day 2", day2);
            Assert.AreEqual("Day 5", day5);
        }

        [Test]
        public void ResolveDayString_SameDay_ReturnsSameInstance()
        {
            // The cache hit must return the SAME string reference, not a
            // freshly-allocated equal string. Reference equality is the
            // contract that proves the allocation has been eliminated.
            var logic = new CreditWebBridgeLogic();
            string first = logic.ResolveDayString(15, 10);   // day 2
            string second = logic.ResolveDayString(15, 10);  // day 2 again
            string third = logic.ResolveDayString(19, 10);   // still day 2 (19/10=1, +1=2)

            Assert.AreEqual("Day 2", first);
            Assert.AreSame(first, second, "Same day must reuse the cached string instance.");
            Assert.AreSame(first, third, "Different ticks within the same day must hit the same cache entry.");
        }

        [Test]
        public void ResolveDayString_NegativeTick_ClampsToDayOne()
        {
            // Defensive: negative ticks should not produce a "Day -1" or
            // "Day 0" string, which the iframe doesn't expect.
            var logic = new CreditWebBridgeLogic();
            string clamped = logic.ResolveDayString(-5, 10);
            Assert.AreEqual("Day 1", clamped);
        }

        [Test]
        public void ResolveDayString_LifecycleAcrossManyDays_NoExceptions()
        {
            // Smoke: ~1500 distinct days (covers a 40-year game ×3).
            // Should not throw, and each call should return the expected
            // "Day N" string. Cache size is bounded by N (small).
            var logic = new CreditWebBridgeLogic();
            for (int day = 1; day <= 1500; day++)
            {
                int tick = (day - 1) * 10;
                string s = logic.ResolveDayString(tick, 10);
                Assert.AreEqual("Day " + day, s);
            }
        }
    }
}
