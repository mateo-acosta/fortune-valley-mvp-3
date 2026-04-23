using UnityEngine;
using FortuneValley.Domain.Tutorial;

namespace FortuneValley.Core
{
    /// <summary>
    /// Authored content for one beat of the onboarding tutorial. Dialog steps
    /// carry pose + dialog text and advance on tap; WaitForX steps carry a
    /// target kind (resolved by TutorialTargetRegistry) and gate advancement
    /// on a matching GameEvent firing. Auto-advance seconds is optional and
    /// applies only to Dialog steps that should progress without a tap.
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialStep", menuName = "FortuneValley/Tutorial/Tutorial Step")]
    public class TutorialStepSO : ScriptableObject
    {
        [Header("Kind")]
        [SerializeField] private TutorialStepKind _kind = TutorialStepKind.Dialog;

        [Header("Dialog")]
        [SerializeField] private CharacterPose _pose = CharacterPose.Neutral;
        [TextArea(2, 4)]
        [SerializeField] private string _dialogText;

        [Header("Wait-for target (ignored for Dialog steps)")]
        [SerializeField] private TutorialTargetKind _targetKind = TutorialTargetKind.None;

        [Header("Auto-advance (Dialog only)")]
        [Tooltip("Optional. If > 0, Dialog step advances automatically after this many seconds " +
                 "even if the player does not tap. 0 = tap-required.")]
        [SerializeField] private float _autoAdvanceSeconds = 0f;

        public TutorialStepKind Kind => _kind;
        public CharacterPose Pose => _pose;
        public string DialogText => _dialogText;
        public TutorialTargetKind TargetKind => _targetKind;
        public float AutoAdvanceSeconds => _autoAdvanceSeconds;
    }
}
