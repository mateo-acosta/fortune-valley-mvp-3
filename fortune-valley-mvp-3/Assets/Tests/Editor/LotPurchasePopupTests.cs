using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FortuneValley.Core;
using FortuneValley.UI.Popups;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for LotPurchasePopup.ConfigureForLot.
    /// </summary>
    [TestFixture]
    public class LotPurchasePopupTests
    {
        private GameObject _go;
        private LotPurchasePopup _popup;
        private CityLotDefinition _testLot;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestPopup");
            _popup = _go.AddComponent<LotPurchasePopup>();

            _testLot = ScriptableObject.CreateInstance<CityLotDefinition>();
            SetPrivateField(_testLot, "_lotId", "test_lot");
            SetPrivateField(_testLot, "_displayName", "Test Lot");
            SetPrivateField(_testLot, "_baseCost", 5000f);
            SetPrivateField(_testLot, "_incomeBonus", 10f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_testLot);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private object GetPrivateField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(obj);
        }

        /// <summary>
        /// EnsureInitialized logs errors for null SerializeField deps in EditMode tests.
        /// These helpers tell NUnit those errors are expected so they don't fail the test.
        /// </summary>
        private void ExpectInitErrors()
        {
            LogAssert.Expect(LogType.Error, "[LotPurchasePopup] _currencyManager not wired in Inspector.");
        }

        [Test]
        public void ConfigureForLot_StoresLotDefinition()
        {
            ExpectInitErrors();
            _popup.ConfigureForLot(_testLot, 5);

            var storedLot = GetPrivateField(_popup, "_currentLot") as CityLotDefinition;
            Assert.AreEqual(_testLot, storedLot);
        }

        [Test]
        public void ConfigureForLot_StoresCurrentTick()
        {
            ExpectInitErrors();
            _popup.ConfigureForLot(_testLot, 42);

            var storedTick = (int)GetPrivateField(_popup, "_currentTick");
            Assert.AreEqual(42, storedTick);
        }

        [Test]
        public void ConfigureForLot_WithNullLot_DoesNotThrow()
        {
            ExpectInitErrors();
            Assert.DoesNotThrow(() => _popup.ConfigureForLot(null, 0));
        }
    }
}
