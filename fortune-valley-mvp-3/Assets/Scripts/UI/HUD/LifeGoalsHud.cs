using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain;
using FortuneValley.Domain.Entities;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Drives the HomebaseHUD/UserInfo HUD elements that surface the Life Goals
    /// system to the player:
    ///   - Slider_Orange progress bar (absolute mapping: max = next unrealized
    ///     goal threshold, value = current Total Net Worth). Hidden until the
    ///     player picks Life Goals; pinned at full when all three are realized.
    ///   - Age text (e.g. "Age: 25", advances on year boundaries)
    ///
    /// Inspector wiring (BLOCKING per CLAUDE.md MCP rule):
    ///   - _progressSlider: HomebaseHUD/UserInfo/Slider_Orange (UnityEngine.UI.Slider)
    ///   - _ageText:        TextMeshProUGUI label inside HomebaseHUD/UserInfo
    ///
    /// Either field may be null; the handlers short-circuit without a NRE so
    /// partially-wired layouts still compile and run.
    /// </summary>
    public class LifeGoalsHud : MonoBehaviour
    {
        [Header("Bindings")]
        [Tooltip("HomebaseHUD/UserInfo/Slider_Orange. Tracks next-cheapest unrealized goal.")]
        [SerializeField] private Slider _progressSlider;

        [Tooltip("Age label. Updates on year boundaries to 'Age: NN'.")]
        [SerializeField] private TextMeshProUGUI _ageText;

        // Set by HandleAllGoalsRealized so the slider stays pinned at full
        // even if a later OnGoalProgressChanged were to fire (defensive --
        // GoalProgressTracker should not, but UI ignores it regardless).
        private bool _allGoalsRealized;

        private void OnEnable()
        {
            GameEvents.OnGoalProgressChanged += HandleGoalProgressChanged;
            GameEvents.OnGoalRealized += HandleGoalRealized;
            GameEvents.OnAllGoalsRealized += HandleAllGoalsRealized;
            GameEvents.OnYearEnd += HandleYearEnd;
            GameEvents.OnGameStart += HandleGameStart;

            // Phase 1 handler is null: LifeGoalSelectionService is owned by GameManager
            // and hydrates from the same DTO in its own Phase 1 path. We only need
            // Phase 2 to re-pull the net-worth snapshot AFTER selection is hydrated,
            // so the cascade NetWorthService -> GoalProgressTracker -> slider actually
            // produces a slider event (it would early-return on null selection otherwise).
            SaveRestoreCatchUp.Subscribe(null, HandleSaveRestored);

            // Pull-pattern: ask NetWorthService to re-emit current cached values.
            // The cascaded OnNetWorthChanged drives GoalProgressTracker, which
            // fires OnGoalProgressChanged or OnAllGoalsRealized to populate the
            // slider on the very first frame after scene load / save load.
            GameEvents.RaiseRequestNetWorthSnapshot();
        }

        private void OnDisable()
        {
            GameEvents.OnGoalProgressChanged -= HandleGoalProgressChanged;
            GameEvents.OnGoalRealized -= HandleGoalRealized;
            GameEvents.OnAllGoalsRealized -= HandleAllGoalsRealized;
            GameEvents.OnYearEnd -= HandleYearEnd;
            GameEvents.OnGameStart -= HandleGameStart;
            SaveRestoreCatchUp.Unsubscribe(null, HandleSaveRestored);
        }

        private void HandleSaveRestored()
        {
            // Selection is guaranteed populated by now (GameManager hydrates in Phase 1,
            // we run in Phase 2). NetWorthService is dirty from hydration MarkDirty
            // calls so the snapshot recomputes fresh.
            GameEvents.RaiseRequestNetWorthSnapshot();
        }

        private void Start()
        {
            // Initialize age display on first frame so the HUD is not empty
            // before the first OnYearEnd fires (which only fires on day boundaries).
            UpdateAgeText(LifespanConstants.StartingAge);
            // Hide slider until the snapshot/event chain populates it. Pre-selection
            // (intro tutorial, before goals chosen) leaves it hidden permanently.
            if (_progressSlider != null) _progressSlider.gameObject.SetActive(false);
        }

        private void HandleGameStart()
        {
            if (GameEvents.LastLoadedSaveDto != null) return;
            UpdateAgeText(LifespanConstants.StartingAge);
            _allGoalsRealized = false;
            if (_progressSlider != null) _progressSlider.gameObject.SetActive(false);
        }

        private void HandleGoalProgressChanged(float currentNetWorth, float prevThreshold, float nextThreshold)
        {
            if (_progressSlider == null) return;
            if (_allGoalsRealized) return; // pinned -- ignore late events

            if (!_progressSlider.gameObject.activeSelf)
            {
                _progressSlider.gameObject.SetActive(true);
            }
            _progressSlider.minValue = 0f;
            _progressSlider.maxValue = nextThreshold;
            _progressSlider.value = Mathf.Clamp(currentNetWorth, 0f, nextThreshold);
        }

        private void HandleGoalRealized(LifeGoalEntry entry)
        {
            // No-op in absolute-slider mode. The next OnGoalProgressChanged (or
            // OnAllGoalsRealized when this was the final goal) will set the
            // correct max + value for the new state.
        }

        private void HandleAllGoalsRealized(float finalThreshold)
        {
            _allGoalsRealized = true;
            if (_progressSlider == null) return;

            if (!_progressSlider.gameObject.activeSelf)
            {
                _progressSlider.gameObject.SetActive(true);
            }
            _progressSlider.minValue = 0f;
            _progressSlider.maxValue = finalThreshold;
            _progressSlider.value = finalThreshold;
        }

        private void HandleYearEnd(int age)
        {
            UpdateAgeText(age);
        }

        private void UpdateAgeText(int age)
        {
            if (_ageText == null) return;
            _ageText.text = $"Age: {age}";
        }
    }
}
