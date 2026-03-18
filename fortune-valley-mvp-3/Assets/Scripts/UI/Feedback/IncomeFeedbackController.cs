using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;
using FortuneValley.UI.HUD;

namespace FortuneValley.UI.Feedback
{
    /// <summary>
    /// Orchestrates the income visual feedback system.
    /// When income is generated: FloatingText -> AccountPulse
    ///
    /// LEARNING DESIGN: This creates a satisfying feedback loop that makes
    /// earning money feel rewarding. Students should feel good about income,
    /// which sets up the contrast with the decision to invest (delayed gratification).
    /// </summary>
    public class IncomeFeedbackController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Pool Sizes")]
        [SerializeField] private int _floatingTextPoolSize = 10;

        [Header("Prefabs")]
        [Tooltip("Prefab for floating text (must have FloatingText component)")]
        [SerializeField] private FloatingText _floatingTextPrefab;

        [Header("References")]
        [Tooltip("The world-space canvas for 3D floating text")]
        [SerializeField] private Canvas _worldCanvas;

        [Tooltip("The checking account display to pulse")]
        [SerializeField] private AccountDisplay _checkingDisplay;

        [Header("Settings")]
        [Tooltip("Minimum income amount to show feedback for")]
        [SerializeField] private float _minimumAmountToShow = 1f;


        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private List<FloatingText> _floatingTextPool = new List<FloatingText>();

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            FindReferences();
            InitializePools();
        }

        private void OnEnable()
        {
            GameEvents.OnIncomeGeneratedWithPosition += HandleIncomeWithPosition;
            GameEvents.OnRivalIncomeGeneratedWithPosition += HandleRivalIncomeWithPosition;
        }

        private void OnDisable()
        {
            GameEvents.OnIncomeGeneratedWithPosition -= HandleIncomeWithPosition;
            GameEvents.OnRivalIncomeGeneratedWithPosition -= HandleRivalIncomeWithPosition;
        }

        // ═══════════════════════════════════════════════════════════════
        // INITIALIZATION
        // ═══════════════════════════════════════════════════════════════

        private void FindReferences()
        {
            if (_checkingDisplay == null)
            {
                // Find checking account display in scene
                var displays = FindObjectsByType<AccountDisplay>(FindObjectsSortMode.None);
                foreach (var display in displays)
                {
                    if (display.AccountType == AccountType.Checking)
                    {
                        _checkingDisplay = display;
                        break;
                    }
                }
            }
        }

        private void InitializePools()
        {
            // Create floating text pool
            if (_floatingTextPrefab != null && _worldCanvas != null)
            {
                for (int i = 0; i < _floatingTextPoolSize; i++)
                {
                    var text = Instantiate(_floatingTextPrefab, _worldCanvas.transform);
                    text.gameObject.SetActive(false);
                    _floatingTextPool.Add(text);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleIncomeWithPosition(float amount, Vector3 worldPosition)
        {
            if (amount < _minimumAmountToShow) return;

            // Step 1: Show floating text at world position
            ShowFloatingText(amount, worldPosition);

            // Step 2: Pulse the account display after a short delay
            Invoke(nameof(PulseAccount), 0.5f);
        }

        private void HandleRivalIncomeWithPosition(float amount, Vector3 worldPosition)
        {
            if (amount < _minimumAmountToShow) return;

            // Show red floating text for rival income (no account pulse)
            var text = GetFloatingTextFromPool();
            if (text == null) return;

            string message = $"+${amount:N0}";
            text.Show(message, worldPosition, new Color(0.9f, 0.2f, 0.2f));
        }

        // ═══════════════════════════════════════════════════════════════
        // FEEDBACK METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show floating "+$X" text at world position.
        /// </summary>
        private void ShowFloatingText(float amount, Vector3 worldPosition)
        {
            var text = GetFloatingTextFromPool();
            if (text == null) return;

            string message = $"+${amount:N0}";
            text.Show(message, worldPosition, true);
        }

        /// <summary>
        /// Pulse the checking account display to draw attention.
        /// </summary>
        private void PulseAccount()
        {
            if (_checkingDisplay != null)
            {
                _checkingDisplay.PulseOnIncome();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // POOL METHODS
        // ═══════════════════════════════════════════════════════════════

        private FloatingText GetFloatingTextFromPool()
        {
            foreach (var text in _floatingTextPool)
            {
                if (!text.IsAnimating)
                {
                    return text;
                }
            }

            // Pool exhausted - expand if we have a prefab
            if (_floatingTextPrefab != null && _worldCanvas != null)
            {
                var text = Instantiate(_floatingTextPrefab, _worldCanvas.transform);
                _floatingTextPool.Add(text);
                return text;
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS (for testing/manual triggering)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Manually trigger income feedback (for testing).
        /// </summary>
        public void TriggerIncomeFeedback(float amount, Vector3 worldPosition)
        {
            HandleIncomeWithPosition(amount, worldPosition);
        }
    }
}
