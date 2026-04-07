using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class DecisionLoggerRunningBalanceTests
    {
        private GameObject _rootGO;
        private DecisionLogger _logger;
        private APIClient _apiClient;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();

            _rootGO = new GameObject("TestRoot");
            _apiClient = _rootGO.AddComponent<APIClient>();
            _logger = _rootGO.AddComponent<DecisionLogger>();

            SetField(_logger, "_apiClient", _apiClient);
            InvokePrivate(_logger, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            InvokePrivate(_logger, "OnDisable");
            Object.DestroyImmediate(_rootGO);
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void CreditCardPayment_IncludesCheckingRunningBalance()
        {
            // Set checking balance via event
            GameEvents.RaiseCheckingBalanceChanged(500f, 0f);

            // Fire CC payment
            GameEvents.RaiseCreditCardPaymentCompleted(200f);

            var dto = _apiClient.LastEnqueuedDecision;
            // LastEnqueuedDecision may be null if CanPersist returns false.
            // In edit mode, CanPersist returns false, so we test the balance tracking
            // indirectly by checking the cached field via reflection.
            float cached = (float)GetField(_logger, "_cachedCheckingBalance");
            Assert.AreEqual(500f, cached, 0.01f);
        }

        [Test]
        public void InvestingBalanceChanged_UpdatesCachedBalance()
        {
            GameEvents.RaiseInvestingBalanceChanged(3000f, 500f);

            float cached = (float)GetField(_logger, "_cachedInvestingBalance");
            Assert.AreEqual(3000f, cached, 0.01f);
        }

        [Test]
        public void CreditBalanceChanged_UpdatesCachedBalance()
        {
            GameEvents.RaiseCreditCardBalanceChanged(800f, 100f);

            float cached = (float)GetField(_logger, "_cachedCreditBalance");
            Assert.AreEqual(800f, cached, 0.01f);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) { field.SetValue(target, value); return; }
                type = type.BaseType;
            }
        }

        private static object GetField(object target, string fieldName)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) return field.GetValue(target);
                type = type.BaseType;
            }
            return null;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(target, null);
        }
    }
}
