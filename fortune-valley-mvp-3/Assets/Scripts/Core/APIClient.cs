using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Handles HTTP persistence to the Rails backend.
    /// Buffers decision events and flushes every 5 seconds.
    /// State saves are sent immediately via JS bridge (no UnityWebRequest needed
    /// since the browser JS handles the fetch).
    /// </summary>
    public class APIClient : MonoBehaviour
    {
        [SerializeField] private float _flushIntervalSeconds = 5f;

        private readonly List<string> _decisionBuffer = new List<string>();
        private float _timeSinceLastFlush;

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
            JSBridge.SaveState(json);
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
                JSBridge.LogDecision(_decisionBuffer[i]);
            }

            _decisionBuffer.Clear();
        }

        /// <summary>
        /// Check if the player is signed in as a student (persistence available).
        /// </summary>
        public bool CanPersist()
        {
            return JSBridge.IsSignedIn() && JSBridge.GetRole() == "student";
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
