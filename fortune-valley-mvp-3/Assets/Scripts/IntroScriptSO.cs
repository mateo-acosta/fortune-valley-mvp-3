using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Ordered list of TutorialStepSO references that make up a full
    /// onboarding run. IntroTutorialController walks this list start to
    /// finish; completion of the last step ends the tutorial.
    /// </summary>
    [CreateAssetMenu(fileName = "IntroScript", menuName = "FortuneValley/Tutorial/Intro Script")]
    public class IntroScriptSO : ScriptableObject
    {
        [SerializeField] private TutorialStepSO[] _steps;

        public int StepCount => _steps != null ? _steps.Length : 0;

        public TutorialStepSO GetStep(int index)
        {
            if (_steps == null) return null;
            if (index < 0 || index >= _steps.Length) return null;
            return _steps[index];
        }
    }
}
