using UnityEngine;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays player and rival lot counts on the scoreboard.
    /// Updates whenever a lot is purchased.
    /// </summary>
    public class ScoreboardDisplay : MonoBehaviour
    {
        [Header("Text References")]
        [SerializeField] private TMP_Text _playerLotCountText;
        [SerializeField] private TMP_Text _rivalLotCountText;

        [Header("Dependencies")]
        [SerializeField] private CityManager _cityManager;

        private void OnEnable()
        {
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void HandleGameStart()
        {
            UpdateCounts();
        }

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            UpdateCounts();
        }

        private void UpdateCounts()
        {
            if (_cityManager == null) return;

            if (_playerLotCountText != null)
            {
                _playerLotCountText.text = _cityManager.PlayerLotCount.ToString();
            }

            if (_rivalLotCountText != null)
            {
                _rivalLotCountText.text = _cityManager.RivalLotCount.ToString();
            }
        }
    }
}
