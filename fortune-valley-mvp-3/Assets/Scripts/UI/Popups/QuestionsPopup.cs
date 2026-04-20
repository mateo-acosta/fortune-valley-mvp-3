using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.UI.Components;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// QuestionMaster popup. Reads lifecycle events from QuestionManager via GameEvents and
    /// renders title, timer bar, prompt, 4 answer buttons, streak text, and correct/incorrect overlays.
    /// Pushes player actions back as GameEvents intents.
    /// </summary>
    public class QuestionsPopup : UIPopup
    {
        [Header("Title / Close")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Button _closeButton;

        [Header("Timer")]
        [SerializeField] private Image _timerBarFill;
        [SerializeField] private TextMeshProUGUI _timerSecondsText;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI _questionText;
        [SerializeField] private TextMeshProUGUI _streakText;
        [SerializeField] private AnswerButton[] _answerButtons;

        [Header("Feedback Overlays")]
        [SerializeField] private GameObject _correctOverlay;
        [SerializeField] private GameObject _incorrectOverlay;

        [Header("Overlay Texts (optional)")]
        [Tooltip("TMP text inside CorrectPanelOverlay that shows the reward earned this question")]
        [SerializeField] private TextMeshProUGUI _correctRewardText;
        [Tooltip("TMP text inside CorrectPanelOverlay that shows the updated streak")]
        [SerializeField] private TextMeshProUGUI _correctStreakText;
        [Tooltip("TMP text inside IncorrectPanelOverlay that shows the streak (usually 0 after a miss)")]
        [SerializeField] private TextMeshProUGUI _incorrectStreakText;

        [Header("Copy")]
        [SerializeField] private string _titleFormat = "QuestionMaster";
        [SerializeField] private string _streakFormat = "Streak: {0}";
        [SerializeField] private string _rewardFormat = "+${0:N0}";

        private int _lastWholeSecond = -1;
        private bool _inputLocked;

        private void Awake()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(HandleCloseClicked);

            if (_answerButtons != null)
            {
                for (int i = 0; i < _answerButtons.Length; i++)
                {
                    if (_answerButtons[i] != null)
                    {
                        _answerButtons[i].OnClicked += HandleAnswerClicked;
                    }
                }
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            SubscribeSessionEvents();
            HideOverlays();
            if (_titleText != null) _titleText.text = _titleFormat;
            UpdateStreakText(0);
            _inputLocked = false;
            GameEvents.RaiseQuestionStartRequested();
        }

        protected override void OnHide()
        {
            base.OnHide();
            UnsubscribeSessionEvents();
            GameEvents.RaiseQuestionSessionEnded();
        }

        private void HandleCloseClicked()
        {
            OnCancelClicked();
        }

        private void HandleAnswerClicked(AnswerButton btn)
        {
            if (_inputLocked || btn == null) return;
            _inputLocked = true;
            GameEvents.RaiseQuestionAnswerSubmitted(btn.AnswerIndex);
        }

        private void SubscribeSessionEvents()
        {
            GameEvents.OnQuestionPresented += HandleQuestionPresented;
            GameEvents.OnQuestionTimerTick += HandleTimerTick;
            GameEvents.OnQuestionAnswered += HandleQuestionAnswered;
            GameEvents.OnQuestionRewardGranted += HandleRewardGranted;
        }

        private void UnsubscribeSessionEvents()
        {
            GameEvents.OnQuestionPresented -= HandleQuestionPresented;
            GameEvents.OnQuestionTimerTick -= HandleTimerTick;
            GameEvents.OnQuestionAnswered -= HandleQuestionAnswered;
            GameEvents.OnQuestionRewardGranted -= HandleRewardGranted;
        }

        private void HandleQuestionPresented(QuestionData q, int streak)
        {
            HideOverlays();
            _inputLocked = false;
            _lastWholeSecond = -1;

            if (_questionText != null) _questionText.text = q.prompt;
            UpdateStreakText(streak);

            int count = Mathf.Min(_answerButtons?.Length ?? 0, q.choices?.Length ?? 0);
            for (int i = 0; i < count; i++)
            {
                if (_answerButtons[i] == null) continue;
                _answerButtons[i].SetContent(i, q.choices[i]);
                _answerButtons[i].SetInteractable(true);
            }
        }

        private void HandleTimerTick(float remaining, float total)
        {
            // Fill bar (cheap, alloc-free).
            if (_timerBarFill != null && total > 0f)
            {
                _timerBarFill.fillAmount = Mathf.Clamp01(remaining / total);
            }

            // Numeric readout: only format on whole-second changes to avoid per-frame GC.
            if (_timerSecondsText != null)
            {
                int whole = Mathf.CeilToInt(remaining);
                if (whole != _lastWholeSecond)
                {
                    _lastWholeSecond = whole;
                    _timerSecondsText.text = whole.ToString();
                }
            }
        }

        private void HandleQuestionAnswered(QuestionData question, bool correct, int chosenIndex, int correctIndex, int currentStreak)
        {
            // Lock all buttons; paint the chosen and correct ones.
            if (_answerButtons != null)
            {
                for (int i = 0; i < _answerButtons.Length; i++)
                {
                    if (_answerButtons[i] == null) continue;
                    _answerButtons[i].SetInteractable(false);
                    if (i == correctIndex)
                    {
                        _answerButtons[i].SetCorrect();
                    }
                    else if (i == chosenIndex)
                    {
                        _answerButtons[i].SetWrong();
                    }
                }
            }

            if (correct && _correctOverlay != null) _correctOverlay.SetActive(true);
            if (!correct)
            {
                if (_incorrectOverlay != null) _incorrectOverlay.SetActive(true);
                if (_incorrectStreakText != null)
                {
                    _incorrectStreakText.text = string.Format(_streakFormat, 0);
                }
            }
        }

        private void HandleRewardGranted(int amount, int newStreak)
        {
            UpdateStreakText(newStreak);
            if (_correctRewardText != null)
            {
                _correctRewardText.text = string.Format(_rewardFormat, amount);
            }
            if (_correctStreakText != null)
            {
                _correctStreakText.text = string.Format(_streakFormat, newStreak);
            }
        }

        private void UpdateStreakText(int streak)
        {
            if (_streakText != null)
            {
                _streakText.text = string.Format(_streakFormat, streak);
            }
        }

        private void HideOverlays()
        {
            if (_correctOverlay != null) _correctOverlay.SetActive(false);
            if (_incorrectOverlay != null) _incorrectOverlay.SetActive(false);
        }
    }
}
