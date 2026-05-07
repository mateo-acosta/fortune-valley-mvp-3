using UnityEngine;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays player and rival balances.
    /// Player balance comes from OnCheckingBalanceChanged,
    /// rival balance from OnRivalBalanceChanged.
    /// Currency formatting is shared via CurrencyFormatter.
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
                _playerBalanceText.text = CurrencyFormatter.FormatCurrency(balance);
            }
        }

        private void HandleRivalBalanceChanged(float balance)
        {
            if (_rivalBalanceText != null)
            {
                _rivalBalanceText.text = CurrencyFormatter.FormatCurrency(balance);
            }
        }
    }
}
