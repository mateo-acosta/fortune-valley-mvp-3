using UnityEngine;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays player and rival balances.
    /// Player balance comes from OnCheckingBalanceChanged,
    /// rival balance from OnRivalBalanceChanged.
    /// </summary>
    public class BalanceDisplay : MonoBehaviour
    {
        [Header("Text References")]
        [SerializeField] private TMP_Text _playerBalanceText;
        [SerializeField] private TMP_Text _rivalBalanceText;

        private void OnEnable()
        {
            GameEvents.OnCheckingBalanceChanged += HandlePlayerBalanceChanged;
            GameEvents.OnRivalBalanceChanged += HandleRivalBalanceChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnCheckingBalanceChanged -= HandlePlayerBalanceChanged;
            GameEvents.OnRivalBalanceChanged -= HandleRivalBalanceChanged;
        }

        private void HandlePlayerBalanceChanged(float balance, float delta)
        {
            if (_playerBalanceText != null)
            {
                _playerBalanceText.text = FormatCurrency(balance);
            }
        }

        private void HandleRivalBalanceChanged(float balance)
        {
            if (_rivalBalanceText != null)
            {
                _rivalBalanceText.text = FormatCurrency(balance);
            }
        }

        /// <summary>
        /// Format currency: $X,XXX for >= 1000, $X.XX otherwise.
        /// </summary>
        private static string FormatCurrency(float amount)
        {
            if (amount >= 1000f)
            {
                return $"${amount:N0}";
            }
            return $"${amount:F2}";
        }
    }
}
