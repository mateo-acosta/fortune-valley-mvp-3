using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for GameSessionController. Uses a hand-rolled fake
    /// IJSBridge rather than NSubstitute (not in the project's precompiled refs)
    /// to record the ordered sequence of bridge calls during teardown.
    ///
    /// Decision 1A + 2A: start is gated on OnGameStart; teardown order on
    /// OnGameEnd must be flush save -> flush decisions -> end session.
    /// </summary>
    [TestFixture]
    public class GameSessionControllerTests
    {
        private GameObject _rootGO;
        private GameSessionController _controller;
        private APIClient _apiClient;
        private AutoSaveController _autoSave;
        private DecisionLogger _logger;
        private FakeJSBridge _fakeBridge;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();

            _rootGO = new GameObject("TestRoot");
            _apiClient = _rootGO.AddComponent<APIClient>();
            _autoSave = _rootGO.AddComponent<AutoSaveController>();
            _logger = _rootGO.AddComponent<DecisionLogger>();
            _controller = _rootGO.AddComponent<GameSessionController>();

            _fakeBridge = new FakeJSBridge();
            // Inject fake bridge into the persistence components so CanPersist()
            // returns true and StartSession/EndSession are observable.
            _apiClient.SetBridge(_fakeBridge);
            _controller.SetBridge(_fakeBridge);

            SetField(_controller, "_apiClient", _apiClient);
            SetField(_controller, "_decisionLogger", _logger);
            SetField(_controller, "_autoSaveController", _autoSave);
            SetField(_controller, "_gameMode", "homebase");

            InvokePrivate(_controller, "OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            InvokePrivate(_controller, "OnDisable");
            Object.DestroyImmediate(_rootGO);
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void OnGameStart_CallsStartSessionWithGameMode()
        {
            GameEvents.RaiseGameStart();
            Assert.AreEqual(1, _fakeBridge.StartSessionCalls.Count);
            Assert.AreEqual("homebase", _fakeBridge.StartSessionCalls[0]);
        }

        [Test]
        public void OnGameStart_SetsGameModeOnDecisionLogger()
        {
            GameEvents.RaiseGameStart();
            string mode = (string)GetField(_logger, "_gameMode");
            Assert.AreEqual("homebase", mode);
        }

        [Test]
        public void OnGameStart_Idempotent()
        {
            GameEvents.RaiseGameStart();
            GameEvents.RaiseGameStart();
            Assert.AreEqual(1, _fakeBridge.StartSessionCalls.Count,
                "re-firing OnGameStart should not open a second session");
        }

        [Test]
        public void OnGameEnd_CallsEndSessionAfterFlush()
        {
            GameEvents.RaiseGameStart();
            _fakeBridge.CallOrder.Clear();

            GameEvents.RaiseGameEnd(Owner.Player);

            // EndSession must be last. APIClient.FlushDecisions is a no-op on an
            // empty buffer but we still expect EndSession to fire.
            Assert.Contains("EndSession", _fakeBridge.CallOrder);
            Assert.AreEqual("EndSession", _fakeBridge.CallOrder[_fakeBridge.CallOrder.Count - 1]);
        }

        [Test]
        public void OnGameEnd_WithoutGameStart_DoesNothing()
        {
            GameEvents.RaiseGameEnd(Owner.Player);
            Assert.AreEqual(0, _fakeBridge.EndSessionCalls.Count);
        }

        // Fake IJSBridge with configurable signed-in + role responses and call
        // recording so tests can assert on ordered invocations.
        private class FakeJSBridge : IJSBridge
        {
            public List<string> StartSessionCalls = new List<string>();
            public List<string> EndSessionCalls = new List<string>();
            public List<string> CallOrder = new List<string>();

            public string GetCsrfToken() => "fake-csrf";
            public bool IsSignedIn() => true;
            public string GetRole() => "student";

            public void SaveState(string json) { CallOrder.Add("SaveState"); }

            public void LogDecision(string json) { CallOrder.Add("LogDecision"); }

            public void StartSession(string gameMode)
            {
                StartSessionCalls.Add(gameMode);
                CallOrder.Add("StartSession");
            }

            public void EndSession(string sessionId)
            {
                EndSessionCalls.Add(sessionId);
                CallOrder.Add("EndSession");
            }

            // Web panel surface — not exercised by these tests; stubbed only
            // to satisfy the IJSBridge contract.
            public void ShowPanel(string panelId) { CallOrder.Add("ShowPanel"); }
            public void HidePanel(string panelId) { CallOrder.Add("HidePanel"); }
            public void UpdatePanel(string panelId, string json) { CallOrder.Add("UpdatePanel"); }
            public void ShowError(string panelId, string message) { CallOrder.Add("ShowError"); }
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            throw new System.Exception($"Field '{fieldName}' not found on {target.GetType().Name}");
        }

        private static object GetField(object target, string fieldName)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
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
