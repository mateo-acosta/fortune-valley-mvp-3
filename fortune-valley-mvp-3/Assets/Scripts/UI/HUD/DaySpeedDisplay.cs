using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays the current day and game speed controls.
    /// </summary>
    public class DaySpeedDisplay : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // UI REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Day Display")]
        [SerializeField] private TextMeshProUGUI _dayText;

        [Header("Speed Controls")]
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _speed1xButton;
        [SerializeField] private Button _speed2xButton;

        [Header("Speed Button Visuals")]
        [SerializeField] private Color _activeSpeedColor = new Color(0.3f, 0.7f, 1f);
        [SerializeField] private Color _inactiveSpeedColor = Color.white;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private int _currentDay;
        private float _currentSpeed = 1f;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            GameEvents.OnGameSpeedChanged += HandleSpeedChanged;
            GameEvents.OnTick += HandleTick;
        }

        private void OnDisable()
        {
            GameEvents.OnGameSpeedChanged -= HandleSpeedChanged;
            GameEvents.OnTick -= HandleTick;
        }

        private void Start()
        {
            SetupButtons();
            UpdateSpeedButtonVisuals();
        }

        private void SetupButtons()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(() => SetSpeed(0f));
            }

            if (_speed1xButton != null)
            {
                _speed1xButton.onClick.AddListener(() => SetSpeed(1f));
            }

            if (_speed2xButton != null)
            {
                _speed2xButton.onClick.AddListener(() => SetSpeed(2f));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleSpeedChanged(float speed)
        {
            _currentSpeed = speed;
            UpdateSpeedButtonVisuals();
        }

        private void HandleTick(int tickNumber)
        {
            // Convert ticks to days (assuming TimeManager handles this)
            // For now, just use tick number as a rough day indicator
            // The actual day calculation depends on TimeManager's ticksPerDay setting
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Update the day display.
        /// </summary>
        public void UpdateDay(int day)
        {
            _currentDay = day;
            if (_dayText != null)
            {
                _dayText.text = $"Day {day}";
            }
        }

        /// <summary>
        /// Set the game speed.
        /// </summary>
        public void SetSpeed(float speed)
        {
            _currentSpeed = speed;
            Time.timeScale = speed;
            GameEvents.RaiseGameSpeedChanged(speed);
            UpdateSpeedButtonVisuals();
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateSpeedButtonVisuals()
        {
            UpdateButtonColor(_pauseButton, _currentSpeed == 0f);
            UpdateButtonColor(_speed1xButton, Mathf.Approximately(_currentSpeed, 1f));
            UpdateButtonColor(_speed2xButton, Mathf.Approximately(_currentSpeed, 2f));
        }

        private void UpdateButtonColor(Button button, bool isActive)
        {
            if (button == null) return;

            var colors = button.colors;
            colors.normalColor = isActive ? _activeSpeedColor : _inactiveSpeedColor;
            button.colors = colors;
        }

        // ═══════════════════════════════════════════════════════════════
        // ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public int CurrentDay => _currentDay;
        public float CurrentSpeed => _currentSpeed;
        public bool IsPaused => _currentSpeed == 0f;
    }
}
