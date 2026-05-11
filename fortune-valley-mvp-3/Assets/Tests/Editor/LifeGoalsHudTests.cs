using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.UI.HUD;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LifeGoalsHudTests
    {
        private GameObject _hudGO;
        private GameObject _sliderGO;
        private GameObject _ageTextGO;
        private LifeGoalsHud _hud;
        private Slider _slider;
        private TextMeshProUGUI _ageText;

        [SetUp]
        public void SetUp()
        {
            _sliderGO = new GameObject("Slider_Orange");
            _slider = _sliderGO.AddComponent<Slider>();

            _ageTextGO = new GameObject("AgeText");
            _ageText = _ageTextGO.AddComponent<TextMeshProUGUI>();

            _hudGO = new GameObject("UserInfo");
            _sliderGO.transform.SetParent(_hudGO.transform);
            _ageTextGO.transform.SetParent(_hudGO.transform);

            _hud = _hudGO.AddComponent<LifeGoalsHud>();
            SetPrivate(_hud, "_progressSlider", _slider);
            SetPrivate(_hud, "_ageText", _ageText);

            // EditMode tests do not auto-fire MonoBehaviour lifecycle methods,
            // so invoke OnEnable then Start explicitly. Order matches Play Mode.
            InvokePrivate(_hud, "OnEnable");
            InvokePrivate(_hud, "Start");
        }

        [TearDown]
        public void TearDown()
        {
            if (_hud != null) InvokePrivate(_hud, "OnDisable");
            if (_hudGO != null) Object.DestroyImmediate(_hudGO);
            GameEvents.ClearAllSubscriptions();
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field, $"Field {fieldName} not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static void InvokePrivate(MonoBehaviour mb, string methodName)
        {
            var method = mb.GetType().GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method, $"Method {methodName} not found on {mb.GetType().Name}");
            method.Invoke(mb, null);
        }

        [Test]
        public void Start_SliderHidden_PreSelectionState()
        {
            Assert.IsFalse(_sliderGO.activeSelf,
                "Slider should be hidden until first OnGoalProgressChanged fires.");
        }

        [Test]
        public void GoalProgressChanged_FirstFire_ShowsAndMapsSliderAbsolute()
        {
            GameEvents.RaiseGoalProgressChanged(50_000f, 0f, 100_000f);

            Assert.IsTrue(_sliderGO.activeSelf, "Slider must become visible.");
            Assert.AreEqual(0f, _slider.minValue);
            Assert.AreEqual(100_000f, _slider.maxValue);
            Assert.AreEqual(50_000f, _slider.value);
        }

        [Test]
        public void GoalProgressChanged_AfterRealize_UpdatesMaxAndValueWithoutStaleClamp()
        {
            // Cross into the next tier in one event. Value $300k must NOT clamp
            // to the old $100k max -- the new max is set first by the same handler.
            GameEvents.RaiseGoalProgressChanged(50_000f, 0f, 100_000f);
            GameEvents.RaiseGoalProgressChanged(300_000f, 100_000f, 500_000f);

            Assert.AreEqual(500_000f, _slider.maxValue);
            Assert.AreEqual(300_000f, _slider.value);
        }

        [Test]
        public void AllGoalsRealized_RevealsAndPinsSliderAtFinalThreshold()
        {
            // Save-load case: snapshot drives this directly without a prior
            // OnGoalProgressChanged. Slider must reveal AND pin in one shot.
            GameEvents.RaiseAllGoalsRealized(2_000_000f);

            Assert.IsTrue(_sliderGO.activeSelf);
            Assert.AreEqual(2_000_000f, _slider.maxValue);
            Assert.AreEqual(2_000_000f, _slider.value);
        }

        [Test]
        public void AfterAllGoalsRealized_LateProgressEventsAreIgnored()
        {
            GameEvents.RaiseAllGoalsRealized(2_000_000f);
            float pinnedMax = _slider.maxValue;
            float pinnedValue = _slider.value;

            // Defensive: tracker should not fire OnGoalProgressChanged after
            // OnAllGoalsRealized, but if it did the HUD must ignore it.
            GameEvents.RaiseGoalProgressChanged(1_000_000f, 500_000f, 2_000_000f);

            Assert.AreEqual(pinnedMax, _slider.maxValue);
            Assert.AreEqual(pinnedValue, _slider.value);
        }

        [Test]
        public void YearEnd_UpdatesAgeText()
        {
            GameEvents.RaiseYearEnd(26);

            Assert.AreEqual("Age: 26", _ageText.text);
        }
    }
}
