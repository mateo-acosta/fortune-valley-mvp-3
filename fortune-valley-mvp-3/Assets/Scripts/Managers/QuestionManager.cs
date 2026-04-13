using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Core.Questions;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Managers
{
    /// <summary>
    /// Runtime wrapper around QuestionSession + StreakRewardCalculator. Drives the
    /// QuestionMaster timer and routes rewards through CurrencyManager. Ticks on
    /// Time.unscaledDeltaTime so the panel behaves independently of game pause state.
    /// </summary>
    public class QuestionManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private QuestionConfig _config;

        [Header("Dependencies")]
        [SerializeField] private CurrencyManager _currencyManager;

        [Header("Debug")]
        [SerializeField] private bool _logRewards = false;

        private readonly QuestionSession _session = new QuestionSession();
        private QuestionSessionPhase _phase = QuestionSessionPhase.Idle;
        private float _timer;

        public bool IsSessionActive => _phase != QuestionSessionPhase.Idle;
        public int CurrentStreak => _session.Streak;

        private void Awake()
        {
            var bank = QuestionBankLoader.Load();
            _session.Start(bank);
        }

        private void OnEnable()
        {
            GameEvents.OnQuestionSessionEnded += HandleSessionEndedSignal;
            GameEvents.OnQuestionStartRequested += StartSession;
            GameEvents.OnQuestionAnswerSubmitted += SubmitAnswer;
        }

        private void OnDisable()
        {
            GameEvents.OnQuestionSessionEnded -= HandleSessionEndedSignal;
            GameEvents.OnQuestionStartRequested -= StartSession;
            GameEvents.OnQuestionAnswerSubmitted -= SubmitAnswer;
        }

        private void HandleSessionEndedSignal()
        {
            // Others may raise the end signal (close button). Mirror internal state.
            if (_phase != QuestionSessionPhase.Idle)
            {
                _phase = QuestionSessionPhase.Idle;
                _timer = 0f;
            }
        }

        /// <summary>
        /// Begin or restart a session. Streak resets per Issue spec.
        /// Safe to call while another session is in progress (restarts).
        /// </summary>
        public void StartSession()
        {
            if (_session.DeckCount == 0)
            {
                Debug.LogWarning("[QuestionManager] Cannot start session -- question bank is empty.");
                return;
            }
            _session.ResetStreakOnly();
            GameEvents.RaiseQuestionSessionStarted();
            PresentNext();
        }

        /// <summary>
        /// End the current session early. Idempotent.
        /// </summary>
        public void EndSession()
        {
            if (_phase == QuestionSessionPhase.Idle) return;
            _phase = QuestionSessionPhase.Idle;
            _timer = 0f;
            GameEvents.RaiseQuestionSessionEnded();
        }

        /// <summary>
        /// Player submits an answer. Ignored unless currently Asking.
        /// </summary>
        public void SubmitAnswer(int chosenIndex)
        {
            if (_phase != QuestionSessionPhase.Asking) return;
            ResolveAnswer(chosenIndex);
        }

        private void Update()
        {
            if (_phase == QuestionSessionPhase.Idle) return;

            _timer -= Time.unscaledDeltaTime;

            if (_phase == QuestionSessionPhase.Asking)
            {
                // Broadcast every frame so the fill bar animates smoothly. fillAmount assignment
                // is alloc-free; text readouts filter to whole-second changes in the UI layer.
                GameEvents.RaiseQuestionTimerTick(Mathf.Max(0f, _timer), _config.QuestionTimeSeconds);

                if (_timer <= 0f)
                {
                    ResolveAnswer(-1); // timeout
                }
            }
            else if (_phase == QuestionSessionPhase.Revealing)
            {
                if (_timer <= 0f)
                {
                    PresentNext();
                }
            }
        }

        private void PresentNext()
        {
            var q = _session.Next();
            if (q == null)
            {
                EndSession();
                return;
            }
            _phase = QuestionSessionPhase.Asking;
            _timer = _config.QuestionTimeSeconds;
            GameEvents.RaiseQuestionPresented(q, _session.Streak);
            // Kick a zero-time broadcast so UI can prime its timer readout.
            GameEvents.RaiseQuestionTimerTick(_timer, _config.QuestionTimeSeconds);
        }

        private void ResolveAnswer(int chosenIndex)
        {
            int correctIndex = _session.Current != null ? _session.Current.correctIndex : -1;
            bool correct = _session.Submit(chosenIndex);

            GameEvents.RaiseQuestionAnswered(correct, chosenIndex, correctIndex);

            if (correct)
            {
                GrantStreakReward();
            }

            _phase = QuestionSessionPhase.Revealing;
            _timer = _config.OverlayDurationSeconds;
        }

        private void GrantStreakReward()
        {
            // Streak is now 1-based: after 1st correct answer, Streak == 1. First reward uses index 0.
            int streakIndex = _session.Streak - 1;
            int reward = StreakRewardCalculator.RewardForStreak(
                streakIndex,
                _config.BaseReward,
                _config.StreakMultiplier,
                _config.RewardRoundingStep);

            if (reward > 0 && _currencyManager != null)
            {
                _currencyManager.AddToChecking(reward, "Question reward");
            }

            if (_logRewards)
            {
                Debug.Log($"[QuestionManager] Correct -- streak {_session.Streak}, reward ${reward}");
            }

            GameEvents.RaiseQuestionRewardGranted(reward, _session.Streak);
        }
    }
}
