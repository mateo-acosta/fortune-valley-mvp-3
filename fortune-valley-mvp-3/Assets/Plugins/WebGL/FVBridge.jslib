mergeInto(LibraryManager.library, {
  FVBridge_GetCsrfToken: function() {
    var token = '';
    try {
      if (window.FV && window.FV.csrf) token = window.FV.csrf.token;
    } catch(e) {}
    var bufferSize = lengthBytesUTF8(token) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(token, buffer, bufferSize);
    return buffer;
  },

  FVBridge_IsSignedIn: function() {
    try {
      return (window.FV && window.FV.auth && window.FV.auth.signedIn) ? 1 : 0;
    } catch(e) { return 0; }
  },

  FVBridge_GetRole: function() {
    var role = 'guest';
    try {
      if (window.FV && window.FV.role) role = window.FV.role;
    } catch(e) {}
    var bufferSize = lengthBytesUTF8(role) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(role, buffer, bufferSize);
    return buffer;
  },

  FVBridge_SaveState: function(jsonPtr) {
    var json = UTF8ToString(jsonPtr);
    try {
      if (window.FV && typeof window.FV.onUnitySaveUpdate === 'function') {
        window.FV.onUnitySaveUpdate(json);
      }
    } catch(e) {}
  },

  FVBridge_LogDecision: function(jsonPtr) {
    var json = UTF8ToString(jsonPtr);
    try {
      if (window.FV && typeof window.FV.onUnityDecision === 'function') {
        window.FV.onUnityDecision(json);
      }
    } catch(e) {}
  },

  // Triggers window.FV.startSession(gameMode) on the Rails side.
  // The Rails helper is responsible for POST /api/game/session/start
  // and for SendMessage'ing the resolved session_id back to the
  // DecisionLogger GameObject in Unity. Fire-and-forget here.
  FVBridge_StartSession: function(gameModePtr) {
    var gameMode = UTF8ToString(gameModePtr);
    try {
      if (window.FV && typeof window.FV.startSession === 'function') {
        Promise.resolve(window.FV.startSession(gameMode)).catch(function() {});
      }
    } catch(e) {}
  },

  // Fire-and-forget. If sessionId is empty string, Rails falls back to
  // its cached sessionId internally (see unity_bridge_controller.js).
  FVBridge_EndSession: function(sessionIdPtr) {
    var sessionId = UTF8ToString(sessionIdPtr);
    try {
      if (window.FV && typeof window.FV.endSession === 'function') {
        Promise.resolve(window.FV.endSession(sessionId)).catch(function() {});
      }
    } catch(e) {}
  },

  // ===============================================================
  // WEB PANEL OVERLAY SURFACE
  // HTML iframes layered above the Unity canvas. Forwarded calls land
  // on window.FV.* host functions defined in the WebGL template's
  // index.html. Read-only display until per-region pointer-events
  // are flipped on for intent buttons.
  // ===============================================================

  FVBridge_ShowPanel: function(panelIdPtr) {
    var panelId = UTF8ToString(panelIdPtr);
    try {
      if (window.FV && typeof window.FV.showPanel === 'function') {
        window.FV.showPanel(panelId);
      }
    } catch(e) {}
  },

  FVBridge_HidePanel: function(panelIdPtr) {
    var panelId = UTF8ToString(panelIdPtr);
    try {
      if (window.FV && typeof window.FV.hidePanel === 'function') {
        window.FV.hidePanel(panelId);
      }
    } catch(e) {}
  },

  FVBridge_UpdatePanel: function(panelIdPtr, jsonPtr) {
    var panelId = UTF8ToString(panelIdPtr);
    var json = UTF8ToString(jsonPtr);
    try {
      if (window.FV && typeof window.FV.updatePanel === 'function') {
        window.FV.updatePanel(panelId, json);
      }
    } catch(e) {}
  },

  FVBridge_ShowError: function(panelIdPtr, messagePtr) {
    var panelId = UTF8ToString(panelIdPtr);
    var message = UTF8ToString(messagePtr);
    try {
      if (window.FV && typeof window.FV.showError === 'function') {
        window.FV.showError(panelId, message);
      }
    } catch(e) {}
  }
});
