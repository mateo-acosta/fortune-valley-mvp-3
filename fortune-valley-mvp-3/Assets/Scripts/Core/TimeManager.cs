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
    public class TimeManager : MonoBehaviour, ITimeService, ITickClock
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION (easily tweakable for gameplay testing)
        // ═══════════════════════════════════════════════════════════════

        [Header("Time Settings")]
        [Tooltip("Seconds between ticks at 1x speed. Lower = faster game. " +
                 "Default 0.4s tunes a full 40-year life (1200 days at " +
                 "LifespanConstants.DaysPerYear=30) to ~80 minutes real time.")]
        [SerializeField] private float _secondsPerTick = 0.4f;

        [Tooltip("Available speed multipliers (e.g., pause=0, normal=1, fast=2)")]
        [SerializeField] private float[] _speedOptions = { 0f, 1f, 2f, 4f };

        [Header("Day Cycle")]
        [Tooltip("Number of ticks that make up one in-game day. " +
                 "10 ticks * 0.4s = 4 sec/day, 30 days/year, 40 years = 4800s = 80min.")]
        [SerializeField] private int _ticksPerDay = 10;

        [Header("Debug")]
        [SerializeField] private bool _logTicks = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private int _currentTick = 0;
        private int _currentDay = 0;
        private float _timeSinceLastTick = 0f;
        private int _currentSpeedIndex = 1; // Default to 1x speed
        private bool _isRunning = false;

        // Reference-counted pause lock, orthogonal to the user-facing speed slider.
        // Callers (e.g. the tutorial controller) acquire a lock to freeze ticks
        // without changing the player's selected speed; releasing the last lock
        // resumes emission at the previously selected speed. Clamped at zero so
        // mismatched release calls cannot leave the counter negative.
        private int _pauseLockCount = 0;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Current tick number within the simulation.
        /// </summary>
        public int CurrentTick => _currentTick;

        /// <summary>
        /// Current day number (increments every _ticksPerDay ticks).
        /// </summary>
        public int CurrentDay => _currentDay;

        /// <summary>
        /// Current game speed multiplier.
        /// </summary>
        public float CurrentSpeed => _speedOptions[_currentSpeedIndex];

        /// <summary>
        /// Whether the game simulation is running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Number of ticks per in-game day.
        /// Used by MonthlyPaymentDayController to compute monthly income.
        /// </summary>
        public int TicksPerDay => _ticksPerDay;

        /// <summary>
        /// Whether the game is paused (speed = 0).
        /// </summary>
        public bool IsPaused => CurrentSpeed == 0f;

        /// <summary>
        /// True while at least one caller holds a pause lock via AcquirePause().
        /// Independent of the player's speed selection. Systems should query
        /// this before reacting to anything tick-driven; Update() already
        /// suppresses tick emission while held.
        /// </summary>
        public bool IsPauseLocked => _pauseLockCount > 0;

        /// <summary>
        /// Number of outstanding pause locks. Intended primarily for diagnostics
        /// and tests; production callers should pair Acquire/Release.
        /// </summary>
        public int PauseLockCount => _pauseLockCount;

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
            if (!_isRunning || IsPaused || IsPauseLocked)
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
        /// <summary>
        /// Restore day and tick from a saved state. Does not fire OnTick
        /// (no systems should process a phantom tick on restore).
        /// </summary>
        public void ApplyState(int day, int tick)
        {
            _currentDay = day;
            _currentTick = tick;
        }

        public void ResetTime()
        {
            _currentTick = 0;
            _currentDay = 0;
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
        /// Acquire a pause lock. Each call must be matched by a ReleasePause()
        /// call. While any locks are outstanding, Update() suppresses all tick
        /// emission regardless of speed selection. Used by the onboarding
        /// tutorial to freeze the simulation (rival, monthly cycle, accidents,
        /// income) during scripted beats without mutating player-facing speed.
        /// </summary>
        public void AcquirePause()
        {
            _pauseLockCount++;
        }

        /// <summary>
        /// Release one pause lock. Clamped to zero so an unmatched release
        /// leaves the counter at zero instead of going negative and silently
        /// corrupting the next acquire.
        /// </summary>
        public void ReleasePause()
        {
            if (_pauseLockCount > 0) _pauseLockCount--;
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

            // Check for end of day
            if (_currentTick % _ticksPerDay == 0)
            {
                _currentDay++;
                GameEvents.RaiseDayEnd(_currentDay);
            }
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
