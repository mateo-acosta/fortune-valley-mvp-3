using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.UI.Tutorial
{
    /// <summary>
    /// Always-active sidekick that toggles the GoalSelectionPanel root in
    /// response to GameEvents.OnGoalSelectionPanelRequested.
    ///
    /// Why a separate component: the panel itself starts inactive (so the
    /// dim backdrop is hidden during normal play), which means its
    /// OnEnable never fires until something turns it on -- so a panel-side
    /// subscription wouldn't hear the show event. This activator sits on
    /// HomebaseSceneManager (always active) and bridges the gap.
    /// </summary>
    public class GoalSelectionPanelActivator : MonoBehaviour
    {
        [Tooltip("The GoalSelectionPanel root. Toggled active/inactive when " +
                 "OnGoalSelectionPanelRequested fires.")]
        [SerializeField] private GameObject _panel;

        private void OnEnable()
        {
            GameEvents.OnGoalSelectionPanelRequested += HandlePanelRequested;
        }

        private void OnDisable()
        {
            GameEvents.OnGoalSelectionPanelRequested -= HandlePanelRequested;
        }

        private void HandlePanelRequested(bool visible)
        {
            if (_panel != null) _panel.SetActive(visible);
        }
    }
}
