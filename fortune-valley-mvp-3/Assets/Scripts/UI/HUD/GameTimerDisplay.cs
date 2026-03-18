using UnityEngine;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays the current game day as "Day X".
    /// Updates every tick.
    /// </summary>
    public class GameTimerDisplay : MonoBehaviour
    {
        [Header("Text Reference")]
        [SerializeField] private TMP_Text _timerText;

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void HandleGameStart()
        {
            if (_timerText != null)
            {
                _timerText.text = "Day 0";
            }
        }

        private void HandleTick(int tickNumber)
        {
            if (_timerText != null)
            {
                _timerText.text = $"Day {tickNumber}";
            }
        }
    }
}
