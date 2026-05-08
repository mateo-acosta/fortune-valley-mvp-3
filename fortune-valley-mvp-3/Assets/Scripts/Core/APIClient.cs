using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Handles HTTP persistence to the Rails backend.
    /// Buffers decision events and flushes every 5 seconds.
    /// State saves are sent immediately via JS bridge (no UnityWebRequest needed
    /// since the browser JS handles the fetch).
    /// </summary>
    public class APIClient : MonoBehaviour, IAPIClient
    {
        [SerializeField] private float _flushIntervalSeconds = 5f;

        private readonly List<string> _decisionBuffer = new List<string>();
        private float _timeSinceLastFlush;
        private IJSBridge _bridge;

        private IJSBridge Bridge
        {
            get
            {
                if (_bridge == null) _bridge = new StaticJSBridge();
                return _bridge;
            }
        }

        // Test hook: lets PlayMode/EditMode tests swap the bridge.
        // Production code never sets this.
        public void SetBridge(IJSBridge bridge) { _bridge = bridge; }

        private void Update()
        {
            if (_decisionBuffer.Count == 0) return;

            _timeSinceLastFlush += Time.unscaledDeltaTime;
            if (_timeSinceLastFlush >= _flushIntervalSeconds)
            {
                FlushDecisions();
            }
        }

        /// <summary>
        /// Save full game state. Delegates to JS bridge which calls
        /// the Rails API with proper CSRF and cookies.
        /// </summary>
        public void SaveState(GamePlayerStateDTO state)
        {
            string json = JsonUtility.ToJson(state);
            Bridge.SaveState(json);
        }

        /// <summary>
        /// Server-side reset of the player's state for a given game mode.
        /// POSTs a fresh DTO with default balances and empty collections;
        /// the Rails controller performs find_or_initialize_by on
        /// (student_id, game_mode) and assign_attributes, so this overwrites
        /// every persisted field in a single round trip. Used by the
        /// Replay Tutorial flow: clicking Replay confirms and wipes
        /// progress before the onboarding tutorial re-runs.
        /// </summary>
        public void WipePlayerState(string gameMode)
        {
            var fresh = BuildFreshState(gameMode);
            SaveState(fresh);
        }

        private static GamePlayerStateDTO BuildFreshState(string gameMode)
        {
            return new GamePlayerStateDTO
            {
                game_mode = string.IsNullOrEmpty(gameMode) ? "homebase" : gameMode,
                current_day = 0,
                checking_balance = 0f,
                credit_balance = 0f,
                investment_balance = 0f,
                credit_score = 650,
                budget_variance_streak = 0,
                tax_liability_ytd = 0f,
                monthly_income = 0f,
                lots_owned = new string[0],
                rival_lots_owned = new string[0],
                learning_levels_completed = new string[0],
                investment_holdings = new InvestmentHoldingDTO[0],
                active_loans = new ActiveLoanDTO[0],
                insurance_policies = new ActiveInsurancePolicyDTO[0],
                consecutive_insolvent_months = 0,
                bankruptcy_flag = false,
                restaurant_level = 1,
                current_tick = 0,
                cosmetic_variants = new CosmeticVariantChoice[0],
                tutorial_completed = false
            };
        }

        /// <summary>
        /// Enqueue a decision event for batched sending.
        /// </summary>
        public void EnqueueDecision(DecisionEventDTO decision)
        {
#if UNITY_INCLUDE_TESTS
            LastEnqueuedDecision = decision;
#endif
            string json = JsonUtility.ToJson(decision);
            _decisionBuffer.Add(json);
        }

        /// <summary>
        /// Immediately flush all buffered decisions to the server.
        /// Called on flush interval and on game end/tab close.
        /// </summary>
        public void FlushDecisions()
        {
            _timeSinceLastFlush = 0f;

            if (_decisionBuffer.Count == 0) return;

            // Send each buffered decision via JS bridge
            for (int i = 0; i < _decisionBuffer.Count; i++)
            {
                Bridge.LogDecision(_decisionBuffer[i]);
            }

            _decisionBuffer.Clear();
        }

        /// <summary>
        /// Check if the current session can persist game data to the server.
        ///
        /// Persistence is allowed for:
        ///  - students (student_id in JWT, writes go to their own data)
        ///  - teacher preview sessions (student_id is null, JWT role is
        ///    "teacher_preview"; the Rails API flags these sessions with
        ///    preview: true so they are excluded from classroom aggregates)
        /// </summary>
        public bool CanPersist()
        {
            if (!Bridge.IsSignedIn()) return false;

            string role = Bridge.GetRole();
            return role == "student" || role == "teacher_preview";
        }

        /// <summary>
        /// Current JWT role as reported by the JS bridge (e.g. "student",
        /// "teacher_preview"). Returned verbatim so callers can apply their
        /// own branching rules without re-implementing the bridge.
        /// </summary>
        public string GetRole() => Bridge.GetRole();

        /// <summary>
        /// Fire-and-forget telemetry event. Forwarded through the JS bridge
        /// to the Rails telemetry endpoint, which captures via Sentry. Used
        /// for low-volume diagnostic signals (e.g. tutorial-gate decisions)
        /// without adding a browser SDK to the WebGL build.
        /// </summary>
        public void ReportTelemetry(string eventName, string propertiesJson)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            Bridge.ReportEvent(eventName, propertiesJson ?? "{}");
        }

#if UNITY_INCLUDE_TESTS
        /// <summary>
        /// Test spy: stores the last DTO passed to EnqueueDecision.
        /// Only compiled into test assemblies.
        /// </summary>
        public DecisionEventDTO LastEnqueuedDecision { get; private set; }
#endif
    }
}
