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
    /// PlayMode tests for InvestingWebBridge. Covers the lifecycle
    /// invariants: Show/Hide idempotency, subscribe/unsubscribe, the
    /// dirty-flag coalescing in LateUpdate, and the ObjectName name
    /// mismatch warning (Issue 4A).
    ///
    /// PopulateDTO is exercised indirectly: when the bridge has no
    /// system references wired, BuildPayloadJson returns null and no
    /// UpdatePanel call is made. Full PopulateDTO integration with
    /// real InvestmentSystem state is out of scope for v1.
    /// </summary>
    [TestFixture]
    public class InvestingWebBridgeTests
    {
        private GameObject _go;
        private InvestingWebBridge _bridge;
        private FakeJSBridge _fakeBridge;

        [SetUp]
        public void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            _go = new GameObject(InvestingWebBridge.ObjectName);
            _bridge = _go.AddComponent<InvestingWebBridge>();
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
            Assert.AreEqual("investing", _fakeBridge.ShowPanelCalls[0]);
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
            Assert.AreEqual("investing", _fakeBridge.HidePanelCalls[0]);
            Assert.IsFalse(_bridge.IsVisible);
        }

        [Test]
        public void Hide_WithoutShow_DoesNothing()
        {
            _bridge.Hide();
            Assert.AreEqual(0, _fakeBridge.HidePanelCalls.Count);
        }

        [Test]
        public void Hide_CalledTwice_IsIdempotent()
        {
            _bridge.Show();
            _bridge.Hide();
            _bridge.Hide();
            Assert.AreEqual(1, _fakeBridge.HidePanelCalls.Count);
        }

        // ───────────────────────── Push behavior with no deps wired ─────────────────────────

        [Test]
        public void Show_WithNoDependenciesWired_DoesNotCallUpdatePanel()
        {
            // Logic.PopulateDTO returns false when InvestmentSystem is null,
            // so BuildPayloadJson returns null and no UpdatePanel fires.
            _bridge.Show();
            Assert.AreEqual(0, _fakeBridge.UpdatePanelCalls.Count);
        }

        // ───────────────────────── Subscription leak guards ─────────────────────────

        [Test]
        public void OnTick_WhileHidden_DoesNotMarkDirty()
        {
            // Bridge starts not visible, so OnTick subscription is also
            // absent. Firing OnTick should not crash and should not push.
            GameEvents.RaiseTick(1);
            Assert.IsFalse(_bridge.IsDirty);
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
            // After Hide, dirty should not get set because handler unsubscribed.
            Assert.IsFalse(_bridge.IsDirty);
        }

        // ───────────────────────── Coalescing (frame-aware) ─────────────────────────

        [UnityTest]
        public IEnumerator MultipleEventsInSameFrame_PushOnceAfterLateUpdate()
        {
            _bridge.Show();
            int initialUpdates = _fakeBridge.UpdatePanelCalls.Count;

            // Multiple events in the same frame
            GameEvents.RaiseTick(1);
            GameEvents.RaiseCheckingBalanceChanged(100f, 10f);
            GameEvents.RaiseInvestingBalanceChanged(200f, 20f);

            Assert.IsTrue(_bridge.IsDirty);

            // Advance one frame for LateUpdate. With no deps wired,
            // BuildPayloadJson returns null so UpdatePanel is NOT called.
            // What we verify is that dirty is cleared (coalescing happened).
            yield return null;

            Assert.IsFalse(_bridge.IsDirty, "LateUpdate should have cleared dirty flag");
            // No UpdatePanel call is expected since no deps are wired;
            // we don't assert on that count here. Coalescing is verified by
            // the dirty flag being cleared exactly once after multiple sets.
        }

        // ───────────────────────── Buy/Sell intent validation (Phase 5) ─────────────────────────

        [Test]
        public void RequestBuyShares_WithMalformedJson_ShowsError()
        {
            _bridge.RequestBuyShares("not-json{");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            Assert.AreEqual("investing", _fakeBridge.ShowErrorCalls[0].PanelId);
            StringAssert.Contains("Malformed", _fakeBridge.ShowErrorCalls[0].Message);
        }

        [Test]
        public void RequestBuyShares_WithEmptyJson_ShowsError()
        {
            _bridge.RequestBuyShares("");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            StringAssert.Contains("Empty", _fakeBridge.ShowErrorCalls[0].Message);
        }

        [Test]
        public void RequestBuyShares_WithMissingSymbol_ShowsError()
        {
            _bridge.RequestBuyShares("{\"qty\":1}");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            StringAssert.Contains("symbol", _fakeBridge.ShowErrorCalls[0].Message.ToLower());
        }

        [Test]
        public void RequestBuyShares_WithZeroQty_ShowsError()
        {
            _bridge.RequestBuyShares("{\"symbol\":\"Stock_Tech_Low\",\"qty\":0}");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            StringAssert.Contains("Quantity", _fakeBridge.ShowErrorCalls[0].Message);
        }

        [Test]
        public void RequestBuyShares_WithNegativeQty_ShowsError()
        {
            _bridge.RequestBuyShares("{\"symbol\":\"Stock_Tech_Low\",\"qty\":-3}");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
        }

        [Test]
        public void RequestBuyShares_WithNoInvestmentSystemWired_ShowsError()
        {
            // SetUp does not wire _investmentSystem, so any well-formed
            // intent should be rejected with a "Game not ready" error.
            _bridge.RequestBuyShares("{\"symbol\":\"Stock_Tech_Low\",\"qty\":1}");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            StringAssert.Contains("not ready", _fakeBridge.ShowErrorCalls[0].Message.ToLower());
        }

        [Test]
        public void RequestSellShares_WithMalformedJson_ShowsError()
        {
            _bridge.RequestSellShares("garbage");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
            StringAssert.Contains("Malformed", _fakeBridge.ShowErrorCalls[0].Message);
        }

        [Test]
        public void RequestSellShares_WithZeroQty_ShowsError()
        {
            _bridge.RequestSellShares("{\"symbol\":\"Stock_Tech_Low\",\"qty\":0}");
            Assert.AreEqual(1, _fakeBridge.ShowErrorCalls.Count);
        }

        // ───────────────────────── Object name validation (Issue 4A) ─────────────────────────

        [Test]
        public void OnEnable_GameObjectNameMatchesObjectName_NoWarning()
        {
            // SetUp already added the bridge to a GameObject named ObjectName.
            // No warning expected.
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void OnEnable_GameObjectNameMismatch_LogsWarning()
        {
            // Create a fresh bridge on a wrong-named GameObject.
            var wrongGo = new GameObject("WrongName");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("does not match expected"));
            wrongGo.AddComponent<InvestingWebBridge>();
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
