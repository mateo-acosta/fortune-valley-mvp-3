using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Pins the POC insurance disable. If a future change re-subscribes
    /// the insurance source systems while the flag is still off, these
    /// tests fail. Flip FeatureFlags.InsuranceEnabled to true and these
    /// tests are expected to be removed/inverted in the same change.
    /// </summary>
    [TestFixture]
    public class InsuranceDisabledTests
    {
        private GameObject _systemGo;

        [TearDown]
        public void TearDown()
        {
            if (_systemGo != null) UnityEngine.Object.DestroyImmediate(_systemGo);
            GameEvents.ClearAllSubscriptions();
        }

        // ===============================================================
        // SANITY
        // ===============================================================

        [Test]
        public void FeatureFlag_InsuranceEnabled_IsFalse()
        {
            // The POC ships with insurance disabled. Flipping this is a
            // deliberate scope decision -- update tests when re-enabling.
            Assert.IsFalse(
                FeatureFlags.InsuranceEnabled,
                "Insurance is supposed to be disabled for the POC. " +
                "If you're flipping this on, also remove or invert the " +
                "InsuranceDisabledTests in the same change.");
        }

        // ===============================================================
        // INSURANCE SYSTEM SOURCE GUARD
        // ===============================================================

        [Test]
        public void InsuranceSystem_FlagOff_OnGameStart_LeavesPortfolioNull()
        {
            // OnEnable's flag guard means HandleGameStart is never wired.
            // Result: raising OnGameStart should NOT initialize the portfolio.
            _systemGo = new GameObject("InsuranceSystemUnderTest");
            var system = _systemGo.AddComponent<InsuranceSystem>();

            GameEvents.RaiseGameStart();

            Assert.IsNull(
                system.Portfolio,
                "Insurance portfolio must remain null when the flag is off " +
                "-- proves OnGameStart subscription was skipped.");
        }

        [Test]
        public void InsuranceSystem_FlagOff_OnAccidentOccurred_DoesNotRaiseAccidentResolved()
        {
            // OnEnable's flag guard means HandleAccidentOccurred is never wired.
            // Result: raising OnAccidentOccurred should NOT cause OnAccidentResolved.
            _systemGo = new GameObject("InsuranceSystemUnderTest");
            _systemGo.AddComponent<InsuranceSystem>();

            int resolvedCount = 0;
            Action<string, string, float, bool, float> handler =
                (lot, name, damage, covered, cost) => resolvedCount++;
            GameEvents.OnAccidentResolved += handler;

            try
            {
                GameEvents.RaiseAccidentOccurred(
                    new AccidentRollResult("lot_test", "fire", "Fire", 1000f));
            }
            finally
            {
                GameEvents.OnAccidentResolved -= handler;
            }

            Assert.AreEqual(
                0,
                resolvedCount,
                "Accident must not resolve when insurance is disabled.");
        }

        // ===============================================================
        // ACCIDENT SYSTEM SOURCE GUARD
        // ===============================================================

        [Test]
        public void AccidentSystem_FlagOff_OnGameStart_LeavesRollerNull()
        {
            // OnEnable's flag guard means HandleGameStart is never wired.
            // Inspect the private _roller field via reflection: it should
            // remain null, proving the system never initialized.
            _systemGo = new GameObject("AccidentSystemUnderTest");
            var system = _systemGo.AddComponent<AccidentSystem>();

            GameEvents.RaiseGameStart();

            FieldInfo rollerField = typeof(AccidentSystem).GetField(
                "_roller", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(rollerField, "Reflection lookup for _roller failed -- field renamed?");

            object roller = rollerField.GetValue(system);
            Assert.IsNull(
                roller,
                "Accident roller must remain null when the flag is off " +
                "-- proves OnGameStart subscription was skipped.");
        }
    }
}
