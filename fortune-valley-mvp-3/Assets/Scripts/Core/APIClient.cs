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

#if UNITY_INCLUDE_TESTS
        /// <summary>
        /// Test spy: stores the last DTO passed to EnqueueDecision.
        /// Only compiled into test assemblies.
        /// </summary>
        public DecisionEventDTO LastEnqueuedDecision { get; private set; }
#endif
    }
}
