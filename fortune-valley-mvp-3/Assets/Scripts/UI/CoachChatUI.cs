using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Domain.Entities;
using FortuneValley.Core;

namespace FortuneValley.UI
{
    /// <summary>
    /// Builds and manages the Coach Val chat interface programmatically.
    /// Attached to the ChatColumn at runtime by GameEndChatIntegration.
    /// </summary>
    public class CoachChatUI : MonoBehaviour
    {
        // ===============================================================
        // COLORS
        // ===============================================================

        private static readonly Color CoachBubbleColor = new Color(0.15f, 0.25f, 0.4f, 0.9f);
        private static readonly Color UserBubbleColor = new Color(0.25f, 0.25f, 0.3f, 0.9f);
        private static readonly Color CoachNameColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color UserNameColor = new Color(0.8f, 0.8f, 0.8f);
        private static readonly Color SuggestionColor = new Color(0.2f, 0.4f, 0.6f);
        private static readonly Color InputBgColor = new Color(0.15f, 0.15f, 0.2f);
        private static readonly Color TitleBarColor = new Color(0.1f, 0.15f, 0.25f);
        private static readonly Color ThinkingColor = new Color(0.5f, 0.5f, 0.6f);

        // ===============================================================
        // STATE
        // ===============================================================

        private CoachChatService _chatService;
        private CoachConfig _config;

        private ScrollRect _scrollRect;
        private Transform _contentContainer;
        private TMP_InputField _inputField;
        private Button _sendButton;
        private GameObject _typingIndicator;
        private GameObject _suggestionsContainer;
        private readonly List<GameObject> _messageBubbles = new List<GameObject>();

        // Safety timeout - unlocks UI if response never arrives
        private bool _waitingForResponse;
        private Coroutine _safetyTimeoutCoroutine;

        // ===============================================================
        // PUBLIC API
        // ===============================================================

        /// <summary>
        /// Build the chat UI and initialize the chat service with game data.
        /// </summary>
        public void Initialize(CoachConfig config, bool isPlayerWin,
            GameSummary summary, InvestmentSystem investmentSystem)
        {
            _config = config;

            // Build context string from game data
            string gameContext = CoachContextBuilder.BuildContext(
                isPlayerWin,
                summary,
                investmentSystem != null ? investmentSystem.ActiveInvestments : null,
                investmentSystem != null ? investmentSystem.AvailableInvestments : null);

            // Create chat service
            _chatService = new CoachChatService(config, gameContext);

            // Build UI hierarchy
            BuildUI();

            // Show greeting and suggestions
            AddCoachMessage(config.GreetingMessage);
            ShowSuggestions();
        }

        /// <summary>
        /// Clean up the chat - clear history and destroy dynamic children.
        /// </summary>
        public void Cleanup()
        {
            _chatService?.ClearHistory();

            foreach (var bubble in _messageBubbles)
            {
                if (bubble != null)
                    Destroy(bubble);
            }
            _messageBubbles.Clear();
        }

        // ===============================================================
        // UI CONSTRUCTION
        // ===============================================================

        private void BuildUI()
        {
            // Root layout - this component is on the ChatColumn
            var rootLayout = gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(0, 0, 0, 0);
            rootLayout.spacing = 0;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            // -- Title Bar --
            var titleBar = UIBuilderUtils.CreateUIElement("TitleBar", transform);
            var titleBarImage = titleBar.AddComponent<Image>();
            titleBarImage.color = TitleBarColor;
            var titleBarLE = titleBar.AddComponent<LayoutElement>();
            titleBarLE.preferredHeight = 40;
            titleBarLE.flexibleWidth = 1;

            var titleText = UIBuilderUtils.CreateTMPText("TitleText", titleBar.transform,
                "Coach Val", 18, FontStyles.Bold, CoachNameColor);
            titleText.alignment = TextAlignmentOptions.Center;
            UIBuilderUtils.StretchToFill(titleText.gameObject);

            // -- Scroll Area --
            var scrollArea = UIBuilderUtils.CreateUIElement("ScrollArea", transform);
            var scrollAreaLE = scrollArea.AddComponent<LayoutElement>();
            scrollAreaLE.flexibleHeight = 1;
            scrollAreaLE.flexibleWidth = 1;

            _scrollRect = scrollArea.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Clamped;
            _scrollRect.scrollSensitivity = 20f;

            // Viewport with rect mask (RectMask2D clips by bounds, no Image needed)
            var viewport = UIBuilderUtils.CreateUIElement("Viewport", scrollArea.transform);
            UIBuilderUtils.StretchToFill(viewport);
            viewport.AddComponent<RectMask2D>();

            // Content container inside viewport
            var content = UIBuilderUtils.CreateUIElement("Content", viewport.transform);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 8;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.viewport = viewport.GetComponent<RectTransform>();
            _scrollRect.content = contentRT;
            _contentContainer = content.transform;

            // -- Typing Indicator (hidden by default) --
            _typingIndicator = CreateMessageBubble("Coach Val is thinking...",
                "Coach Val", CoachBubbleColor, ThinkingColor);
            _typingIndicator.SetActive(false);

            // -- Suggestions Container --
            _suggestionsContainer = UIBuilderUtils.CreateUIElement("Suggestions", _contentContainer);
            var sugLayout = _suggestionsContainer.AddComponent<VerticalLayoutGroup>();
            sugLayout.spacing = 4;
            sugLayout.childAlignment = TextAnchor.UpperLeft;
            sugLayout.childControlWidth = true;
            sugLayout.childControlHeight = true;
            sugLayout.childForceExpandWidth = true;
            sugLayout.childForceExpandHeight = false;
            sugLayout.padding = new RectOffset(4, 4, 4, 4);

            // -- Input Area --
            var inputArea = UIBuilderUtils.CreateUIElement("InputArea", transform);
            var inputAreaImage = inputArea.AddComponent<Image>();
            inputAreaImage.color = TitleBarColor;
            var inputAreaLayout = inputArea.AddComponent<HorizontalLayoutGroup>();
            inputAreaLayout.padding = new RectOffset(8, 8, 6, 6);
            inputAreaLayout.spacing = 6;
            inputAreaLayout.childAlignment = TextAnchor.MiddleCenter;
            inputAreaLayout.childControlWidth = true;
            inputAreaLayout.childControlHeight = false;
            inputAreaLayout.childForceExpandWidth = false;
            inputAreaLayout.childForceExpandHeight = false;
            var inputAreaLE = inputArea.AddComponent<LayoutElement>();
            inputAreaLE.preferredHeight = 50;
            inputAreaLE.flexibleWidth = 1;

            // Input field
            var inputFieldGo = UIBuilderUtils.CreateUIElement("InputField", inputArea.transform);
            var inputFieldImage = inputFieldGo.AddComponent<Image>();
            inputFieldImage.color = InputBgColor;
            _inputField = inputFieldGo.AddComponent<TMP_InputField>();
            var inputFieldLE = inputFieldGo.AddComponent<LayoutElement>();
            inputFieldLE.flexibleWidth = 1;
            inputFieldLE.preferredHeight = 36;

            // Input text area
            var textArea = UIBuilderUtils.CreateUIElement("Text Area", inputFieldGo.transform);
            UIBuilderUtils.StretchToFill(textArea);
            var textAreaRT = textArea.GetComponent<RectTransform>();
            textAreaRT.offsetMin = new Vector2(8, 2);
            textAreaRT.offsetMax = new Vector2(-8, -2);

            // Placeholder text
            var placeholder = UIBuilderUtils.CreateTMPText("Placeholder", textArea.transform,
                "Ask about your game...", 14, FontStyles.Italic, new Color(0.4f, 0.4f, 0.5f));
            placeholder.alignment = TextAlignmentOptions.Left;
            UIBuilderUtils.StretchToFill(placeholder.gameObject);

            // Input text
            var inputText = UIBuilderUtils.CreateTMPText("Text", textArea.transform,
                "", 14, FontStyles.Normal, Color.white);
            inputText.alignment = TextAlignmentOptions.Left;
            UIBuilderUtils.StretchToFill(inputText.gameObject);

            _inputField.textViewport = textAreaRT;
            _inputField.textComponent = inputText;
            _inputField.placeholder = placeholder;
            _inputField.fontAsset = inputText.font;
            _inputField.pointSize = 14;

            // Submit on Enter
            _inputField.onSubmit.AddListener(OnInputSubmit);

            // Send button
            _sendButton = UIBuilderUtils.CreateButton("SendButton", inputArea.transform,
                "Ask", new Color(0.2f, 0.5f, 0.7f), 60, 36);
            _sendButton.onClick.AddListener(OnSendClicked);
        }

        // ===============================================================
        // MESSAGE HANDLING
        // ===============================================================

        private void AddCoachMessage(string text)
        {
            var bubble = CreateMessageBubble(text, "Coach Val", CoachBubbleColor, CoachNameColor);
            _messageBubbles.Add(bubble);
            MoveSuggestionsAndTypingToEnd();
            ScrollToBottom();
        }

        private void AddUserMessage(string text)
        {
            var bubble = CreateMessageBubble(text, "You", UserBubbleColor, UserNameColor);
            _messageBubbles.Add(bubble);
            MoveSuggestionsAndTypingToEnd();
            ScrollToBottom();
        }

        private GameObject CreateMessageBubble(string text, string sender, Color bgColor, Color nameColor)
        {
            var bubble = UIBuilderUtils.CreateUIElement("Message", _contentContainer);
            var bubbleImage = bubble.AddComponent<Image>();
            bubbleImage.color = bgColor;

            var bubbleLayout = bubble.AddComponent<VerticalLayoutGroup>();
            bubbleLayout.padding = new RectOffset(10, 10, 6, 8);
            bubbleLayout.spacing = 2;
            bubbleLayout.childAlignment = TextAnchor.UpperLeft;
            bubbleLayout.childControlWidth = true;
            bubbleLayout.childControlHeight = true;
            bubbleLayout.childForceExpandWidth = true;
            bubbleLayout.childForceExpandHeight = false;

            // Sender name
            var nameText = UIBuilderUtils.CreateTMPText("Name", bubble.transform,
                sender, 12, FontStyles.Bold, nameColor);
            var nameLE = nameText.gameObject.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 18;

            // Message content
            var msgText = UIBuilderUtils.CreateTMPText("Content", bubble.transform,
                text, 14, FontStyles.Normal, new Color(0.9f, 0.9f, 0.95f));
            msgText.textWrappingMode = TextWrappingModes.Normal;

            // Auto-size the bubble height
            var fitter = bubble.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return bubble;
        }

        private void ShowSuggestions()
        {
            if (_config == null || _config.SuggestedQuestions == null) return;

            foreach (string question in _config.SuggestedQuestions)
            {
                var btn = UIBuilderUtils.CreateButton("Suggestion", _suggestionsContainer.transform,
                    question, SuggestionColor, 0, 30);

                // Override layout to stretch width
                var le = btn.GetComponent<LayoutElement>();
                le.preferredWidth = -1;
                le.flexibleWidth = 1;
                le.preferredHeight = 32;

                // Make text smaller and left-aligned
                var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.fontSize = 13;
                    btnText.fontStyle = FontStyles.Normal;
                    btnText.alignment = TextAlignmentOptions.Left;
                }

                // Capture for closure
                string q = question;
                btn.onClick.AddListener(() => OnSuggestionClicked(q));
            }
        }

        private void HideSuggestions()
        {
            if (_suggestionsContainer != null)
                _suggestionsContainer.SetActive(false);
        }

        private void ShowTypingIndicator()
        {
            if (_typingIndicator != null)
            {
                _typingIndicator.SetActive(true);
                MoveSuggestionsAndTypingToEnd();
                ScrollToBottom();
            }
        }

        private void HideTypingIndicator()
        {
            if (_typingIndicator != null)
                _typingIndicator.SetActive(false);
        }

        /// <summary>
        /// Keep suggestions and typing indicator at the bottom of the content.
        /// </summary>
        private void MoveSuggestionsAndTypingToEnd()
        {
            if (_typingIndicator != null)
                _typingIndicator.transform.SetAsLastSibling();
            if (_suggestionsContainer != null)
                _suggestionsContainer.transform.SetAsLastSibling();
        }

        private void ScrollToBottom()
        {
            // Rebuild layout first - needed for word-wrapped TMP text in fresh hierarchy
            if (_scrollRect != null && _scrollRect.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
                _scrollRect.normalizedPosition = new Vector2(0, 0);
        }

        // ===============================================================
        // INPUT HANDLING
        // ===============================================================

        private void OnSendClicked()
        {
            SendCurrentInput();
        }

        private void OnInputSubmit(string text)
        {
            SendCurrentInput();
        }

        private void OnSuggestionClicked(string question)
        {
            HideSuggestions();
            SendUserMessage(question);
        }

        private void SendCurrentInput()
        {
            if (_inputField == null) return;

            string text = _inputField.text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                UnityEngine.Debug.Log("[CoachChat] Input was empty, ignoring.");
                return;
            }

            UnityEngine.Debug.Log($"[CoachChat] Sending: {text}");
            _inputField.text = "";
            HideSuggestions();
            SendUserMessage(text);
        }

        private void SendUserMessage(string message)
        {
            if (_chatService == null)
            {
                UnityEngine.Debug.LogWarning("[CoachChat] ChatService is null!");
                return;
            }
            if (_chatService.IsProcessing)
            {
                UnityEngine.Debug.Log("[CoachChat] Already processing, ignoring.");
                return;
            }

            // Show user message
            AddUserMessage(message);

            // Disable input while processing
            SetInputEnabled(false);
            ShowTypingIndicator();
            _waitingForResponse = true;

            // Safety timeout: unlock UI if response never arrives
            float timeoutSeconds = (_config != null ? _config.RequestTimeoutSeconds : 30) + 5f;
            _safetyTimeoutCoroutine = StartCoroutine(SafetyTimeout(timeoutSeconds));

            UnityEngine.Debug.Log("[CoachChat] Starting API request...");
            // Send to API
            StartCoroutine(_chatService.SendMessage(message, OnResponseReceived));
        }

        private void OnResponseReceived(string response, bool isError)
        {
            _waitingForResponse = false;
            if (_safetyTimeoutCoroutine != null)
            {
                StopCoroutine(_safetyTimeoutCoroutine);
                _safetyTimeoutCoroutine = null;
            }

            UnityEngine.Debug.Log($"[CoachChat] Response received (isError={isError}): {response?.Substring(0, Mathf.Min(response?.Length ?? 0, 100))}");
            HideTypingIndicator();
            SetInputEnabled(true);

            AddCoachMessage(response);

            // Focus the input field for quick follow-up
            if (_inputField != null)
                _inputField.ActivateInputField();
        }

        private void SetInputEnabled(bool enabled)
        {
            if (_inputField != null)
                _inputField.interactable = enabled;
            if (_sendButton != null)
                _sendButton.interactable = enabled;
        }

        /// <summary>
        /// Safety net: if the API response never arrives, unlock the UI after a timeout.
        /// Timeout is derived from config (RequestTimeoutSeconds + 5s buffer).
        /// </summary>
        private IEnumerator SafetyTimeout(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if (!_waitingForResponse) yield break;

            UnityEngine.Debug.LogWarning("[CoachChat] Safety timeout - unlocking UI");
            _waitingForResponse = false;
            HideTypingIndicator();
            SetInputEnabled(true);
            AddCoachMessage("Sorry, the request timed out. Try asking again.");
        }
    }
}
