using UnityEngine;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays the current game day as "Day X".
    /// Updates when a day completes via OnDayEnd.
    /// </summary>
    public class GameTimerDisplay : MonoBehaviour
    {
        [Header("Text Reference")]
        [SerializeField] private TMP_Text _timerText;

        private void OnEnable()
        {
            GameEvents.OnDayEnd += HandleDayEnd;
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnDayEnd -= HandleDayEnd;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void HandleGameStart()
        {
            if (_timerText != null)
            {
                _timerText.text = "Day 0";
            }
        }

        private void HandleDayEnd(int dayNumber)
        {
            if (_timerText != null)
            {
                _timerText.text = $"Day {dayNumber}";
            }
        }
    }
}
