using UnityEngine;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Persistent HUD element showing the player's current credit score.
    /// Subscribes to OnCreditScoreChanged and re-displays on game start.
    ///
    /// LEARNING DESIGN: Keeping the credit score visible at all times
    /// reinforces that every financial decision affects creditworthiness.
    /// </summary>
    public class CreditScoreHUD : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _labelText;

        [Header("Colors")]
        [SerializeField] private Color _excellentColor = new Color(0.2f, 0.8f, 0.2f);   // 750+
        [SerializeField] private Color _goodColor = new Color(0.6f, 0.8f, 0.2f);        // 700-749
        [SerializeField] private Color _fairColor = new Color(0.9f, 0.7f, 0.1f);        // 650-699
        [SerializeField] private Color _poorColor = new Color(0.8f, 0.2f, 0.2f);        // below 650

        private void OnEnable()
        {
            GameEvents.OnCreditScoreChanged += HandleScoreChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCreditScoreChanged -= HandleScoreChanged;
        }

        private void HandleScoreChanged(int newScore)
        {
            if (_scoreText == null) return;

            _scoreText.text = newScore.ToString();
            _scoreText.color = GetScoreColor(newScore);
        }

        private Color GetScoreColor(int score)
        {
            if (score >= 750) return _excellentColor;
            if (score >= 700) return _goodColor;
            if (score >= 650) return _fairColor;
            return _poorColor;
        }
    }
}
