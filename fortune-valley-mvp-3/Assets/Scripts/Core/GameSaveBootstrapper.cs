using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Persistent receiver for the saved-state JSON delivered by the host page
    /// via window.FV.loadState round-trip. The Rails JS calls
    /// unityInstance.SendMessage("GameSaveBootstrapper", "OnSaveLoaded", json)
    /// once after Unity instantiates, so the receiving GameObject must exist by
    /// that point AND survive scene transitions (Homebase -> Learning Level).
    ///
    /// Lifetime: a single instance via DontDestroyOnLoad. Duplicates spawned in
    /// other scenes self-destruct on Awake. The original instance keeps the
    /// cached DTO and the static catch-up handles in GameEvents alive across
    /// scene loads, so newly-instantiated systems can hydrate from cached state
    /// without waiting for another network round-trip.
    ///
    /// Two-phase signal:
    ///   Phase 1: GameEvents.OnSaveStateLoaded(dto)  - systems hydrate self
    ///   Phase 2: GameEvents.OnSaveRestored          - one frame later, derived
    ///            state reconciles (e.g. CurrencyManager.RefreshInvestingBalance
    ///            after InvestmentSystem rebuilt its portfolio).
    ///
    /// No SerializeField references to systems by design - keeps the bootstrapper
    /// system-agnostic and avoids the cross-layer SerializeField method-call rule.
    /// </summary>
    public class GameSaveBootstrapper : MonoBehaviour
    {
        // Singleton guard: only the first-instantiated bootstrapper survives.
        private static GameSaveBootstrapper _existing;

        // Holds JSON delivered before Start has run. Applied from Start.
        private string _pendingSaveJson;

        // Most recent successfully-applied DTO. Mirrored to GameEvents.LastLoadedSaveDto
        // for late-joining systems; kept locally for diagnostics.
        private GamePlayerStateDTO _cachedDto;

        private bool _ready;
        private bool _reconcileQueued;

        private void Awake()
        {
            if (_existing != null && _existing != this)
            {
                Destroy(gameObject);
                return;
            }
            _existing = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_existing == this)
            {
                _existing = null;
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        private void Start()
        {
            _ready = true;
            if (!string.IsNullOrEmpty(_pendingSaveJson))
            {
                Apply(_pendingSaveJson);
                _pendingSaveJson = null;
            }
        }

        private void Update()
        {
            if (!_reconcileQueued) return;
            _reconcileQueued = false;
            GameEvents.HasSaveBeenRestored = true;
            GameEvents.RaiseSaveRestored();
        }

        /// <summary>
        /// SendMessage entry point. The host page calls
        /// unityInstance.SendMessage("GameSaveBootstrapper", "OnSaveLoaded", json)
        /// once after Unity ready. Public + parameterless-or-string-typed so
        /// Unity's SendMessage can dispatch.
        /// </summary>
        public void OnSaveLoaded(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            if (!_ready)
            {
                _pendingSaveJson = json;
                return;
            }
            Apply(json);
        }

        /// <summary>
        /// Also called by the Rails bridge when the consent banner is accepted
        /// (unity_bridge_controller.js wires "OnConsentGranted" on the same
        /// GameObject). Future hook; no-op for now.
        /// </summary>
        public void OnConsentGranted() { }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Intentionally NO auto-replay here. Auto-replay would clobber
            // in-progress state with the original cached DTO when a player
            // returns to a previously-loaded scene. Newly-loaded systems
            // catch up via GameEvents.LastLoadedSaveDto in their own OnEnable.
            // (See plan: ~/.claude/plans/what-is-the-plan-merry-quill.md, Issue 1 CQ.)
        }

        private void Apply(string json)
        {
            int byteLength = json != null ? json.Length : 0;
            Debug.Log($"[GameSaveBootstrapper] OnSaveLoaded received: {byteLength} bytes");

            GamePlayerStateDTO dto;
            try
            {
                dto = JsonUtility.FromJson<GamePlayerStateDTO>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameSaveBootstrapper] parse failed: {ex.Message}");
                return;
            }

            if (dto == null || string.IsNullOrEmpty(dto.game_mode))
            {
                Debug.Log("[GameSaveBootstrapper] empty/first-time payload, skipping restore");
                return;
            }

            _cachedDto = dto;

            // Set catch-up handle BEFORE invoking subscribers so a subscriber
            // reading LastLoadedSaveDto during the event sees the same DTO.
            GameEvents.LastLoadedSaveDto = dto;
            GameEvents.RaiseSaveStateLoaded(dto);

            // Phase 2 fires next frame so all Phase 1 subscribers are done.
            _reconcileQueued = true;

            int lotCount = dto.lots_owned != null ? dto.lots_owned.Length : 0;
            Debug.Log($"[GameSaveBootstrapper] restored {dto.game_mode} day={dto.current_day} lots={lotCount}");
        }

        /// <summary>
        /// Test hook: allows EditMode tests to force-apply a JSON payload
        /// synchronously without going through the SendMessage / Start lifecycle.
        /// </summary>
        public void ApplyForTest(string json)
        {
            _ready = true;
            Apply(json);
        }

        /// <summary>
        /// Test hook: clears the singleton guard so PlayMode tests can spin
        /// up a fresh bootstrapper without leaking the previous test's instance.
        /// </summary>
        public static void ResetExistingForTests()
        {
            _existing = null;
        }
    }
}
