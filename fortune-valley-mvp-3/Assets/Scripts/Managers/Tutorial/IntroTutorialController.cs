using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Tutorial;
using FortuneValley.Managers.Notifications;

// NOTE: PlayerPrefs is used as a local fallback flag so a brief network
// outage on completion does not cause the tutorial to re-run on reload.
// The Rails API retry queue on APIClient eventually persists the flag.

namespace FortuneValley.Managers.Tutorial
{
    /// <summary>
    /// State machine driver for the scripted onboarding tutorial.
    ///
    /// Responsibilities:
    /// - On OnTutorialStartRequested, acquire the TimeManager pause lock,
    ///   suppress the GuidanceController, show the overlay, block input,
    ///   and walk the IntroScriptSO step by step.
    /// - For Dialog steps, advance when the player taps
    ///   (OnTutorialAdvanceRequested). For WaitForX steps, subscribe to the
    ///   matching GameEvent and advance when it fires.
    /// - At any point after the first dialog scene, OnTutorialSkipRequested
    ///   short-circuits to completion.
    /// - On completion, release the pause, unsuppress guidance, hide the
    ///   overlay / highlight / blocker, and raise OnTutorialComplete so
    ///   GameFlowController can resume the normal countdown path.
    ///
    /// UI components (TutorialOverlayUI, TutorialHighlight, InputBlocker)
    /// subscribe to the corresponding OnTutorialXxx events and drive their
    /// own state, so this controller never references UI types directly
    /// (Managers layer cannot see UI, and must not).
    /// </summary>
    public class IntroTutorialController : MonoBehaviour
    {
        public const string PlayerPrefsKeyPrefix = "FV_TutorialCompleted_";

        [SerializeField] private TimeManager _timeManager;
        [SerializeField] private GuidanceController _guidanceController;
        [SerializeField] private TutorialTargetRegistry _targetRegistry;
        [SerializeField] private IntroScriptSO _script;
        [SerializeField] private PlayerStateAccessor _playerStateAccessor;
        [SerializeField] private APIClient _apiClient;

        private IKeyValueStore _keyValueStore;
        private TutorialSequenceMachine _machine;
        private TutorialStepKind _activeWaitKind = TutorialStepKind.Dialog;
        private bool _awaitingAdvanceTap;
        private bool _isActive;
        private bool _skipRevealed;
        private bool _lastEndWasSkip;

        public bool LastEndWasSkip => _lastEndWasSkip;

        public bool IsActive => _isActive;
        public int CurrentStepIndex => _machine != null ? _machine.CurrentIndex : -1;
        public TutorialStepSO CurrentStep => _machine != null ? _machine.CurrentStep : null;

        private void Awake()
        {
            _machine = new TutorialSequenceMachine(_script);
        }

        private void OnEnable()
        {
            GameEvents.OnTutorialStartRequested += HandleTutorialStartRequested;
            GameEvents.OnTutorialAdvanceRequested += HandleAdvanceRequested;
            GameEvents.OnTutorialSkipRequested += HandleSkipRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnTutorialStartRequested -= HandleTutorialStartRequested;
            GameEvents.OnTutorialAdvanceRequested -= HandleAdvanceRequested;
            GameEvents.OnTutorialSkipRequested -= HandleSkipRequested;
            UnsubscribeWaitEvent();
        }

        /// <summary>
        /// Test hook: swap in explicit dependencies without scene wiring.
        /// </summary>
        public void Initialize(
            TimeManager timeManager,
            GuidanceController guidanceController,
            TutorialTargetRegistry targetRegistry,
            IntroScriptSO script,
            PlayerStateAccessor playerStateAccessor = null,
            APIClient apiClient = null,
            IKeyValueStore keyValueStore = null)
        {
            _timeManager = timeManager;
            _guidanceController = guidanceController;
            _targetRegistry = targetRegistry;
            _script = script;
            _playerStateAccessor = playerStateAccessor;
            _apiClient = apiClient;
            _keyValueStore = keyValueStore;
            _machine = new TutorialSequenceMachine(script);
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ENTRY POINTS
        // ═══════════════════════════════════════════════════════════════

        public void HandleTutorialStartRequested()
        {
            if (_isActive) return;
            if (_machine == null) _machine = new TutorialSequenceMachine(_script);

            _isActive = true;
            _skipRevealed = false;
            _lastEndWasSkip = false;

            if (_timeManager != null) _timeManager.AcquirePause();
            if (_guidanceController != null) _guidanceController.SetSuppressed(true);

            GameEvents.RaiseTutorialInputBlockChanged(true);
            GameEvents.RaiseTutorialOverlayVisibilityChanged(true);

            _machine.Start();

            if (_machine.IsComplete)
            {
                // Empty script → finish immediately.
                EndTutorial();
                return;
            }

            BeginStep(_machine.CurrentStep);
        }

        public void HandleAdvanceRequested()
        {
            if (!_isActive) return;
            if (!_awaitingAdvanceTap) return;
            AdvanceStep();
        }

        public void HandleSkipRequested()
        {
            if (!_isActive) return;
            // Skip is only valid after the first dialog scene has completed.
            if (!_skipRevealed) return;
            if (_machine == null) return;

            _lastEndWasSkip = true;
            _machine.JumpToEnd();
            EndTutorial();
        }

        // ═══════════════════════════════════════════════════════════════
        // STEP LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void BeginStep(TutorialStepSO step)
        {
            if (step == null)
            {
                EndTutorial();
                return;
            }

            GameEvents.RaiseTutorialDialogChanged(step.DialogText, step.Pose);

            if (step.Kind == TutorialStepKind.Dialog)
            {
                GameEvents.RaiseTutorialHighlightTarget(null);
                _awaitingAdvanceTap = true;
                _activeWaitKind = TutorialStepKind.Dialog;
                return;
            }

            // WaitForX step: resolve the target and subscribe to the matching event.
            _awaitingAdvanceTap = false;
            Transform target = _targetRegistry != null ? _targetRegistry.GetTarget(step.TargetKind) : null;
            GameEvents.RaiseTutorialHighlightTarget(target);
            SubscribeWaitEvent(step.Kind);
        }

        private void AdvanceStep()
        {
            ExitCurrentStep();

            if (_machine == null) { EndTutorial(); return; }
            _machine.Advance();

            // After advancing OUT of step 0 for the first time, reveal the Skip button.
            // Revealing happens once per run so re-entering dialog steps later does not toggle it off.
            if (!_skipRevealed && _machine.CurrentIndex > 0)
            {
                _skipRevealed = true;
                GameEvents.RaiseTutorialSkipRevealed();
            }

            if (_machine.IsComplete)
            {
                EndTutorial();
                return;
            }

            BeginStep(_machine.CurrentStep);
        }

        private void ExitCurrentStep()
        {
            _awaitingAdvanceTap = false;
            UnsubscribeWaitEvent();
            GameEvents.RaiseTutorialHighlightTarget(null);
        }

        private void EndTutorial()
        {
            ExitCurrentStep();

            PersistCompletionFlag();

            GameEvents.RaiseTutorialInputBlockChanged(false);
            GameEvents.RaiseTutorialOverlayVisibilityChanged(false);

            if (_timeManager != null) _timeManager.ReleasePause();
            if (_guidanceController != null) _guidanceController.SetSuppressed(false);

            _isActive = false;
            GameEvents.RaiseTutorialComplete();
        }

        /// <summary>
        /// Writes the tutorial_completed flag to PlayerPrefs first (so a
        /// network failure on SaveState does not cause the tutorial to
        /// re-run on reload), mutates the cached player state, and then
        /// calls APIClient.SaveState. Any of the three steps is optional:
        /// missing PlayerStateAccessor / APIClient / KV store skips that
        /// half of persistence without blocking the others.
        /// </summary>
        private void PersistCompletionFlag()
        {
            string gameMode = "homebase";
            GamePlayerStateDTO state = _playerStateAccessor != null ? _playerStateAccessor.Current : null;
            if (state != null)
            {
                state.tutorial_completed = true;
                if (!string.IsNullOrEmpty(state.game_mode)) gameMode = state.game_mode;
                if (_apiClient != null) _apiClient.SaveState(state);
            }

            var store = _keyValueStore ?? PlayerPrefsStore;
            store.SetInt(PlayerPrefsKeyPrefix + gameMode, 1);
            store.Save();
        }

        private static IKeyValueStore _playerPrefsStore;
        private static IKeyValueStore PlayerPrefsStore =>
            _playerPrefsStore ??= new PlayerPrefsKeyValueStore();

        // ═══════════════════════════════════════════════════════════════
        // WAIT-EVENT SUBSCRIPTION PER STEP KIND
        // ═══════════════════════════════════════════════════════════════

        private void SubscribeWaitEvent(TutorialStepKind kind)
        {
            _activeWaitKind = kind;
            switch (kind)
            {
                case TutorialStepKind.WaitForRestaurantTap:
                    GameEvents.OnRestaurantSelected += HandleRestaurantSelected;
                    return;
                case TutorialStepKind.WaitForIncomeCollected:
                    GameEvents.OnIncomeGenerated += HandleIncomeGenerated;
                    return;
                case TutorialStepKind.WaitForLoanTaken:
                    GameEvents.OnLoanOriginated += HandleLoanOriginated;
                    return;
                case TutorialStepKind.WaitForLotPurchased:
                    GameEvents.OnLotPurchased += HandleLotPurchased;
                    return;
                case TutorialStepKind.WaitForRestaurantUpgraded:
                    GameEvents.OnRestaurantUpgraded += HandleRestaurantUpgraded;
                    return;
                case TutorialStepKind.WaitForLoanPanelOpened:
                    // Panel-open event not yet wired. Behaves like a Dialog step:
                    // the player taps to advance. This branch can be replaced
                    // once UIManager.ShowPanel raises a typed OnPanelOpened event.
                    _awaitingAdvanceTap = true;
                    _activeWaitKind = TutorialStepKind.Dialog;
                    return;
            }
        }

        private void UnsubscribeWaitEvent()
        {
            switch (_activeWaitKind)
            {
                case TutorialStepKind.WaitForRestaurantTap:
                    GameEvents.OnRestaurantSelected -= HandleRestaurantSelected;
                    break;
                case TutorialStepKind.WaitForIncomeCollected:
                    GameEvents.OnIncomeGenerated -= HandleIncomeGenerated;
                    break;
                case TutorialStepKind.WaitForLoanTaken:
                    GameEvents.OnLoanOriginated -= HandleLoanOriginated;
                    break;
                case TutorialStepKind.WaitForLotPurchased:
                    GameEvents.OnLotPurchased -= HandleLotPurchased;
                    break;
                case TutorialStepKind.WaitForRestaurantUpgraded:
                    GameEvents.OnRestaurantUpgraded -= HandleRestaurantUpgraded;
                    break;
            }
            _activeWaitKind = TutorialStepKind.Dialog;
        }

        // ═══════════════════════════════════════════════════════════════
        // WAIT-EVENT HANDLERS (each advances the sequence)
        // ═══════════════════════════════════════════════════════════════

        private void HandleRestaurantSelected() => AdvanceStep();

        private void HandleIncomeGenerated(float amount, string source) => AdvanceStep();

        private void HandleLoanOriginated(ActiveLoan loan) => AdvanceStep();

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (owner != Owner.Player) return;
            AdvanceStep();
        }

        private void HandleRestaurantUpgraded(int newLevel) => AdvanceStep();
    }
}
