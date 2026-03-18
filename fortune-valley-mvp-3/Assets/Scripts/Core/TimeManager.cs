using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Interfaces;

namespace FortuneValley.Core
{
    /// <summary>
    /// Controls the game's time flow. Emits ticks that drive all game systems.
    ///
    /// DESIGN NOTE: Tick-based simulation (not real-time) makes compound interest
    /// moments explicit and easier for students to observe.
    /// </summary>
    public class TimeManager : MonoBehaviour, ITimeService
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION (easily tweakable for gameplay testing)
        // ═══════════════════════════════════════════════════════════════

        [Header("Time Settings")]
        [Tooltip("Seconds between ticks at 1x speed. Lower = faster game.")]
        [SerializeField] private float _secondsPerTick = 1f;

        [Tooltip("Available speed multipliers (e.g., pause=0, normal=1, fast=2)")]
        [SerializeField] private float[] _speedOptions = { 0f, 1f, 2f, 4f };

        [Header("Debug")]
        [SerializeField] private bool _logTicks = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private int _currentTick = 0;
        private float _timeSinceLastTick = 0f;
        private int _currentSpeedIndex = 1; // Default to 1x speed
        private bool _isRunning = false;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Current tick number (essentially "days" in game time).
        /// </summary>
        public int CurrentTick => _currentTick;

        /// <summary>
        /// Current game speed multiplier.
        /// </summary>
        public float CurrentSpeed => _speedOptions[_currentSpeedIndex];

        /// <summary>
        /// Whether the game simulation is running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Whether the game is paused (speed = 0).
        /// </summary>
        public bool IsPaused => CurrentSpeed == 0f;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            // Listen for game start/end to control time flow
            GameEvents.OnGameStart += HandleGameStart;
            GameEvents.OnGameEnd += HandleGameEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStart -= HandleGameStart;
            GameEvents.OnGameEnd -= HandleGameEnd;
        }

        private void Update()
        {
            if (!_isRunning || IsPaused)
                return;

            // Accumulate time, scaled by current speed
            _timeSinceLastTick += Time.deltaTime * CurrentSpeed;

            // Emit tick(s) when enough time has passed
            while (_timeSinceLastTick >= _secondsPerTick)
            {
                _timeSinceLastTick -= _secondsPerTick;
                EmitTick();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Start the game clock.
        /// </summary>
        public void StartTime()
        {
            _isRunning = true;
        }

        /// <summary>
        /// Stop the game clock.
        /// </summary>
        public void StopTime()
        {
            _isRunning = false;
        }

        /// <summary>
        /// Reset tick counter to zero.
        /// </summary>
        public void ResetTime()
        {
            _currentTick = 0;
            _timeSinceLastTick = 0f;
        }

        /// <summary>
        /// Cycle to the next speed option.
        /// </summary>
        public void CycleSpeed()
        {
            _currentSpeedIndex = (_currentSpeedIndex + 1) % _speedOptions.Length;
            GameEvents.RaiseGameSpeedChanged(CurrentSpeed);
        }

        /// <summary>
        /// Set speed to a specific index.
        /// </summary>
        public void SetSpeedIndex(int index)
        {
            if (index >= 0 && index < _speedOptions.Length)
            {
                _currentSpeedIndex = index;
                GameEvents.RaiseGameSpeedChanged(CurrentSpeed);
            }
        }

        /// <summary>
        /// Toggle pause (speed 0) and previous speed.
        /// </summary>
        public void TogglePause()
        {
            if (IsPaused)
            {
                // Unpause: go to 1x if we were at 0
                if (_currentSpeedIndex == 0)
                    _currentSpeedIndex = 1;
            }
            else
            {
                // Pause: go to 0
                _currentSpeedIndex = 0;
            }
            GameEvents.RaiseGameSpeedChanged(CurrentSpeed);
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void EmitTick()
        {
            _currentTick++;

            if (_logTicks)
            {
                Debug.Log($"[TimeManager] Tick {_currentTick}");
            }

            GameEvents.RaiseTick(_currentTick);
        }

        private void HandleGameStart()
        {
            ResetTime();
            StartTime();
        }

        private void HandleGameEnd(Owner winner)
        {
            StopTime();
        }
    }
}
