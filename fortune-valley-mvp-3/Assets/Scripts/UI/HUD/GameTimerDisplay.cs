using UnityEngine;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays the player's current age. The underlying tick is in days,
    /// but the player only ever sees their age in years; days never appear
    /// in the UI to avoid the "30 days = 1 year" confusion.
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
                _timerText.text = $"Age {LifespanConstants.StartingAge}";
            }
        }

        private void HandleDayEnd(int dayNumber)
        {
            if (_timerText != null)
            {
                _timerText.text = $"Age {LifespanConstants.AgeFromTick(dayNumber)}";
            }
        }
    }
}
