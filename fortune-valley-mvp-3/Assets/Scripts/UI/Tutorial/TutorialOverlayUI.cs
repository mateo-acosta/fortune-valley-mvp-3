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
        [SerializeField] private GameObject _tapIndicator;
        [Tooltip("Characters per second revealed by the typewriter.")]
        [SerializeField] private float _charsPerSecond = 30f;

        [Header("Next button")]
        [Tooltip("The dedicated 'Next' button. Only way to advance Dialog steps.")]
        [SerializeField] private Button _buttonNext;
        [Tooltip("Root GameObject to hide during WaitForX steps so only the real game action can advance.")]
        [SerializeField] private GameObject _buttonNextRoot;

        [Header("Skip")]
        [SerializeField] private GameObject _skipButtonRoot;
        [SerializeField] private Button _skipButton;

        [Header("Mask")]
        [Tooltip("4-rect donut mask. Full dim for Dialog steps, hole-around-target for WaitForX steps.")]
        [SerializeField] private MaskOverlay _maskOverlay;

        [Header("Dialog visibility (in-panel steps hide both)")]
        [Tooltip("Frame_Message + dialog text root. Hidden by OnTutorialDialogVisibilityChanged(false).")]
        [SerializeField] private GameObject _frameMessageRoot;
        [Tooltip("Character portrait root. Hidden alongside the dialog frame on in-panel steps.")]
        [SerializeField] private GameObject _characterRoot;

        private string _fullText;
        private float _revealedChars;
        private bool _revealComplete;

        public bool IsShowing => _canvasGroup != null && _canvasGroup.alpha > 0f;
        public bool IsTypewriterComplete => _revealComplete;

        private void Awake()
        {
            if (_buttonNext != null) _buttonNext.onClick.AddListener(HandleAdvanceTap);
            if (_skipButton != null) _skipButton.onClick.AddListener(HandleSkipTap);
            HideSkipButton();
            Hide();
        }

        private void OnEnable()
        {
            GameEvents.OnTutorialOverlayVisibilityChanged += HandleOverlayVisibilityChanged;
            GameEvents.OnTutorialDialogChanged += HandleDialogChanged;
            GameEvents.OnTutorialSkipRevealed += HandleSkipRevealed;
            GameEvents.OnTutorialDialogModeEntered += HandleDialogModeEntered;
            GameEvents.OnTutorialWaitModeEntered += HandleWaitModeEntered;
            GameEvents.OnTutorialDialogWithHighlightEntered += HandleDialogWithHighlightEntered;
            GameEvents.OnTutorialDialogVisibilityChanged += HandleDialogVisibilityChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnTutorialOverlayVisibilityChanged -= HandleOverlayVisibilityChanged;
            GameEvents.OnTutorialDialogChanged -= HandleDialogChanged;
            GameEvents.OnTutorialSkipRevealed -= HandleSkipRevealed;
            GameEvents.OnTutorialDialogModeEntered -= HandleDialogModeEntered;
            GameEvents.OnTutorialWaitModeEntered -= HandleWaitModeEntered;
            GameEvents.OnTutorialDialogWithHighlightEntered -= HandleDialogWithHighlightEntered;
            GameEvents.OnTutorialDialogVisibilityChanged -= HandleDialogVisibilityChanged;
        }

        private void HandleDialogVisibilityChanged(bool visible)
        {
            if (_frameMessageRoot != null) _frameMessageRoot.SetActive(visible);
            if (_characterRoot != null) _characterRoot.SetActive(visible);
        }

        /// <summary>
        /// Dialog step: full-screen dim, Next button visible. Only way forward is Button_Next.
        /// </summary>
        private void HandleDialogModeEntered()
        {
            if (_maskOverlay != null) _maskOverlay.ShowFullDim();
            if (_buttonNextRoot != null) _buttonNextRoot.SetActive(true);
        }

        /// <summary>
        /// WaitForX step: donut hole around target, Next button hidden so only
        /// the real game action (tap restaurant, open panel, etc.) advances.
        /// </summary>
        private void HandleWaitModeEntered(Rect targetScreenRect)
        {
            if (_maskOverlay != null) _maskOverlay.ShowDonut(targetScreenRect);
            if (_buttonNextRoot != null) _buttonNextRoot.SetActive(false);
        }

        /// <summary>
        /// Dialog step that points at a target (e.g. "here's the Investing
        /// tab"): donut hole around target AND Next button still visible so
        /// the player can keep tapping through.
        /// </summary>
        private void HandleDialogWithHighlightEntered(Rect targetScreenRect)
        {
            if (_maskOverlay != null) _maskOverlay.ShowDonut(targetScreenRect);
            if (_buttonNextRoot != null) _buttonNextRoot.SetActive(true);
        }

        private void OnDestroy()
        {
            if (_buttonNext != null) _buttonNext.onClick.RemoveListener(HandleAdvanceTap);
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
            Debug.Log($"[TutorialOverlayUI] OnTutorialOverlayVisibilityChanged({visible}) received. " +
                      $"canvasGroup={(_canvasGroup == null ? "null" : "ok")} " +
                      $"dialogText={(_dialogText == null ? "null" : "ok")} " +
                      $"portrait={(_portraitImage == null ? "null" : "ok")}");
            if (visible) Show();
            else Hide();
        }

        private void HandleDialogChanged(string text, CharacterPose pose)
        {
            Debug.Log($"[TutorialOverlayUI] OnTutorialDialogChanged received. pose={pose} textLen={(text == null ? 0 : text.Length)}");
            SetDialog(text, pose);
        }

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
            if (_maskOverlay != null) _maskOverlay.Hide();
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
