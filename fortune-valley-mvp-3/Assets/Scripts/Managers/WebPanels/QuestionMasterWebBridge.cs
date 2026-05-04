using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Entities.WebPanels;

namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// QuestionMaster panel WebPanelBridge. While visible, drives a question
    /// session by raising QuestionStartRequested on Show, listening to the
    /// QuestionManager event stream, and forwarding state snapshots to the
    /// HTML iframe.
    ///
    /// JS-facing SendMessage targets (called via unityInstance.SendMessage):
    ///   RequestAnswer(string json)  - parses {idx}; idx==-1 is a JS-side timeout
    ///   RequestClose()              - fires OnHidePanelRequested
    ///
    /// Scene GameObject MUST be named exactly "QuestionMasterWebBridge" (see
    /// ObjectName const). Mismatch logs a warning at OnEnable. The matching
    /// JS const lives in the WebGL template's index.html.
    /// </summary>
    public class QuestionMasterWebBridge : WebPanelBridgeBase
    {
        public const string ObjectName = "QuestionMasterWebBridge";

        public override string PanelId => "questionMaster";
        public override string ExpectedObjectName => ObjectName;

        [Header("Config")]
        [SerializeField] private QuestionConfig _config;

        [Header("Reward Override")]
        [Tooltip("When enabled, the values below override the QuestionConfig reward tunables for sessions started from this panel.")]
        [SerializeField] private bool _overrideRewardConfig = false;
        [SerializeField] private float _baseReward = 25f;
        [SerializeField] private float _streakMultiplier = 1.15f;

        private readonly QuestionMasterPanelDTO _dto = new QuestionMasterPanelDTO();
        private readonly QuestionMasterWebBridgeLogic _logic = new QuestionMasterWebBridgeLogic();

        // Throttle timer-tick pushes to whole-second changes so JsonUtility
        // doesn't allocate every frame while the timer drains.
        private int _lastWholeSecond = -1;

        protected override void OnEnable()
        {
            base.OnEnable();
            _logic.Initialize(_config);
        }

        protected override void Subscribe()
        {
            _logic.ResetSession();
            _lastWholeSecond = -1;

            GameEvents.OnQuestionSessionStarted += HandleSessionStarted;
            GameEvents.OnQuestionPresented += HandlePresented;
            GameEvents.OnQuestionTimerTick += HandleTimerTick;
            GameEvents.OnQuestionAnswered += HandleAnswered;
            GameEvents.OnQuestionRewardGranted += HandleRewardGranted;
            GameEvents.OnQuestionSessionEnded += HandleSessionEnded;

            if (_overrideRewardConfig)
            {
                _logic.SetRewardConfigOverride(_baseReward, _streakMultiplier);
                GameEvents.RaiseQuestionRewardConfigOverrideRequested(_baseReward, _streakMultiplier);
            }
            else
            {
                _logic.ClearRewardConfigOverride();
            }

            // Kick the session. QuestionManager will raise SessionStarted +
            // Presented in response, which seed our cached state via the
            // handlers above before the initial PushNow snapshot fires.
            GameEvents.RaiseQuestionStartRequested();
        }

        protected override void Unsubscribe()
        {
            GameEvents.OnQuestionSessionStarted -= HandleSessionStarted;
            GameEvents.OnQuestionPresented -= HandlePresented;
            GameEvents.OnQuestionTimerTick -= HandleTimerTick;
            GameEvents.OnQuestionAnswered -= HandleAnswered;
            GameEvents.OnQuestionRewardGranted -= HandleRewardGranted;
            GameEvents.OnQuestionSessionEnded -= HandleSessionEnded;

            // Tell QuestionManager the panel is gone so the timer stops
            // ticking and the next open starts fresh.
            GameEvents.RaiseQuestionSessionEnded();
        }

        protected override string BuildPayloadJson()
        {
            if (!_logic.PopulateDTO(_dto)) return null;
            return JsonUtility.ToJson(_dto);
        }

        // ---------- Event handlers (cache state, mark dirty) ----------

        private void HandleSessionStarted()
        {
            _logic.OnSessionStarted();
            _lastWholeSecond = -1;
            MarkDirty();
        }

        private void HandlePresented(QuestionData q, int streak)
        {
            _logic.OnPresented(q, streak);
            _lastWholeSecond = -1;
            MarkDirty();
        }

        private void HandleTimerTick(float remaining, float total)
        {
            int whole = Mathf.CeilToInt(remaining);
            if (whole == _lastWholeSecond) return;
            _lastWholeSecond = whole;
            _logic.OnTimerTick(remaining);
            MarkDirty();
        }

        private void HandleAnswered(QuestionData question, bool correct, int chosenIndex, int correctIndex, int currentStreak)
        {
            _logic.OnAnswered(question, correct, chosenIndex, correctIndex, currentStreak);
            MarkDirty();
        }

        private void HandleRewardGranted(int amount, int newStreak)
        {
            _logic.OnRewardGranted(amount, newStreak);
            MarkDirty();
        }

        private void HandleSessionEnded()
        {
            _logic.OnSessionEnded();
            MarkDirty();
        }

        // ---------- SendMessage entry points (called from JS) ----------

        public void RequestAnswer(string json)
        {
            if (!TryParseAnswerIntent(json, out QuestionAnswerIntent intent)) return;
            GameEvents.RaiseQuestionAnswerSubmitted(intent.idx);
        }

        public void RequestClose()
        {
            GameEvents.RaiseHidePanelRequested(PanelType.QuestionMaster);
        }

        private bool TryParseAnswerIntent(string json, out QuestionAnswerIntent intent)
        {
            intent = null;
            if (string.IsNullOrEmpty(json))
            {
                Bridge.ShowError(PanelId, "Empty request.");
                return false;
            }

            try { intent = JsonUtility.FromJson<QuestionAnswerIntent>(json); }
            catch
            {
                Bridge.ShowError(PanelId, "Malformed request.");
                return false;
            }

            if (intent == null)
            {
                Bridge.ShowError(PanelId, "Invalid answer payload.");
                return false;
            }
            return true;
        }
    }
}
