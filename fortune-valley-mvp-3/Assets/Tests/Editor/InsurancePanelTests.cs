using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.UI.Panels;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for InsurancePanel.
    /// Verifies event handlers respect IsVisible guard.
    /// </summary>
    [TestFixture]
    public class InsurancePanelTests
    {
        private GameObject _go;
        private InsurancePanel _panel;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestInsurancePanel");
            _panel = _go.AddComponent<InsurancePanel>();
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
        public void InsurancePurchased_WhileHidden_DoesNotThrow()
        {
            // Panel starts hidden (IsVisible = false by default on UIPanel).
            // Firing the event should not cause errors since RefreshList
            // is guarded by IsVisible check.
            Assert.DoesNotThrow(() =>
            {
                GameEvents.RaiseInsurancePurchased("lot_1", "policy_1");
            });
        }

        [Test]
        public void InsuranceCanceled_WhileHidden_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameEvents.RaiseInsuranceCanceled("lot_1", FortuneValley.Domain.Enums.InsurancePolicyType.GeneralProtection);
            });
        }

        [Test]
        public void LotPurchased_WhileHidden_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameEvents.RaiseLotPurchased("lot_1", FortuneValley.Domain.Enums.Owner.Player);
            });
        }
    }
}
