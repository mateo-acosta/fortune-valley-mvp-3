using System.Collections;
using UnityEngine;
using FortuneValley.Domain.Entities;
using FortuneValley.Core;

namespace FortuneValley.UI
{
    /// <summary>
    /// Wires Coach Val chat into the Game End panel.
    /// Listens for game end events, creates CoachChatUI on the ChatColumn,
    /// and cleans up on game restart.
    /// </summary>
    public class GameEndChatIntegration : MonoBehaviour
    {
        // ===============================================================
        // SERIALIZED FIELDS (wired by GameEndPanelBuilder)
        // ===============================================================

        [Header("Coach Config")]
        [SerializeField] private CoachConfig _coachConfig;

        [Header("Dependencies")]
        [SerializeField] private InvestmentSystem _investmentSystem;

        [Header("UI")]
        [Tooltip("The ChatColumn transform to populate with the chat UI")]
        [SerializeField] private Transform _chatColumn;

        // ===============================================================
        // STATE
        // ===============================================================

        private CoachChatUI _chatUI;

        // ===============================================================
        // LIFECYCLE
        // ===============================================================

        private void OnEnable()
        {
            GameEvents.OnGameEndWithSummary += HandleGameEnd;
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnGameEndWithSummary -= HandleGameEnd;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        // ===============================================================
        // EVENT HANDLERS
        // ===============================================================

        private void HandleGameEnd(bool isPlayerWin, GameSummary summary)
        {
            // Wait one frame for GameEndPanel to finish populating stats
            StartCoroutine(InitializeChatNextFrame(isPlayerWin, summary));
        }

        private void HandleGameStart()
        {
            // Clean up chat on new game
            if (_chatUI != null)
            {
                _chatUI.Cleanup();
                Destroy(_chatUI);
                _chatUI = null;
            }

            // Clear any dynamic children from the chat column
            if (_chatColumn != null)
            {
                foreach (Transform child in _chatColumn)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        // ===============================================================
        // INITIALIZATION
        // ===============================================================

        private IEnumerator InitializeChatNextFrame(bool isPlayerWin, GameSummary summary)
        {
            yield return null; // Wait one frame

            if (_chatColumn == null)
            {
                UnityEngine.Debug.LogWarning("[GameEndChatIntegration] ChatColumn not wired.");
                yield break;
            }

            if (_coachConfig == null)
            {
                UnityEngine.Debug.LogWarning("[GameEndChatIntegration] CoachConfig not wired.");
                yield break;
            }

            // Clean up previous chat if any
            if (_chatUI != null)
            {
                _chatUI.Cleanup();
                Destroy(_chatUI);
            }

            // Clear existing children
            foreach (Transform child in _chatColumn)
            {
                Destroy(child.gameObject);
            }

            // Create CoachChatUI on the chat column
            _chatUI = _chatColumn.gameObject.AddComponent<CoachChatUI>();
            _chatUI.Initialize(_coachConfig, isPlayerWin, summary, _investmentSystem);
        }
    }
}
