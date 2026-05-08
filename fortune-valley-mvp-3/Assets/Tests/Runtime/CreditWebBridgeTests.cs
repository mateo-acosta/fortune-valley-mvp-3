using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FortuneValley.Core;
using FortuneValley.Managers.WebPanels;

namespace FortuneValley.Tests
{
    /// <summary>
    /// PlayMode tests for CreditWebBridge. Mirrors the InvestingWebBridge
    /// test surface: lifecycle invariants, ObjectName warning, intent
    /// validation paths. Full PopulateDTO integration with real
    /// LoanSystem/CreditCardSystem state is out of scope for v1.
    /// </summary>
    [TestFixture]
    public class CreditWebBridgeTests
    {
        private GameObject _go;
        private CreditWebBridge _bridge;
        private FakeJSBridge _fakeBridge;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _go = new GameObject(CreditWebBridge.ObjectName);
            _bridge = _go.AddComponent<CreditWebBridge>();
            _fakeBridge = new FakeJSBridge();
            _bridge.SetBridge(_fakeBridge);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            GameEvents.ClearAllSubscriptions();
        }

        // ───────────────────────── Show / Hide basics ─────────────────────────

        [Test]
        public void Show_FirstCall_CallsJSBridgeShowPanel()
        {
            _bridge.Show();
            Assert.AreEqual(1, _fakeBridge.ShowPanelCalls.Count);
            Assert.AreEqual("credit", _fakeBridge.ShowPanelCalls[0]);
            Assert.IsTrue(_bridge.IsVisible);
        }

        [Test]
        public void Show_CalledTwice_IsIdempotent()
        {
            _bridge.Show();
            _bridge.Show();
            Assert.AreEqual(1, _fakeBridge.ShowPanelCalls.Count);
        }

        [Test]
        public void Hide_AfterShow_CallsJSBridgeHidePanel()
        {
            _bridge.Show();
            _bridge.Hide();
            Assert.AreEqual(1, _fakeBridge.HidePanelCalls.Count);
            Assert.AreEqual("credit", _fakeBridge.HidePanelCalls[0]);
            Assert.IsFalse(_bridge.IsVisible);
        }

        [Test]
        public void Hide_WithoutShow_DoesNothing()
        {
            _bridge.Hide();
            Assert.AreEqual(0, _fakeBridge.HidePanelCalls.Count);
        }

        // ───────────────────────── Subscription guards ─────────────────────────

        [Test]
        public void Show_WithNoDependenciesWired_DoesNotCallUpdatePanel()
        {
            _bridge.Show();
            Assert.AreEqual(0, _fakeBridge.UpdatePanelCalls.Count);
        }

        [Test]
        public void OnTick_WhileVisible_MarksDirty()
        {
            _bridge.Show();
            Assert.IsFalse(_bridge.IsDirty);
            GameEvents.RaiseTick(1);
            Assert.IsTrue(_bridge.IsDirty);
        }

        [Test]
        public void Hide_AfterShow_UnsubscribesFromOnTick()
        {
            _bridge.Show();
            _bridge.Hide();
            GameEvents.RaiseTick(1);
            Assert.IsFalse(_bridge.IsDirty);
        }

        [UnityTest]
        public IEnumerator MultipleEventsInSameFrame_ClearDirtyAfterLateUpdate()
        {
            _bridge.Show();

            GameEvents.RaiseTick(1);
            GameEvents.RaiseCreditCardBalanceChanged(100f, 10f);
            GameEvents.RaiseCreditScoreChanged(680);
            Assert.IsTrue(_bridge.IsDirty);

            yield return null;
            Assert.IsFalse(_bridge.IsDirty, "LateUpdate should have cleared dirty flag");
        }

        // ───────────────────────── Apply for loan validation ─────────────────────────

        [Test]
        public void RequestApplyForLoan_WithMalformedJson_ShowsError()
        {
            _bridge.RequestApplyForLoan("not-json{");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            StringAssert.Contains("Malformed", _fakeBridge.ShowErrorCalls[0].Message);
        }

        [Test]
        public void RequestApplyForLoan_WithEmptyJson_ShowsError()
        {
            _bridge.RequestApplyForLoan("");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            StringAssert.Contains("Empty", _fakeBridge.ShowErrorCalls[0].Message);
        }

        [Test]
        public void RequestApplyForLoan_WithMissingLoanConfigId_ShowsError()
        {
            _bridge.RequestApplyForLoan("{\"lotId\":\"lot_block02\",\"price\":100000}");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            StringAssert.Contains("loan", _fakeBridge.ShowErrorCalls[0].Message.ToLower());
        }

        [Test]
        public void RequestApplyForLoan_WithMissingLotId_ShowsError()
        {
            _bridge.RequestApplyForLoan("{\"loanConfigId\":\"loan-30y\",\"price\":100000}");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            StringAssert.Contains("lot", _fakeBridge.ShowErrorCalls[0].Message.ToLower());
        }

        [Test]
        public void RequestApplyForLoan_WithZeroPrice_ShowsError()
        {
            _bridge.RequestApplyForLoan("{\"loanConfigId\":\"loan-30y\",\"lotId\":\"lot_block02\",\"price\":0}");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            StringAssert.Contains("price", _fakeBridge.ShowErrorCalls[0].Message.ToLower());
        }

        [Test]
        public void RequestApplyForLoan_WithNoLoanSystemWired_ShowsError()
        {
            _bridge.RequestApplyForLoan("{\"loanConfigId\":\"loan-30y\",\"lotId\":\"lot_block02\",\"price\":100000}");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            StringAssert.Contains("not ready", _fakeBridge.ShowErrorCalls[0].Message.ToLower());
        }

        // ───────────────────────── Object name validation ─────────────────────────

        [Test]
        public void OnEnable_GameObjectNameMatchesObjectName_NoWarning()
        {
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnEnable_GameObjectNameMismatch_LogsWarning()
        {
            var wrongGo = new GameObject("WrongName");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("does not match expected"));
            wrongGo.AddComponent<CreditWebBridge>();
            Object.Destroy(wrongGo);
        }

        // ───────────────────────── Test fakes ─────────────────────────

        private class FakeJSBridge : IJSBridge
        {
            public List<string> ShowPanelCalls = new List<string>();
            public List<string> HidePanelCalls = new List<string>();
            public List<UpdatePanelCall> UpdatePanelCalls = new List<UpdatePanelCall>();
            public List<ShowErrorCall> ShowErrorCalls = new List<ShowErrorCall>();

            public string GetCsrfToken() => "";
            public bool IsSignedIn() => false;
            public string GetRole() => "test";
            public void SaveState(string json) { }
            public void LogDecision(string json) { }
            public void StartSession(string gameMode) { }
            public void EndSession(string sessionId) { }

            public void ShowPanel(string panelId) { ShowPanelCalls.Add(panelId); }
            public void HidePanel(string panelId) { HidePanelCalls.Add(panelId); }
            public void UpdatePanel(string panelId, string json)
            {
                UpdatePanelCalls.Add(new UpdatePanelCall { PanelId = panelId, Json = json });
            }
            public void ShowError(string panelId, string message)
            {
                ShowErrorCalls.Add(new ShowErrorCall { PanelId = panelId, Message = message });
            }
            public void ReportEvent(string eventName, string propertiesJson) { }
        }

        private class UpdatePanelCall
        {
            public string PanelId;
            public string Json;
        }

        private class ShowErrorCall
        {
            public string PanelId;
            public string Message;
        }
    }
}
