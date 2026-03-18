using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Configuration for the AI Coach (Coach Val) chatbot.
    /// Stores API settings, system prompt template, and UI defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "CoachConfig", menuName = "Fortune Valley/Coach Config")]
    public class CoachConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════════
        // API SETTINGS
        // ═══════════════════════════════════════════════════════════════

        [Header("API Settings")]
        [Tooltip("Chat completions endpoint URL")]
        [SerializeField] private string _apiEndpoint = "https://api.openai.com/v1/chat/completions";

        [Tooltip("Model name to use")]
        [SerializeField] private string _modelName = "gpt-4.1-nano";

        [Tooltip("Max tokens for each response")]
        [SerializeField] private int _maxTokens = 300;

        [Tooltip("Temperature (0 = deterministic, 1 = creative)")]
        [Range(0f, 1f)]
        [SerializeField] private float _temperature = 0.7f;

        [Tooltip("HTTP request timeout in seconds")]
        [SerializeField] private int _requestTimeoutSeconds = 15;

        [Tooltip("Environment variable name for the API key")]
        [SerializeField] private string _envVarName = "COACH_API_KEY";

        [Tooltip("API key override — use this if env var doesn't work (e.g., macOS Unity Hub). Leave empty to use env var.")]
        [SerializeField] private string _apiKeyOverride = "";

        // ═══════════════════════════════════════════════════════════════
        // CONVERSATION SETTINGS
        // ═══════════════════════════════════════════════════════════════

        [Header("Conversation")]
        [Tooltip("Max user/assistant exchanges to keep in history (system message always kept)")]
        [SerializeField] private int _maxHistoryExchanges = 10;

        [Tooltip("System prompt template — use {GAME_CONTEXT} placeholder for game data")]
        [TextArea(10, 20)]
        [SerializeField] private string _systemPromptTemplate =
            "You are Coach Val, a friendly financial literacy coach for students playing Fortune Valley.\n\n" +
            "The student just finished a game. Their data is below — use it for personalized explanations.\n\n" +
            "{GAME_CONTEXT}\n\n" +
            "YOUR ROLE:\n" +
            "- Explain why they won/lost, connecting outcomes to financial decisions\n" +
            "- Use simple language (middle school level), 2-4 sentences per response\n" +
            "- Reference their ACTUAL numbers from the data above — never vague generalities\n" +
            "- Focus on: compound interest, opportunity cost, time value of money, risk vs reward\n" +
            "- The student has already read the Learning Reflections on screen — do NOT just repeat them verbatim. Use them as a launchpad to ask the student deeper follow-up questions.\n" +
            "- When the student's first message arrives, open by naming ONE specific number from their game (e.g., investment gains or net worth) to show you know their data.\n" +
            "- NOTE: \"Total Investment Gains\" includes BOTH realized gains from sells AND unrealized gains/losses on currently held positions — they can point in opposite directions. Use the Sell History and Portfolio sections to explain the breakdown.\n\n" +
            "GAME MECHANICS:\n" +
            "- Restaurant earns steady income each day\n" +
            "- Player can invest in stocks, ETFs, bonds, T-bills (different risk/return profiles)\n" +
            "- Must buy city lots to win before the rival AI does\n" +
            "- First to own all lots wins\n\n" +
            "TONE: Warm, encouraging, like a supportive tutor. Ask follow-up questions to make the student think deeper.";

        // ═══════════════════════════════════════════════════════════════
        // UI SETTINGS
        // ═══════════════════════════════════════════════════════════════

        [Header("UI")]
        [Tooltip("Greeting message shown when chat opens")]
        [TextArea(2, 4)]
        [SerializeField] private string _greetingMessage =
            "Hey! I'm Coach Val. I just watched your game — want to talk about what happened? " +
            "Ask me anything about your investments, strategy, or financial concepts!";

        [Tooltip("Clickable starter questions shown below the greeting")]
        [SerializeField] private string[] _suggestedQuestions = new string[]
        {
            "Why did I win/lose?",
            "What is compound interest?",
            "Should I have invested differently?",
            "What is opportunity cost?"
        };

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public string ApiEndpoint => _apiEndpoint;
        public string ModelName => _modelName;
        public int MaxTokens => _maxTokens;
        public float Temperature => _temperature;
        public int RequestTimeoutSeconds => _requestTimeoutSeconds;
        public string EnvVarName => _envVarName;
        public string ApiKeyOverride => _apiKeyOverride;
        public int MaxHistoryExchanges => _maxHistoryExchanges;
        public string SystemPromptTemplate => _systemPromptTemplate;
        public string GreetingMessage => _greetingMessage;
        public string[] SuggestedQuestions => _suggestedQuestions;
    }
}
