using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.UI.Panels;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for LoanPanel.
    /// Verifies event handlers respect IsVisible guard.
    /// </summary>
    [TestFixture]
    public class LoanPanelTests
    {
        private GameObject _go;
        private LoanPanel _panel;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestLoanPanel");
            _panel = _go.AddComponent<LoanPanel>();
            GameEvents.RaiseGameStart();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            GameEvents.ClearAllSubscriptions();
        }

        // ===============================================================
        // ISVISIBLE GUARD
        // ===============================================================

        [Test]
        public void LoanOriginated_WhileHidden_DoesNotThrow()
        {
            // Panel starts hidden (IsVisible = false by default on UIPanel).
            // Firing the event should not cause errors since RefreshList
            // is guarded by IsVisible check.
            Assert.DoesNotThrow(() =>
            {
                GameEvents.RaiseLoanOriginated(null);
            });
        }

        [Test]
        public void LoanPaymentMade_WhileHidden_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameEvents.RaiseLoanPaymentMade(null, 100f);
            });
        }

        [Test]
        public void LoanPaidOff_WhileHidden_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameEvents.RaiseLoanPaidOff(null);
            });
        }
    }
}
