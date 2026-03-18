using UnityEngine;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.Feedback
{
    /// <summary>
    /// Celebrates when an investment compounds by showing sparkle effects
    /// and floating "+$X compound!" text.
    ///
    /// LEARNING DESIGN: This is the key moment for teaching compound interest.
    /// By celebrating every compound event, we make the abstract concept tangible.
    /// Students should feel excitement when they see their money grow.
    /// </summary>
    public class CompoundCelebration : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("UI References")]
        [Tooltip("Pool of floating text for compound messages")]
        [SerializeField] private FloatingText _floatingTextPrefab;
        [SerializeField] private Transform _floatingTextParent;

        [Header("Portfolio Panel Reference")]
        [Tooltip("The portfolio panel to show celebration near")]
        [SerializeField] private RectTransform _portfolioPanelRect;

        [Header("Particle Effects")]
        [Tooltip("Particle effect to play on compound")]
        [SerializeField] private ParticleSystem _sparkleEffect;

        [Header("Colors")]
        [SerializeField] private Color _compoundTextColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _lowRiskColor = new Color(0.3f, 0.9f, 0.4f);
        [SerializeField] private Color _mediumRiskColor = new Color(0.9f, 0.8f, 0.2f);
        [SerializeField] private Color _highRiskColor = new Color(1f, 0.5f, 0.2f);

        [Header("Settings")]
        [Tooltip("Minimum compound amount to show celebration")]
        [SerializeField] private float _minimumAmountToShow = 0.5f;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private System.Collections.Generic.List<FloatingText> _textPool =
            new System.Collections.Generic.List<FloatingText>();
        private int _poolSize = 5;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            InitializePool();
        }

        private void OnEnable()
        {
            GameEvents.OnInvestmentCompounded += HandleCompound;
        }

        private void OnDisable()
        {
            GameEvents.OnInvestmentCompounded -= HandleCompound;
        }

        // ═══════════════════════════════════════════════════════════════
        // INITIALIZATION
        // ═══════════════════════════════════════════════════════════════

        private void InitializePool()
        {
            if (_floatingTextPrefab == null || _floatingTextParent == null) return;

            for (int i = 0; i < _poolSize; i++)
            {
                var text = Instantiate(_floatingTextPrefab, _floatingTextParent);
                text.gameObject.SetActive(false);
                _textPool.Add(text);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleCompound(ActiveInvestment investment)
        {
            if (investment == null) return;

            float gain = investment.LastCompoundGain;
            if (gain < _minimumAmountToShow) return;

            // Determine color based on risk level
            Color textColor = GetColorForRisk(investment.Definition?.RiskLevel ?? RiskLevel.Low);

            // Show floating text
            ShowCompoundText(gain, textColor);

            // Play particle effect
            PlaySparkles();
        }

        // ═══════════════════════════════════════════════════════════════
        // VISUAL FEEDBACK
        // ═══════════════════════════════════════════════════════════════

        private void ShowCompoundText(float amount, Color color)
        {
            var text = GetTextFromPool();
            if (text == null) return;

            // Format message
            string message = $"+${amount:N2} compound!";

            // Position near portfolio panel or center of screen
            Vector3 position = GetCelebrationPosition();

            text.Show(message, position, color);
        }

        private void PlaySparkles()
        {
            if (_sparkleEffect != null)
            {
                // Position sparkles at celebration position
                _sparkleEffect.transform.position = GetCelebrationPosition();
                _sparkleEffect.Play();
            }
        }

        private Vector3 GetCelebrationPosition()
        {
            // If we have a portfolio panel reference, show near it
            if (_portfolioPanelRect != null)
            {
                Vector3[] corners = new Vector3[4];
                _portfolioPanelRect.GetWorldCorners(corners);
                // Return position slightly above center
                return (corners[0] + corners[2]) / 2f + Vector3.up * 50f;
            }

            // Default to center of screen
            return new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        }

        private Color GetColorForRisk(RiskLevel risk)
        {
            switch (risk)
            {
                case RiskLevel.Low:
                    return _lowRiskColor;
                case RiskLevel.Medium:
                    return _mediumRiskColor;
                case RiskLevel.High:
                    return _highRiskColor;
                default:
                    return _compoundTextColor;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // POOL METHODS
        // ═══════════════════════════════════════════════════════════════

        private FloatingText GetTextFromPool()
        {
            foreach (var text in _textPool)
            {
                if (!text.IsAnimating)
                {
                    return text;
                }
            }

            // Expand pool if needed
            if (_floatingTextPrefab != null && _floatingTextParent != null)
            {
                var newText = Instantiate(_floatingTextPrefab, _floatingTextParent);
                _textPool.Add(newText);
                return newText;
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS (for testing)
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Manually trigger a compound celebration (for testing).
        /// </summary>
        public void TriggerCelebration(float amount, RiskLevel risk = RiskLevel.Medium)
        {
            ShowCompoundText(amount, GetColorForRisk(risk));
            PlaySparkles();
        }
    }
}
