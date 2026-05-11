using UnityEngine;
using TMPro;
using UnityEngine.UI;
using FortuneValley.Core;

namespace FortuneValley.UI.Popups
{
    /// <summary>
    /// Modal popup shown after a soft bankruptcy reset. Explains to the
    /// student that this is a mid-life reset (not a full game over):
    ///   - Wiped: balances, all loans, insurance, investments, non-starter
    ///     lots, credit score (back to 650), pending income
    ///   - Kept:  age, selected Life Goals (and any already-realized states),
    ///     bankruptcy_flag (now permanently true for this life),
    ///     starter lot ownership (forced to T1 dilapidated)
    ///   - Rival kept everything.
    ///
    /// Subscribes to GameEvents.OnSoftBankruptcyReset; shows itself on fire.
    /// Player dismisses via the Continue button.
    ///
    /// Inspector wiring:
    ///   - _popupRoot (inherited): the GameObject to toggle on/off
    ///   - _continueButton: the OK / dismiss button
    ///   - _bodyText (optional): override copy at runtime; otherwise leave as
    ///     the prefab's default copy
    /// </summary>
    public class BankruptcyPopup : UIPopup
    {
        [Header("Bankruptcy Popup")]
        [SerializeField] private Button _continueButton;

        [Tooltip("Optional. Body copy is set at runtime when wired; otherwise the " +
                 "prefab's static text is used.")]
        [SerializeField] private TextMeshProUGUI _bodyText;

        [Tooltip("Copy shown to the player. Edit here OR set _bodyText directly in the prefab.")]
        [TextArea(3, 8)]
        [SerializeField] private string _bodyCopy =
            "You declared bankruptcy.\n\n" +
            "Your debts, lots, investments, and insurance are wiped. " +
            "Your starter restaurant is back to a dilapidated state.\n\n" +
            "But you keep your age and your Life Goals. " +
            "Your rival keeps their progress. The clock keeps ticking — " +
            "make these years count.";

        private void OnEnable()
        {
            GameEvents.OnSoftBankruptcyReset += HandleSoftBankruptcyReset;
            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(OnContinueClicked);
            }
        }

        private void OnDisable()
        {
            GameEvents.OnSoftBankruptcyReset -= HandleSoftBankruptcyReset;
            if (_continueButton != null)
            {
                _continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        protected override void OnShow()
        {
            if (_bodyText != null && !string.IsNullOrEmpty(_bodyCopy))
            {
                _bodyText.text = _bodyCopy;
            }
        }

        private void HandleSoftBankruptcyReset()
        {
            Show();
        }

        private void OnContinueClicked()
        {
            Hide();
            OnCancelClicked();
        }
    }
}
