using NUnit.Framework;
using UnityEngine;
using FortuneValley.UI;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Runtime tests for UIManager popup stack and overlay management.
    /// </summary>
    [TestFixture]
    public class UIManagerTests
    {
        private GameObject _managerGo;
        private UIManager _uiManager;
        private GameObject _overlayGo;
        private TestPopup _popupA;
        private TestPopup _popupB;

        /// <summary>
        /// Minimal UIPopup subclass for testing.
        /// </summary>
        private class TestPopup : UIPopup { }

        [SetUp]
        public void SetUp()
        {
            _managerGo = new GameObject("TestUIManager");
            _uiManager = _managerGo.AddComponent<UIManager>();

            // Create overlay
            _overlayGo = new GameObject("Overlay");
            _overlayGo.SetActive(false);
            SetPrivateField(_uiManager, "_popupOverlay", _overlayGo);

            // Create test popups with popup roots so Show/Hide doesn't deactivate the component itself
            _popupA = CreateTestPopup("PopupA");
            _popupB = CreateTestPopup("PopupB");
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_popupA.gameObject);
            Object.Destroy(_popupB.gameObject);
            Object.Destroy(_overlayGo);
            Object.Destroy(_managerGo);
        }

        private TestPopup CreateTestPopup(string name)
        {
            var go = new GameObject(name);
            var popup = go.AddComponent<TestPopup>();
            // Create a child as the popup root so Show/Hide toggles the child
            var root = new GameObject(name + "_Root");
            root.transform.SetParent(go.transform);
            root.SetActive(false);
            SetPrivateField(popup, "_popupRoot", root);
            return popup;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return;
                }
                type = type.BaseType;
            }
        }

        [Test]
        public void ShowPopup_ActivatesOverlay()
        {
            _uiManager.ShowPopup(_popupA);

            Assert.IsTrue(_overlayGo.activeSelf);
        }

        [Test]
        public void HidePopup_DeactivatesOverlay()
        {
            _uiManager.ShowPopup(_popupA);
            _uiManager.HidePopup(_popupA);

            Assert.IsFalse(_overlayGo.activeSelf);
        }

        [Test]
        public void ShowPopup_PushesToStack()
        {
            _uiManager.ShowPopup(_popupA);

            Assert.IsTrue(_uiManager.IsPopupOpen);
        }

        [Test]
        public void HidePopup_RemovesFromStack()
        {
            _uiManager.ShowPopup(_popupA);
            _uiManager.HidePopup(_popupA);

            Assert.IsFalse(_uiManager.IsPopupOpen);
        }

        [Test]
        public void ShowPanel_Portfolio_SetsPortfolioPanelVisible()
        {
            var portfolioGO = new GameObject("PortfolioPanel");
            var portfolioPanel = portfolioGO.AddComponent<UIPanel>();
            SetPrivateField(_uiManager, "_portfolioPanel", portfolioPanel);

            _uiManager.ShowPanel(PanelType.Portfolio);

            Assert.IsTrue(portfolioPanel.IsVisible);
            Object.Destroy(portfolioGO);
        }

        [Test]
        public void ShowPanel_Lots_HidesPreviousPanel()
        {
            var portfolioGO = new GameObject("PortfolioPanel");
            var portfolioPanel = portfolioGO.AddComponent<UIPanel>();
            SetPrivateField(_uiManager, "_portfolioPanel", portfolioPanel);

            var lotsGO = new GameObject("LotsPanel");
            var lotsPanel = lotsGO.AddComponent<UIPanel>();
            SetPrivateField(_uiManager, "_lotsPanel", lotsPanel);

            _uiManager.ShowPanel(PanelType.Portfolio);
            _uiManager.ShowPanel(PanelType.Lots);

            Assert.IsTrue(lotsPanel.IsVisible);
            Assert.IsFalse(portfolioPanel.IsVisible);

            Object.Destroy(portfolioGO);
            Object.Destroy(lotsGO);
        }

        [Test]
        public void HideCurrentPanel_AfterShow_SetsIsVisibleFalse()
        {
            var portfolioGO = new GameObject("PortfolioPanel");
            var portfolioPanel = portfolioGO.AddComponent<UIPanel>();
            SetPrivateField(_uiManager, "_portfolioPanel", portfolioPanel);

            _uiManager.ShowPanel(PanelType.Portfolio);
            Assert.IsTrue(portfolioPanel.IsVisible);

            _uiManager.HideCurrentPanel();
            Assert.IsFalse(portfolioPanel.IsVisible);

            Object.Destroy(portfolioGO);
        }

        [Test]
        public void ShowPopup_MultiplePopups_OverlayStaysActive()
        {
            _uiManager.ShowPopup(_popupA);
            _uiManager.ShowPopup(_popupB);

            // Hide one — overlay should stay active
            _uiManager.HidePopup(_popupA);
            Assert.IsTrue(_overlayGo.activeSelf);

            // Hide the last — overlay should deactivate
            _uiManager.HidePopup(_popupB);
            Assert.IsFalse(_overlayGo.activeSelf);
        }
    }
}
