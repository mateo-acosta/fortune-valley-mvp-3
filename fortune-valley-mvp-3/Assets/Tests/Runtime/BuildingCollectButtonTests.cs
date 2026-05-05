using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.UI.World;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Coverage for the non-interactive flash-indicator BuildingCollectButton.
    /// The button no longer participates in collection (taps are removed); it
    /// flashes when its building's income lands at day-end and reveals on
    /// hover for a static "+$X/day" rate readout.
    /// </summary>
    [TestFixture]
    public class BuildingCollectButtonTests
    {
        private const string BuildingId = "lot_test";

        private GameObject _serviceGO;
        private DailyIncomeAccumulator _service;

        private GameObject _canvasGO;
        private GameObject _buttonGO;
        private BuildingCollectButton _button;
        private Image _fillImage;
        private Image _coinTintImage;
        private Button _uiButton;
        private TextMeshProUGUI _label;
        private CanvasGroup _visibilityGroup;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();

            _serviceGO = new GameObject("Accumulator");
            _service = _serviceGO.AddComponent<DailyIncomeAccumulator>();
            _service.Initialize(new TestLotRegistryLocal(), new TestTickClockLocal { TicksPerDay = 10 });

            _canvasGO = new GameObject("Canvas");
            _canvasGO.AddComponent<Canvas>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_button != null) InvokePrivate(_button, "OnDisable");
            if (_buttonGO != null) Object.DestroyImmediate(_buttonGO);
            Object.DestroyImmediate(_canvasGO);
            Object.DestroyImmediate(_serviceGO);
            GameEvents.ClearAllSubscriptions();
        }

        // ═══════════════════════════════════════════════════════════════
        // Non-interactive contract
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Button_NeverInteractable_OnAwake()
        {
            CreateButton();
            Assert.IsFalse(_uiButton.interactable);
        }

        [Test]
        public void Awake_DisablesFillImage_AtRuntime()
        {
            CreateButton();
            Assert.IsFalse(_fillImage.gameObject.activeSelf,
                "Fill image is hidden at runtime (no pending pot in the new model).");
        }

        [Test]
        public void OnEnable_StartsHidden_WithCanvasGroupAlphaZero()
        {
            CreateButton();
            Assert.AreEqual(0f, _visibilityGroup.alpha);
            Assert.IsFalse(_visibilityGroup.blocksRaycasts);
        }

        // ═══════════════════════════════════════════════════════════════
        // Hover-driven visibility
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void Hover_OnMatchingId_RevealsCanvasGroup()
        {
            CreateButton();
            GameEvents.RaiseBlockHoverChanged(BuildingId, true);
            Assert.AreEqual(1f, _visibilityGroup.alpha);
        }

        [Test]
        public void Hover_OnOtherId_DoesNotReveal()
        {
            CreateButton();
            GameEvents.RaiseBlockHoverChanged("someone_else", true);
            Assert.AreEqual(0f, _visibilityGroup.alpha);
        }

        [Test]
        public void HoverEnd_HidesCanvasGroup()
        {
            CreateButton();
            GameEvents.RaiseBlockHoverChanged(BuildingId, true);
            GameEvents.RaiseBlockHoverChanged(BuildingId, false);
            Assert.AreEqual(0f, _visibilityGroup.alpha);
        }

        // ═══════════════════════════════════════════════════════════════
        // Label format switching: rate (hover/state) vs deposit (flash)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void CoinStateChanged_UpdatesLabel_ToRateFormat()
        {
            CreateButton();

            GameEvents.RaiseCoinStateChanged(BuildingId, 80f, 0f, false);

            Assert.AreEqual("+$80/day", _label.text);
        }

        [Test]
        public void CoinStateChanged_ForDifferentId_IsIgnored()
        {
            CreateButton();
            string baseline = _label.text;

            GameEvents.RaiseCoinStateChanged("someone_else", 999f, 0f, true);

            Assert.AreEqual(baseline, _label.text);
        }

        [Test]
        public void IncomeCollected_ForMatchingId_UpdatesLabelToDepositFormat()
        {
            CreateButton();
            GameEvents.RaiseCoinStateChanged(BuildingId, 100f, 0f, false); // sets baseline rate label

            GameEvents.RaiseIncomeCollected(BuildingId, 75f);

            // Flash label is "+$75" (no /day suffix). Format runs synchronously
            // before the DOTween sequence's onComplete restores the rate label.
            Assert.AreEqual("+$75", _label.text);
        }

        [Test]
        public void IncomeCollected_ForOtherId_DoesNotUpdateLabel()
        {
            CreateButton();
            GameEvents.RaiseCoinStateChanged(BuildingId, 100f, 0f, false);
            string baseline = _label.text;

            GameEvents.RaiseIncomeCollected("someone_else", 75f);

            Assert.AreEqual(baseline, _label.text);
        }

        // ═══════════════════════════════════════════════════════════════
        // Flash mid-state suppresses label flicker (decision 8A)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void CoinStateChanged_DuringFlash_DoesNotOverwriteFlashLabel()
        {
            CreateButton();
            GameEvents.RaiseCoinStateChanged(BuildingId, 100f, 0f, false);

            // Trigger a flash; label is now "+$50".
            GameEvents.RaiseIncomeCollected(BuildingId, 50f);
            Assert.AreEqual("+$50", _label.text);

            // A rate-state event arriving mid-flash must not overwrite the
            // deposit label.
            GameEvents.RaiseCoinStateChanged(BuildingId, 200f, 0f, false);

            Assert.AreEqual("+$50", _label.text,
                "_isFlashing guard prevents the rate label from clobbering the flash deposit label.");
        }

        // ═══════════════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════════════

        private void CreateButton()
        {
            _buttonGO = new GameObject("CollectCoin", typeof(RectTransform));
            _buttonGO.transform.SetParent(_canvasGO.transform);

            var fillGO = new GameObject("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(_buttonGO.transform);
            _fillImage = fillGO.AddComponent<Image>();
            _fillImage.type = Image.Type.Filled;
            _fillImage.fillMethod = Image.FillMethod.Radial360;

            var tintGO = new GameObject("Tint", typeof(RectTransform));
            tintGO.transform.SetParent(_buttonGO.transform);
            _coinTintImage = tintGO.AddComponent<Image>();

            _uiButton = _buttonGO.AddComponent<Button>();
            _visibilityGroup = _buttonGO.AddComponent<CanvasGroup>();

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(_buttonGO.transform);
            _label = labelGO.AddComponent<TextMeshProUGUI>();

            _button = _buttonGO.AddComponent<BuildingCollectButton>();
            SetField(_button, "_fillImage", _fillImage);
            SetField(_button, "_coinTintImage", _coinTintImage);
            SetField(_button, "_button", _uiButton);
            SetField(_button, "_amountLabel", _label);
            SetField(_button, "_visibilityGroup", _visibilityGroup);
            SetField(_button, "_buildingId", BuildingId);

            InvokePrivate(_button, "Awake");
            InvokePrivate(_button, "OnEnable");
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var m = target.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (m != null) m.Invoke(target, null);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (field != null) { field.SetValue(target, value); return; }
                type = type.BaseType;
            }
            throw new System.Exception($"Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }
}
