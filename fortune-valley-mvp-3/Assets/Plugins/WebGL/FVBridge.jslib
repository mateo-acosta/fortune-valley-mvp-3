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
  }
});
