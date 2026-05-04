using FortuneValley.Core;
using FortuneValley.Core.Questions;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Entities.WebPanels;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// Cached event-driven state for the QuestionMaster panel + DTO population.
    /// QuestionManager exposes its per-question payload only via GameEvents
    /// (OnQuestionPresented / OnQuestionAnswered / OnQuestionRewardGranted /
    /// OnQuestionTimerTick), so this logic class records the latest event
    /// state and the bridge serialises it on push.
    ///
    /// Pure C# so EditMode tests can exercise the state machine without a
    /// scene or PlayMode session.
    /// </summary>
    public class QuestionMasterWebBridgeLogic : WebPanelBridgeLogic<QuestionMasterPanelDTO>
    {
        public const string PhaseIdle = "idle";
        public const string PhaseAsking = "asking";
        public const string PhaseRevealing = "revealing";

        private QuestionConfig _config;

        private string _phase = PhaseIdle;
        private int _streak;
        private float _timeRemainingSeconds;

        private string _questionId;
        private string _category;
        private string _prompt;
        private string[] _choices;

        private int _correctIndex = -1;
        private int _chosenIndex = -1;
        private bool _wasCorrect;
        private int _rewardEarned;
        private string _explanation;
        private bool _wasTimeout;

        private bool _hasRewardOverride;
        private float _overrideBaseReward;
        private float _overrideStreakMultiplier;

        public string Phase => _phase;

        public void Initialize(QuestionConfig config)
        {
            _config = config;
        }

        public void ResetSession()
        {
            _phase = PhaseIdle;
            _streak = 0;
            _timeRemainingSeconds = 0f;
            _questionId = null;
            _category = null;
            _prompt = null;
            _choices = null;
            _correctIndex = -1;
            _chosenIndex = -1;
            _wasCorrect = false;
            _rewardEarned = 0;
            _explanation = null;
            _wasTimeout = false;
        }

        public void OnSessionStarted()
        {
            _phase = PhaseAsking;
            _streak = 0;
            _correctIndex = -1;
            _chosenIndex = -1;
            _wasCorrect = false;
            _rewardEarned = 0;
            _wasTimeout = false;
        }

        public void OnPresented(QuestionData q, int streak)
        {
            _phase = PhaseAsking;
            _streak = streak;
            if (q != null)
            {
                _questionId = q.id;
                _category = q.category;
                _prompt = q.prompt;
                _choices = q.choices;
                _explanation = q.explanation;
            }
            _correctIndex = -1;
            _chosenIndex = -1;
            _wasCorrect = false;
            _rewardEarned = 0;
            _wasTimeout = false;
            _timeRemainingSeconds = _config != null ? _config.QuestionTimeSeconds : 0f;
        }

        public void OnTimerTick(float remaining)
        {
            _timeRemainingSeconds = remaining;
        }

        public void OnAnswered(QuestionData question, bool correct, int chosenIndex, int correctIndex, int currentStreak)
        {
            _phase = PhaseRevealing;
            _wasCorrect = correct;
            _chosenIndex = chosenIndex;
            _correctIndex = correctIndex;
            _streak = currentStreak;
            _wasTimeout = chosenIndex < 0;
            if (question != null)
            {
                _explanation = question.explanation;
            }
            // Reward is filled by OnRewardGranted on correct answers; reset
            // so a wrong answer's payload doesn't carry a stale reward value.
            if (!correct) _rewardEarned = 0;
        }

        public void OnRewardGranted(int amount, int newStreak)
        {
            _rewardEarned = amount;
            _streak = newStreak;
        }

        public void OnSessionEnded()
        {
            _phase = PhaseIdle;
        }

        public void SetRewardConfigOverride(float baseReward, float streakMultiplier)
        {
            _hasRewardOverride = true;
            _overrideBaseReward = baseReward;
            _overrideStreakMultiplier = streakMultiplier;
        }

        public void ClearRewardConfigOverride()
        {
            _hasRewardOverride = false;
        }

        public override bool PopulateDTO(QuestionMasterPanelDTO target)
        {
            if (target == null) return false;
            if (_config == null) return false;

            target.phase = _phase;
            target.streak = _streak;
            target.timeLimitSeconds = _config.QuestionTimeSeconds;
            target.timeRemainingSeconds = _timeRemainingSeconds;
            target.nextReward = ComputeNextReward();

            target.questionId = _questionId;
            target.category = _category;
            target.prompt = _prompt;
            target.choices = _choices;

            target.correctIndex = _correctIndex;
            target.chosenIndex = _chosenIndex;
            target.wasCorrect = _wasCorrect;
            target.rewardEarned = _rewardEarned;
            target.explanation = _explanation;
            target.wasTimeout = _wasTimeout;
            return true;
        }

        private int ComputeNextReward()
        {
            if (_config == null) return 0;
            float baseReward = _hasRewardOverride ? _overrideBaseReward : _config.BaseReward;
            float multiplier = _hasRewardOverride ? _overrideStreakMultiplier : _config.StreakMultiplier;
            // Streak is 1-based after a correct answer; the index for the
            // upcoming reward is just _streak (0 -> first reward, 1 -> second, ...).
            return StreakRewardCalculator.RewardForStreak(_streak, baseReward, multiplier, _config.RewardRoundingStep);
        }
    }
}
