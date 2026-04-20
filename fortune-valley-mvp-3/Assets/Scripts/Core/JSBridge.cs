using System.Runtime.InteropServices;
using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// C# wrapper around the FVBridge.jslib plugin.
    /// Provides typed access to browser-side auth and persistence functions.
    /// In the Unity Editor, returns safe fallback values.
    /// </summary>
    public static class JSBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string FVBridge_GetCsrfToken();

        [DllImport("__Internal")]
        private static extern int FVBridge_IsSignedIn();

        [DllImport("__Internal")]
        private static extern string FVBridge_GetRole();

        [DllImport("__Internal")]
        private static extern void FVBridge_SaveState(string json);

        [DllImport("__Internal")]
        private static extern void FVBridge_LogDecision(string json);

        [DllImport("__Internal")]
        private static extern void FVBridge_StartSession(string gameMode);

        [DllImport("__Internal")]
        private static extern void FVBridge_EndSession(string sessionId);
#endif

        public static string GetCsrfToken()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return FVBridge_GetCsrfToken();
#else
            return "";
#endif
        }

        public static bool IsSignedIn()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return FVBridge_IsSignedIn() == 1;
#else
            return false;
#endif
        }

        public static string GetRole()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return FVBridge_GetRole();
#else
            return "guest";
#endif
        }

        public static void SaveState(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FVBridge_SaveState(json);
#else
            Debug.Log($"[JSBridge] SaveState (editor): {json}");
#endif
        }

        public static void LogDecision(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FVBridge_LogDecision(json);
#else
            Debug.Log($"[JSBridge] LogDecision (editor): {json}");
#endif
        }

        public static void StartSession(string gameMode)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FVBridge_StartSession(gameMode);
#else
            Debug.Log($"[JSBridge] StartSession (editor): {gameMode}");
#endif
        }

        public static void EndSession(string sessionId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            FVBridge_EndSession(sessionId);
#else
            Debug.Log($"[JSBridge] EndSession (editor): {sessionId}");
#endif
        }
    }
}
