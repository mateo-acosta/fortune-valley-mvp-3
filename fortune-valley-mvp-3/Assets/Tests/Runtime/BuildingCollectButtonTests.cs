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
    /// Coin button lifecycle + subscription-race regression coverage.
    ///
    /// Subscription race: BuildingCollectButton must seed its visual state
    /// correctly even when it enables AFTER the PendingIncomeService has
    /// already emitted its last OnCoinStateChanged event. The button raises
    /// OnIncomePendingQuery on OnEnable and the service re-emits.
    /// </summary>
    [TestFixture]
    public class BuildingCollectButtonTests
    {
        private const string BuildingId = "lot_test";

        private GameObject _serviceGO;
        private PendingIncomeService _service;

        private GameObject _canvasGO;
        private GameObject _buttonGO;
        private BuildingCollectButton _button;
        private Image _fillImage;
        private Button _uiButton;
        private TextMeshProUGUI _label;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();

            _serviceGO = new GameObject("PendingService");
            _service = _serviceGO.AddComponent<PendingIncomeService>();
            // Inject directly into _buckets; stub out LotRegistry/TickClock.
            InjectBucketsScratchpad();

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
        // Subscription-race: service already has state when button enables
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void EnableAfterServiceState_InDrain_SeedsFromQuery()
        {
            SeedBucket(BuildingId, dailyPayout: 80f, ticksRemaining: 4, ticksPerDay: 10, isReady: false);
            CreateButton();

            // Expected: progress01 = 4/10 = 0.4
            Assert.AreEqual(0.4f, _fillImage.fillAmount, 0.001f);
            Assert.AreEqual("+$80/day", _label.text);
            Assert.IsFalse(_uiButton.interactable);
        }

        [Test]
        public void EnableAfterServiceState_Ready_SeedsPulsingAndInteractable()
        {
            SeedBucket(BuildingId, dailyPayout: 100f, ticksRemaining: 0, ticksPerDay: 10, isReady: true);
            CreateButton();

            Assert.AreEqual(0f, _fillImage.fillAmount, 0.001f);
            Assert.AreEqual("+$100/day", _label.text);
            Assert.IsTrue(_uiButton.interactable);
        }

        [Test]
        public void EnableWithNoBucket_StaysNonInteractable()
        {
            // Button enabled with no service state; must remain non-interactable.
            CreateButton();

            Assert.IsFalse(_uiButton.interactable);
        }

        // ═══════════════════════════════════════════════════════════════
        // Runtime event handling
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void OnClick_DisablesButtonAndRaisesCollectRequest()
        {
            SeedBucket(BuildingId, 50f, 0, 10, isReady: true);
            CreateButton();

            string observedId = null;
            CollectReason observedReason = CollectReason.OwnershipLost;
            GameEvents.OnIncomeCollectRequested += (id, r) => { observedId = id; observedReason = r; };

            InvokePrivate(_button, "HandleClicked");

            Assert.AreEqual(BuildingId, observedId);
            Assert.AreEqual(CollectReason.PlayerTap, observedReason);
            Assert.IsFalse(_uiButton.interactable);
        }

        [Test]
        public void CoinStateChanged_ReadyFlip_EnablesButtonAndUpdatesFill()
        {
            SeedBucket(BuildingId, 50f, 5, 10, isReady: false);
            CreateButton();

            Assert.IsFalse(_uiButton.interactable);

            GameEvents.RaiseCoinStateChanged(BuildingId, 50f, 0f, true);

            Assert.AreEqual(0f, _fillImage.fillAmount, 0.001f);
            Assert.IsTrue(_uiButton.interactable);
        }

        [Test]
        public void CoinStateChanged_DirtyCheckSkipsLabelRebuildForSameRoundedAmount()
        {
            SeedBucket(BuildingId, 12f, 10, 10, isReady: false);
            CreateButton();

            string firstLabel = _label.text;
            _label.text = "DIRTY";

            // Same rounded (Mathf.FloorToInt(12.4f) == 12 == previous).
            GameEvents.RaiseCoinStateChanged(BuildingId, 12.4f, 1f, false);

            Assert.AreEqual("DIRTY", _label.text,
                "Dirty-check should skip label update when the floored integer is unchanged.");
        }

        [Test]
        public void CoinStateChanged_ForDifferentId_IsIgnored()
        {
            SeedBucket(BuildingId, 50f, 10, 10, isReady: false);
            CreateButton();
            float baselineFill = _fillImage.fillAmount;

            GameEvents.RaiseCoinStateChanged("someone_else", 0f, 0f, true);

            Assert.AreEqual(baselineFill, _fillImage.fillAmount);
            Assert.IsFalse(_uiButton.interactable);
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

            _uiButton = _buttonGO.AddComponent<Button>();

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(_buttonGO.transform);
            _label = labelGO.AddComponent<TextMeshProUGUI>();

            _button = _buttonGO.AddComponent<BuildingCollectButton>();
            SetField(_button, "_fillImage", _fillImage);
            SetField(_button, "_button", _uiButton);
            SetField(_button, "_amountLabel", _label);
            SetField(_button, "_buildingId", BuildingId);

            InvokePrivate(_button, "Awake");
            InvokePrivate(_button, "OnEnable");
        }

        private void InjectBucketsScratchpad()
        {
            // Inject minimal stubs so HandleQuery's progress calc has a
            // TicksPerDay denominator. Tests seed buckets directly via
            // reflection rather than going through the service's public API
            // (which depends on a real CityManager).
            _service.Initialize(new TestLotRegistryLocal(), new TestTickClockLocal { TicksPerDay = 10 });
        }

        private void SeedBucket(string id, float dailyPayout, int ticksRemaining, int ticksPerDay, bool isReady)
        {
            var bucketsField = typeof(PendingIncomeService).GetField("_buckets",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var buckets = (Dictionary<string, PendingBucket>)bucketsField.GetValue(_service);
            buckets[id] = new PendingBucket
            {
                DailyPayout = dailyPayout,
                TicksRemaining = ticksRemaining,
                IsReady = isReady,
            };
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
