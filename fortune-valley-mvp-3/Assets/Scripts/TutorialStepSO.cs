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

        [Header("Mask + dialog overrides (in-panel steps)")]
        [Tooltip("If set, the mask donut hole is sized to this target instead of the arrow target. " +
                 "Use for in-panel steps: keep the whole panel visible while the arrow points at one " +
                 "element inside it.")]
        [SerializeField] private TutorialTargetKind _maskTargetKind = TutorialTargetKind.None;

        [Tooltip("Hide the dialog frame + character on this step. Use for in-panel steps where the " +
                 "arrow is enough and the dialog would obscure the panel.")]
        [SerializeField] private bool _hideDialog = false;

        [Tooltip("On step entry, close any open panels/popups. Used after taking a loan so the " +
                 "player sees the world for the next step.")]
        [SerializeField] private bool _closePanelsOnEnter = false;

        [Tooltip("Allow world-space hover canvases (e.g. the For-Sale lot Buy popup) during this " +
                 "step. The tutorial normally suppresses hover so the dialog can take focus; turn " +
                 "this on for steps where the player needs to interact with a world-space hover UI.")]
        [SerializeField] private bool _allowWorldHover = false;

        [Tooltip("Keep the mask full-screen even when a target is set. Use for Dialog steps that " +
                 "show an arrow over an element the player should NOT interact with yet (e.g. the " +
                 "Question Bonus button). The arrow still points; the dim still blocks raycasts.")]
        [SerializeField] private bool _keepFullDim = false;

        [Tooltip("Extra pixels added to the mask hole on each side beyond the target's screen rect. " +
                 "X = horizontal extension, Y = vertical extension. Used per-step to expand the hole " +
                 "when the target's bounds don't cover everything the player needs to see (e.g. the " +
                 "Buy popup with header/footer ornaments outside the panel rect).")]
        [SerializeField] private Vector2 _maskPaddingExtra = Vector2.zero;

        [Tooltip("Extra screen-pixel offset applied to the arrow on top of TutorialHighlight's " +
                 "global offset. Use to nudge the arrow off the bounds-center when the visible " +
                 "thing of interest sits off-center inside the target footprint (e.g. the rival's " +
                 "restaurant doesn't occupy the lot center).")]
        [SerializeField] private Vector2 _arrowScreenOffset = Vector2.zero;

        [Header("Auto-advance (Dialog only)")]
        [Tooltip("Optional. If > 0, Dialog step advances automatically after this many seconds " +
                 "even if the player does not tap. 0 = tap-required.")]
        [SerializeField] private float _autoAdvanceSeconds = 0f;

        public TutorialStepKind Kind => _kind;
        public CharacterPose Pose => _pose;
        public string DialogText => _dialogText;
        public TutorialTargetKind TargetKind => _targetKind;
        public TutorialTargetKind MaskTargetKind => _maskTargetKind;
        public bool HideDialog => _hideDialog;
        public bool ClosePanelsOnEnter => _closePanelsOnEnter;
        public bool AllowWorldHover => _allowWorldHover;
        public bool KeepFullDim => _keepFullDim;
        public Vector2 MaskPaddingExtra => _maskPaddingExtra;
        public Vector2 ArrowScreenOffset => _arrowScreenOffset;
        public float AutoAdvanceSeconds => _autoAdvanceSeconds;
    }
}
