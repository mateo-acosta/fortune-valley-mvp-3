using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FortuneValley.Core;
using FortuneValley.Domain.Tutorial;

namespace FortuneValley.UI.Tutorial
{
    /// <summary>
    /// Character portrait + dialog box + typewriter + Skip button. Subscribes
    /// to GameEvents tutorial-UI-control events (visibility, dialog, skip
    /// reveal) and raises tutorial-input events (advance tap, skip tap).
    /// Decoupled from IntroTutorialController so the controller (Managers
    /// layer) never references UI types directly.
    ///
    /// The typewriter reveal runs in Update() with unscaledDeltaTime so the
    /// tutorial still animates while TimeManager is paused.
    /// </summary>
    public class TutorialOverlayUI : MonoBehaviour
    {
        [Serializable]
        public struct PoseEntry
        {
            public CharacterPose pose;
            public Sprite sprite;
        }

        [Header("Root")]
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Character")]
        [SerializeField] private Image _portraitImage;
        [SerializeField] private PoseEntry[] _poseSprites;

        [Header("Dialog")]
        [SerializeField] private TextMeshProUGUI _dialogText;
        [SerializeField] private Button _advanceTapArea;
        [SerializeField] private GameObject _tapIndicator;
        [Tooltip("Characters per second revealed by the typewriter.")]
        [SerializeField] private float _charsPerSecond = 30f;

        [Header("Skip")]
        [SerializeField] private GameObject _skipButtonRoot;
        [SerializeField] private Button _skipButton;

        private string _fullText;
        private float _revealedChars;
        private bool _revealComplete;

        public bool IsShowing => _canvasGroup != null && _canvasGroup.alpha > 0f;
        public bool IsTypewriterComplete => _revealComplete;

        private void Awake()
        {
            if (_advanceTapArea != null) _advanceTapArea.onClick.AddListener(HandleAdvanceTap);
            if (_skipButton != null) _skipButton.onClick.AddListener(HandleSkipTap);
            HideSkipButton();
            Hide();
        }

        private void OnEnable()
        {
            GameEvents.OnTutorialOverlayVisibilityChanged += HandleOverlayVisibilityChanged;
            GameEvents.OnTutorialDialogChanged += HandleDialogChanged;
            GameEvents.OnTutorialSkipRevealed += HandleSkipRevealed;
        }

        private void OnDisable()
        {
            GameEvents.OnTutorialOverlayVisibilityChanged -= HandleOverlayVisibilityChanged;
            GameEvents.OnTutorialDialogChanged -= HandleDialogChanged;
            GameEvents.OnTutorialSkipRevealed -= HandleSkipRevealed;
        }

        private void OnDestroy()
        {
            if (_advanceTapArea != null) _advanceTapArea.onClick.RemoveListener(HandleAdvanceTap);
            if (_skipButton != null) _skipButton.onClick.RemoveListener(HandleSkipTap);
        }

        private void Update()
        {
            if (!IsShowing || _revealComplete || string.IsNullOrEmpty(_fullText)) return;

            _revealedChars += _charsPerSecond * Time.unscaledDeltaTime;
            int charCount = Mathf.Min(Mathf.FloorToInt(_revealedChars), _fullText.Length);
            if (_dialogText != null) _dialogText.maxVisibleCharacters = charCount;

            if (charCount >= _fullText.Length)
            {
                _revealComplete = true;
                if (_tapIndicator != null) _tapIndicator.SetActive(true);
            }
        }

        private void HandleOverlayVisibilityChanged(bool visible)
        {
            if (visible) Show();
            else Hide();
        }

        private void HandleDialogChanged(string text, CharacterPose pose) => SetDialog(text, pose);

        private void HandleSkipRevealed() => RevealSkipButton();

        public void Show()
        {
            // Use CanvasGroup (not SetActive) so subscriptions in OnEnable
            // stay alive when the overlay is hidden. A SetActive-hidden
            // overlay would stop listening for OnTutorialOverlayVisibilityChanged
            // and could never be re-shown via the event.
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
        }

        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
            if (_tapIndicator != null) _tapIndicator.SetActive(false);
            HideSkipButton();
        }

        public void RevealSkipButton()
        {
            if (_skipButtonRoot != null) _skipButtonRoot.SetActive(true);
        }

        public void HideSkipButton()
        {
            if (_skipButtonRoot != null) _skipButtonRoot.SetActive(false);
        }

        public void SetDialog(string text, CharacterPose pose)
        {
            _fullText = text ?? string.Empty;
            _revealedChars = 0f;
            _revealComplete = false;
            if (_tapIndicator != null) _tapIndicator.SetActive(false);
            if (_dialogText != null)
            {
                _dialogText.text = _fullText;
                _dialogText.maxVisibleCharacters = 0;
            }
            ApplyPose(pose);
        }

        public void CompleteTypewriter()
        {
            if (string.IsNullOrEmpty(_fullText))
            {
                _revealComplete = true;
                return;
            }
            _revealedChars = _fullText.Length;
            if (_dialogText != null) _dialogText.maxVisibleCharacters = _fullText.Length;
            _revealComplete = true;
            if (_tapIndicator != null) _tapIndicator.SetActive(true);
        }

        private void ApplyPose(CharacterPose pose)
        {
            if (_portraitImage == null || _poseSprites == null) return;
            for (int i = 0; i < _poseSprites.Length; i++)
            {
                if (_poseSprites[i].pose == pose)
                {
                    _portraitImage.sprite = _poseSprites[i].sprite;
                    return;
                }
            }
        }

        private void HandleAdvanceTap()
        {
            if (!_revealComplete)
            {
                CompleteTypewriter();
                return;
            }
            GameEvents.RaiseTutorialAdvanceRequested();
        }

        private void HandleSkipTap() => GameEvents.RaiseTutorialSkipRequested();
    }
}
