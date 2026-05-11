using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Components
{
    /// <summary>
    /// Single answer row inside the QuestionMaster popup. Wraps the Button, its background Image,
    /// and the TMP label. Applies idle / correct / wrong colors from the shared QuestionUITheme.
    /// Raises click via a local event so the parent popup can route to GameEvents.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AnswerButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private QuestionUITheme _theme;

        public event System.Action<AnswerButton> OnClicked;

        public int AnswerIndex { get; private set; }

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClicked);
        }

        private void HandleClicked()
        {
            OnClicked?.Invoke(this);
        }

        public void SetContent(int answerIndex, string text)
        {
            AnswerIndex = answerIndex;
            if (_label != null) _label.text = text;
            SetIdle();
        }

        public void SetInteractable(bool value)
        {
            if (_button != null) _button.interactable = value;
        }

        public void SetIdle()
        {
            if (_background != null && _theme != null) _background.color = _theme.IdleColor;
        }

        public void SetCorrect()
        {
            if (_background != null && _theme != null) _background.color = _theme.CorrectColor;
        }

        public void SetWrong()
        {
            if (_background != null && _theme != null) _background.color = _theme.WrongColor;
        }
    }
}
