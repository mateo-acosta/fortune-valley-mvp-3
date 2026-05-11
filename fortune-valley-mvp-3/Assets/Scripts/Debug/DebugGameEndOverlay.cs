#if UNITY_EDITOR
using UnityEngine;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Editor-only IMGUI overlay that draws DBG: Win / DBG: Lose buttons in the
    /// top-right corner during Play Mode. Auto-spawns once after scene load so
    /// no Inspector wiring is required.
    ///
    /// Why we activate the panel ourselves: the Homebase scene starts the
    /// GameEndPanel inactive, and (depending on the loaded scene) there may
    /// be no GameFlowController instance present to flip it on when the event
    /// fires. So the overlay locates the inactive panel directly, calls
    /// SetActive(true) (which synchronously runs its OnEnable + subscribes
    /// to OnGameEndWithSummary), then raises the event so the now-subscribed
    /// panel populates and shows itself.
    ///
    /// Wrapped in #if UNITY_EDITOR so this file compiles to nothing in builds.
    /// </summary>
    public class DebugGameEndOverlay : MonoBehaviour
    {
        // Use string-based type lookup so this Core-layer debug file does not
        // import FortuneValley.UI.Panels (Core -> UI is forbidden by CLAUDE.md
        // architecture rules).
        private const string GameEndPanelTypeName = "FortuneValley.UI.Panels.GameEndPanel";

        [SerializeField] private int _buttonWidth = 150;
        [SerializeField] private int _buttonHeight = 32;
        [SerializeField] private int _padding = 8;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoSpawn()
        {
            var go = new GameObject("[DebugGameEndOverlay]");
            go.AddComponent<DebugGameEndOverlay>();
            DontDestroyOnLoad(go);
        }

        private void OnGUI()
        {
            int x = Screen.width - _buttonWidth - _padding;
            int y = _padding;

            if (GUI.Button(new Rect(x, y, _buttonWidth, _buttonHeight), "DBG: Win Panel"))
            {
                Fire(true, DebugGameEndButton.BuildMockWinSummary());
            }

            y = y + _buttonHeight + _padding;

            if (GUI.Button(new Rect(x, y, _buttonWidth, _buttonHeight), "DBG: Lose Panel"))
            {
                Fire(false, DebugGameEndButton.BuildMockLoseSummary());
            }
        }

        private static void Fire(bool isWin, GameSummary summary)
        {
            ActivateGameEndPanel();
            UnityEngine.Debug.Log("[DebugGameEndOverlay] Raising OnGameEndWithSummary (isWin=" + isWin + ")");
            GameEvents.RaiseGameEndWithSummary(isWin, summary);
        }

        private static void ActivateGameEndPanel()
        {
            var panelType = ResolveGameEndPanelType();
            if (panelType == null)
            {
                UnityEngine.Debug.LogError("[DebugGameEndOverlay] Could not resolve type " + GameEndPanelTypeName + ". Cannot activate panel.");
                return;
            }

            // Resources.FindObjectsOfTypeAll returns inactive scene objects too,
            // unlike FindObjectsByType / GameObject.Find. This is the standard
            // editor-debug way to reach an inactive UI panel without keeping a
            // serialized reference.
            var found = Resources.FindObjectsOfTypeAll(panelType);
            if (found == null || found.Length == 0)
            {
                UnityEngine.Debug.LogError("[DebugGameEndOverlay] No GameEndPanel found in loaded scenes.");
                return;
            }

            // Prefer a real scene instance (skip prefab assets that also show up
            // in Resources.FindObjectsOfTypeAll).
            for (int i = 0; i < found.Length; i++)
            {
                var mb = found[i] as MonoBehaviour;
                if (mb == null) continue;
                var go = mb.gameObject;
                if (!go.scene.IsValid()) continue;

                if (!go.activeSelf)
                {
                    UnityEngine.Debug.Log("[DebugGameEndOverlay] Activating " + go.name + " so its OnEnable can subscribe.");
                    go.SetActive(true);
                }
                return;
            }

            UnityEngine.Debug.LogError("[DebugGameEndOverlay] Found GameEndPanel only as a prefab asset, no scene instance.");
        }

        private static System.Type ResolveGameEndPanelType()
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var t = assemblies[i].GetType(GameEndPanelTypeName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
#endif
