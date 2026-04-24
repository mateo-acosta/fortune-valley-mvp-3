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

        [Tooltip("Camera used to project 3D world targets into screen space for the mask hole.")]
        [SerializeField] private Camera _screenRectCamera;
        [Tooltip("Fallback screen rect size (pixels) used when the target has no RectTransform and no Renderer/Collider bounds can be resolved.")]
        [SerializeField] private Vector2 _fallbackTargetSize = new Vector2(160f, 160f);

        private IKeyValueStore _keyValueStore;
        private TutorialSequenceMachine _machine;
        private TutorialStepKind _activeWaitKind = TutorialStepKind.Dialog;
        private bool _awaitingAdvanceTap;
        private bool _isActive;
        private bool _skipRevealed;
        private bool _lastEndWasSkip;
        // True when the current run came from ReplayTutorialService (settings
        // menu). On the player's very first tutorial pass, the Skip button
        // never appears -- they have to walk through the whole thing.
        private bool _isReplayRun;

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

        public void HandleTutorialStartRequested(bool isReplay = false)
        {
            Debug.Log($"[IntroTutorialController] HandleTutorialStartRequested fired. isReplay={isReplay} " +
                      $"active={_isActive} script={(_script == null ? "null" : _script.name)} " +
                      $"stepCount={(_script == null ? 0 : _script.StepCount)} " +
                      $"timeManager={(_timeManager == null ? "null" : "ok")} " +
                      $"guidance={(_guidanceController == null ? "null" : "ok")} " +
                      $"registry={(_targetRegistry == null ? "null" : "ok")}");

            if (_isActive) return;
            if (_machine == null) _machine = new TutorialSequenceMachine(_script);

            _isActive = true;
            _isReplayRun = isReplay;
            _skipRevealed = false;
            _lastEndWasSkip = false;

            if (_timeManager != null) _timeManager.AcquirePause();
            if (_guidanceController != null) _guidanceController.SetSuppressed(true);

            // Fire the existing OnBlockingPanelOpenChanged event so world-space
            // hover responders (BlockHoverController, LotSelector, etc.) treat
            // the tutorial like a modal popup and stop rendering hover canvases
            // while the tutorial is running.
            GameEvents.RaiseBlockingPanelOpenChanged(true);
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

            if (step.ClosePanelsOnEnter) GameEvents.RaiseTutorialClosePanelsRequested();
            GameEvents.RaiseTutorialWorldHoverAllowedChanged(step.AllowWorldHover);
            GameEvents.RaiseTutorialArrowOffsetChanged(step.ArrowScreenOffset);

            GameEvents.RaiseTutorialDialogChanged(step.DialogText, step.Pose);
            GameEvents.RaiseTutorialDialogVisibilityChanged(!step.HideDialog);

            if (step.Kind == TutorialStepKind.Dialog)
            {
                // A Dialog with a target gets the arrow + donut + Next button
                // (e.g. "here's the Investing tab"). Without a target it's the
                // classic full-dim Dialog. The KeepFullDim flag forces full
                // dim even when a target is set so the target stays dimmed
                // (for steps where clicking the target should NOT advance).
                if (step.TargetKind != TutorialTargetKind.None && _targetRegistry != null)
                {
                    Transform dialogTarget = _targetRegistry.GetTarget(step.TargetKind);
                    if (dialogTarget != null)
                    {
                        GameEvents.RaiseTutorialHighlightTarget(dialogTarget);
                        if (step.KeepFullDim)
                        {
                            GameEvents.RaiseTutorialDialogModeEntered();
                        }
                        else
                        {
                            Transform dialogMaskTarget = dialogTarget;
                            if (step.MaskTargetKind != TutorialTargetKind.None)
                            {
                                var override_ = _targetRegistry.GetTarget(step.MaskTargetKind);
                                if (override_ != null) dialogMaskTarget = override_;
                            }
                            GameEvents.RaiseTutorialDialogWithHighlightEntered(
                                ExpandRect(ResolveScreenRect(dialogMaskTarget), step.MaskPaddingExtra));
                        }
                        _awaitingAdvanceTap = true;
                        _activeWaitKind = TutorialStepKind.Dialog;
                        return;
                    }
                }

                GameEvents.RaiseTutorialHighlightTarget(null);
                GameEvents.RaiseTutorialDialogModeEntered();
                _awaitingAdvanceTap = true;
                _activeWaitKind = TutorialStepKind.Dialog;
                return;
            }

            // WaitForX step: resolve the target and subscribe to the matching event.
            _awaitingAdvanceTap = false;
            Transform target = _targetRegistry != null ? _targetRegistry.GetTarget(step.TargetKind) : null;
            // Mask hole defaults to the arrow target; in-panel steps override
            // it to the panel root so the whole panel stays bright while the
            // arrow points at one element inside.
            Transform maskTarget = target;
            if (step.MaskTargetKind != TutorialTargetKind.None && _targetRegistry != null)
            {
                var override_ = _targetRegistry.GetTarget(step.MaskTargetKind);
                if (override_ != null) maskTarget = override_;
            }
            Debug.Log($"[IntroTutorialController] WaitFor step entered. kind={step.Kind} " +
                      $"targetKind={step.TargetKind} maskTargetKind={step.MaskTargetKind} hideDialog={step.HideDialog} " +
                      $"target={(target == null ? "null" : target.name + " at " + target.position)} " +
                      $"maskTarget={(maskTarget == null ? "null" : maskTarget.name)}");
            GameEvents.RaiseTutorialHighlightTarget(target);
            GameEvents.RaiseTutorialWaitModeEntered(ExpandRect(ResolveScreenRect(maskTarget), step.MaskPaddingExtra));
            SubscribeWaitEvent(step.Kind);
        }

        /// <summary>
        /// Expand a screen-space rect by the per-step extra padding. extra.x
        /// adds to the left and right; extra.y adds to top and bottom.
        /// </summary>
        private static Rect ExpandRect(Rect r, Vector2 extra)
        {
            if (extra == Vector2.zero) return r;
            return Rect.MinMaxRect(r.xMin - extra.x, r.yMin - extra.y, r.xMax + extra.x, r.yMax + extra.y);
        }

        /// <summary>
        /// Returns the target's screen-space Rect (pixels) so the mask
        /// overlay can cut a donut hole around it. Handles both UI
        /// RectTransforms and 3D world Renderers/Colliders. Falls back to
        /// a small square centered on the screen if the target has no
        /// usable bounds source.
        /// </summary>
        private Rect ResolveScreenRect(Transform target)
        {
            if (target == null) return ZeroCenteredFallback();

            // Authored corner anchors take precedence -- they describe the
            // true footprint (e.g. a block's lot edges) better than the
            // parent transform or whichever renderer happens to live first
            // in the child list.
            var anchors = target.GetComponent<TutorialWorldBoundsAnchors>();
            if (anchors != null && anchors.TryGetBounds(out var anchored))
            {
                return BoundsToScreenRect(anchored);
            }

            var rt = target as RectTransform;
            if (rt != null)
            {
                return RectTransformToScreenRect(rt);
            }

            var renderer = target.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                return BoundsToScreenRect(renderer.bounds);
            }

            var collider = target.GetComponentInChildren<Collider>();
            if (collider != null)
            {
                return BoundsToScreenRect(collider.bounds);
            }

            // No bounds source: project the pivot and pad by fallback size.
            Camera cam = _screenRectCamera != null ? _screenRectCamera : Camera.main;
            if (cam == null) return ZeroCenteredFallback();
            Vector3 sp = cam.WorldToScreenPoint(target.position);
            return new Rect(
                sp.x - _fallbackTargetSize.x * 0.5f,
                sp.y - _fallbackTargetSize.y * 0.5f,
                _fallbackTargetSize.x,
                _fallbackTargetSize.y);
        }

        private static Rect RectTransformToScreenRect(RectTransform rt)
        {
            // Screen Space Overlay canvas: world corners are already in screen pixels.
            // Other canvas modes: convert via the canvas's render camera.
            var canvas = rt.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                Vector2 sp = cam != null
                    ? (Vector2)cam.WorldToScreenPoint(corners[i])
                    : (Vector2)corners[i];
                if (sp.x < minX) minX = sp.x;
                if (sp.x > maxX) maxX = sp.x;
                if (sp.y < minY) minY = sp.y;
                if (sp.y > maxY) maxY = sp.y;
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private Rect BoundsToScreenRect(Bounds bounds)
        {
            Camera cam = _screenRectCamera != null ? _screenRectCamera : Camera.main;
            if (cam == null) return ZeroCenteredFallback();

            // Project all 8 corners of the world-space AABB; take the screen
            // bounding box. Handles rotated/tilted cameras correctly.
            var center = bounds.center;
            var ext = bounds.extents;
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int dx = -1; dx <= 1; dx += 2)
            for (int dy = -1; dy <= 1; dy += 2)
            for (int dz = -1; dz <= 1; dz += 2)
            {
                Vector3 wp = center + new Vector3(ext.x * dx, ext.y * dy, ext.z * dz);
                Vector3 sp = cam.WorldToScreenPoint(wp);
                if (sp.x < minX) minX = sp.x;
                if (sp.x > maxX) maxX = sp.x;
                if (sp.y < minY) minY = sp.y;
                if (sp.y > maxY) maxY = sp.y;
            }
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        private Rect ZeroCenteredFallback()
        {
            float w = Screen.width;
            float h = Screen.height;
            return new Rect(
                w * 0.5f - _fallbackTargetSize.x * 0.5f,
                h * 0.5f - _fallbackTargetSize.y * 0.5f,
                _fallbackTargetSize.x,
                _fallbackTargetSize.y);
        }

        private void AdvanceStep()
        {
            ExitCurrentStep();

            if (_machine == null) { EndTutorial(); return; }
            _machine.Advance();

            // Skip button only appears on REPLAY runs. On the player's very
            // first pass through the tutorial they have to walk through the
            // whole thing -- it's how the learning hooks land.
            if (_isReplayRun && !_skipRevealed && _machine.CurrentIndex > 0)
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
            // Balance the BlockingPanelOpenChanged(true) we fired at start so
            // BlockHoverController's internal counter returns to zero.
            GameEvents.RaiseBlockingPanelOpenChanged(false);
            // Reset the world-hover gate so it doesn't leak past tutorial end.
            GameEvents.RaiseTutorialWorldHoverAllowedChanged(false);

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
                    GameEvents.OnPanelOpened += HandlePanelOpened;
                    return;
                case TutorialStepKind.WaitForLoanShopTabSelected:
                    GameEvents.OnLoanShopTabSelected += HandleLoanShopTabSelected;
                    return;
                case TutorialStepKind.WaitForLotInfoOpened:
                    GameEvents.OnLotInfoRequested += HandleLotInfoRequested;
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
                case TutorialStepKind.WaitForLoanPanelOpened:
                    GameEvents.OnPanelOpened -= HandlePanelOpened;
                    break;
                case TutorialStepKind.WaitForLoanShopTabSelected:
                    GameEvents.OnLoanShopTabSelected -= HandleLoanShopTabSelected;
                    break;
                case TutorialStepKind.WaitForLotInfoOpened:
                    GameEvents.OnLotInfoRequested -= HandleLotInfoRequested;
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

        private void HandlePanelOpened(PanelType panelType)
        {
            // The only WaitForX step that listens on OnPanelOpened is
            // WaitForLoanPanelOpened. If the player opens a different panel
            // (Insurance, Portfolio, etc.), do nothing.
            if (_activeWaitKind != TutorialStepKind.WaitForLoanPanelOpened) return;
            if (panelType != PanelType.Loan) return;
            AdvanceStep();
        }

        private void HandleLoanShopTabSelected() => AdvanceStep();

        private void HandleLotInfoRequested(string lotId) => AdvanceStep();
    }
}
