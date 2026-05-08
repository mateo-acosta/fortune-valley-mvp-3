using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// Base class for WebPanel bridges. Owns the show/hide visibility flag,
    /// the dirty flag, and the LateUpdate coalescing that pushes one full
    /// state snapshot per frame regardless of how many GameEvents fired.
    ///
    /// Subclasses provide the panel id, the scene GameObject name (for
    /// the C#/JS contract per Issue 4A), and the per-panel Subscribe /
    /// Unsubscribe / BuildPayloadJson hooks.
    ///
    /// The IJSBridge is constructed lazily; tests can inject a mock via
    /// SetBridge before Show is called.
    /// </summary>
    public abstract class WebPanelBridgeBase : MonoBehaviour
    {
        private IJSBridge _bridge;
        private bool _isVisible;
        private bool _isDirty;
        private bool _warnedNullPayload;

        /// <summary>Panel id passed to JSBridge.ShowPanel/UpdatePanel/etc. Lower-case, e.g. "investing".</summary>
        public abstract string PanelId { get; }

        /// <summary>Expected scene GameObject name. Subclasses expose this as a const so JS SendMessage targets stay aligned (Issue 4A).</summary>
        public abstract string ExpectedObjectName { get; }

        protected IJSBridge Bridge
        {
            get
            {
                if (_bridge == null) _bridge = new StaticJSBridge();
                return _bridge;
            }
        }

        /// <summary>Test hook: substitute an IJSBridge mock before Show is called.</summary>
        public void SetBridge(IJSBridge bridge) { _bridge = bridge; }

        /// <summary>Visible to tests so they can assert state without touching MonoBehaviour internals.</summary>
        public bool IsVisible => _isVisible;
        public bool IsDirty => _isDirty;

        protected virtual void OnEnable()
        {
            if (gameObject.name != ExpectedObjectName)
            {
                Debug.LogWarning($"[WebPanelBridge] GameObject name '{gameObject.name}' does not match expected '{ExpectedObjectName}'. JS SendMessage targets will not resolve.");
            }
        }

        protected virtual void OnDisable()
        {
            // Defensive: if disabled while still subscribed, drop subscriptions
            // so we never deliver an event after teardown.
            if (_isVisible)
            {
                Unsubscribe();
                _isVisible = false;
                _isDirty = false;
            }
        }

        /// <summary>
        /// Show the panel. Subscribes to GameEvents, calls JS show, and
        /// pushes a fresh full snapshot before any event fires.
        /// Idempotent: a second Show while visible is a no-op.
        /// </summary>
        public void Show()
        {
            if (_isVisible) return;
            _isVisible = true;

            Subscribe();
            Bridge.ShowPanel(PanelId);

            // Initial paint: push current state directly. Reads off live
            // system properties, so it works whether the game just started
            // or a save was loaded.
            PushNow();
        }

        /// <summary>
        /// Hide the panel. Unsubscribes from GameEvents and calls JS hide.
        /// Idempotent: a second Hide while not visible is a no-op.
        /// </summary>
        public void Hide()
        {
            if (!_isVisible) return;
            _isVisible = false;
            _isDirty = false;

            Unsubscribe();
            Bridge.HidePanel(PanelId);
        }

        /// <summary>Subclasses call this from each event handler; coalesces multiple events in a frame into one push.</summary>
        protected void MarkDirty()
        {
            if (_isVisible) _isDirty = true;
        }

        protected virtual void LateUpdate()
        {
            if (!_isVisible || !_isDirty) return;
            _isDirty = false;
            PushNow();
        }

        private void PushNow()
        {
            string json = BuildPayloadJson();
            if (json == null)
            {
                // Silent drops leave the iframe stuck on its mockState fallback.
                // Emit a single warning per bridge instance so the cause is visible.
                if (!_warnedNullPayload)
                {
                    Debug.LogWarning($"[{PanelId}WebBridge] BuildPayloadJson returned null. Iframe will keep showing mockState. Verify SerializeField wiring on '{ExpectedObjectName}'.");
                    _warnedNullPayload = true;
                }
                return;
            }
            Bridge.UpdatePanel(PanelId, json);
        }

        /// <summary>Subclass subscribes to its specific GameEvents. Call MarkDirty from each handler.</summary>
        protected abstract void Subscribe();

        /// <summary>Subclass unsubscribes from the same GameEvents subscribed in Subscribe.</summary>
        protected abstract void Unsubscribe();

        /// <summary>
        /// Subclass returns a JSON payload for the current panel state, or
        /// null to skip this push (e.g., if dependencies are not yet initialized).
        /// </summary>
        protected abstract string BuildPayloadJson();
    }
}
