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
    ///   - Slider_Orange progress bar (next-cheapest unrealized goal)
    ///   - Age text (e.g. "Age: 25", advances on year boundaries)
    ///
    /// Inspector wiring (BLOCKING per CLAUDE.md's MCP Inspector rule):
    ///   - _progressSlider: HomebaseHUD/UserInfo/Slider_Orange (UnityEngine.UI.Slider)
    ///   - _ageText:        TextMeshProUGUI label inside HomebaseHUD/UserInfo
    ///
    /// Either field may be null; the handler short-circuits without a NRE so
    /// partially-wired layouts still compile and run.
    /// </summary>
    public class LifeGoalsHud : MonoBehaviour
    {
        [Header("Bindings")]
        [Tooltip("HomebaseHUD/UserInfo/Slider_Orange. Tracks next-cheapest unrealized goal.")]
        [SerializeField] private Slider _progressSlider;

        [Tooltip("Age label. Updates on year boundaries to 'Age: NN'.")]
        [SerializeField] private TextMeshProUGUI _ageText;

        private void OnEnable()
        {
            GameEvents.OnGoalProgressChanged += HandleGoalProgressChanged;
            GameEvents.OnGoalRealized += HandleGoalRealized;
            GameEvents.OnYearEnd += HandleYearEnd;
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnGoalProgressChanged -= HandleGoalProgressChanged;
            GameEvents.OnGoalRealized -= HandleGoalRealized;
            GameEvents.OnYearEnd -= HandleYearEnd;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void Start()
        {
            // Initialize age display on first frame so the HUD is not empty
            // before the first OnYearEnd fires (which only fires on day boundaries).
            UpdateAgeText(LifespanConstants.StartingAge);
            if (_progressSlider != null) _progressSlider.value = 0f;
        }

        private void HandleGameStart()
        {
            UpdateAgeText(LifespanConstants.StartingAge);
            if (_progressSlider != null) _progressSlider.value = 0f;
        }

        private void HandleGoalProgressChanged(float currentNetWorth, float prevThreshold, float nextThreshold)
        {
            if (_progressSlider == null) return;

            float span = nextThreshold - prevThreshold;
            float ratio = span <= 0f
                ? 1f
                : Mathf.Clamp01((currentNetWorth - prevThreshold) / span);

            _progressSlider.value = ratio;
        }

        private void HandleGoalRealized(LifeGoalEntry entry)
        {
            // Snap to full so the player sees the bar reach 100% before the
            // next OnGoalProgressChanged event drives it back to the partial
            // fill toward the next goal. A future polish pass can replace this
            // with a DOTween animation; current rule (no MonoBehaviour Lerp /
            // arithmetic in Update) precludes a hand-rolled tween here.
            if (_progressSlider != null) _progressSlider.value = 1f;
        }

        private void HandleYearEnd(int age)
        {
            UpdateAgeText(age);
        }

        private void UpdateAgeText(int age)
        {
            if (_ageText == null) return;
            _ageText.text = "Age: " + age.ToString();
        }
    }
}
