using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using FortuneValley.Core;
using FortuneValley.UI.HUD;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Coverage for the persistent "+$X/day" HUD readout. The HUD is a pure
    /// renderer subscribed to OnTotalDailyIncomeChanged; tests drive the event
    /// directly and assert the TextMeshProUGUI text is updated with the
    /// expected format.
    /// </summary>
    [TestFixture]
    public class DailyIncomeHudTests
    {
        private GameObject _go;
        private DailyIncomeHud _hud;
        private TextMeshProUGUI _text;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _go = new GameObject("HudHost");
            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(_go.transform);
            _text = textGO.AddComponent<TextMeshProUGUI>();

            _hud = _go.AddComponent<DailyIncomeHud>();
            SetField(_hud, "_dailyIncomeText", _text);

            InvokePrivate(_hud, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            InvokePrivate(_hud, "OnDisable");
            Object.DestroyImmediate(_go);
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void OnTotalDailyIncomeChanged_FormatsTextWithThousandsAndPerDay()
        {
            GameEvents.RaiseTotalDailyIncomeChanged(1234f);
            Assert.AreEqual("+$1,234/day", _text.text);
        }

        [Test]
        public void OnTotalDailyIncomeChanged_ZeroAmount_FormatsAsZero()
        {
            GameEvents.RaiseTotalDailyIncomeChanged(0f);
            Assert.AreEqual("+$0/day", _text.text);
        }

        [Test]
        public void OnTotalDailyIncomeChanged_RepeatSameRoundedValue_SkipsUpdate()
        {
            GameEvents.RaiseTotalDailyIncomeChanged(100f);
            _text.text = "DIRTY";

            // Same rounded total -> no rewrite.
            GameEvents.RaiseTotalDailyIncomeChanged(100.4f);

            Assert.AreEqual("DIRTY", _text.text,
                "Same-rounded-int total must skip the redundant TMP rebuild.");
        }

        [Test]
        public void OnTotalDailyIncomeChanged_DifferentRoundedValue_Updates()
        {
            GameEvents.RaiseTotalDailyIncomeChanged(100f);
            GameEvents.RaiseTotalDailyIncomeChanged(101f);

            Assert.AreEqual("+$101/day", _text.text);
        }

        [Test]
        public void OnTotalDailyIncomeChanged_NullTextRef_DoesNotCrash()
        {
            SetField(_hud, "_dailyIncomeText", null);
            Assert.DoesNotThrow(() => GameEvents.RaiseTotalDailyIncomeChanged(50f));
        }

        [Test]
        public void OnDisable_UnsubscribesFromEvent()
        {
            InvokePrivate(_hud, "OnDisable");
            _text.text = "BEFORE";

            GameEvents.RaiseTotalDailyIncomeChanged(999f);

            Assert.AreEqual("BEFORE", _text.text,
                "After OnDisable, the HUD must not respond to events anymore.");
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
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null) { field.SetValue(target, value); return; }
                type = type.BaseType;
            }
        }
    }
}
