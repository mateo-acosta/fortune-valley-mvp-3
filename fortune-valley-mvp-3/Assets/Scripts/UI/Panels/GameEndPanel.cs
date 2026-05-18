using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Entities;
using FortuneValley.Core;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// End game summary panel showing win/lose status and financial statistics.
    /// Provides closure and a learning moment for students.
    ///
    /// LEARNING DESIGN: The end screen is crucial for reflection. By showing
    /// what happened (days played, lots owned, investment gains), students can
    /// connect their decisions to outcomes. "I won because I invested early
    /// and compound interest helped me afford lots faster."
    /// </summary>
    public class GameEndPanel : UIPanel
    {
        // ═══════════════════════════════════════════════════════════════
        // UI REFERENCES
        // ═══════════════════════════════════════════════════════════════

        [Header("Outcome Display")]
        [SerializeField] private TextMeshProUGUI _outcomeText;
        [SerializeField] private Image _outcomeBackground;
        [SerializeField] private Image _outcomeIcon;

        [Header("Statistics")]
        [SerializeField] private TextMeshProUGUI _daysPlayedText;
        [SerializeField] private TextMeshProUGUI _lotsOwnedText;
        [SerializeField] private TextMeshProUGUI _rivalLotsText;
        [SerializeField] private TextMeshProUGUI _netWorthText;
        [SerializeField] private TextMeshProUGUI _investmentGainsText;
        [SerializeField] private TextMeshProUGUI _restaurantIncomeText;

        [Header("Key Decisions")]
        [SerializeField] private Transform _decisionsContainer;
        [SerializeField] private TextMeshProUGUI _decisionItemPrefab;

        [Header("Learning Reflections")]
        [SerializeField] private TextMeshProUGUI _headlineText;
        [SerializeField] private TextMeshProUGUI _investmentInsightText;
        [SerializeField] private TextMeshProUGUI _opportunityCostText;
        [SerializeField] private TextMeshProUGUI _whatIfText;

        [Header("Life Goals Scorecard")]
        [Tooltip("Optional. Renders the GoalScorecard from GameSummary at game end. " +
                 "If null, scorecard text is appended to _whatIfText as a fallback.")]
        [SerializeField] private TextMeshProUGUI _scorecardText;

        [Header("Buttons")]
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("Colors")]
        [SerializeField] private Color _victoryColor = new Color(0.2f, 0.6f, 0.9f);
        [SerializeField] private Color _victoryAccent = new Color(0.9f, 0.8f, 0.2f);
        [SerializeField] private Color _defeatColor = new Color(0.7f, 0.2f, 0.2f);
        [SerializeField] private Color _defeatAccent = new Color(0.4f, 0.1f, 0.1f);
        [SerializeField] private Color _gainColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _lossColor = new Color(0.8f, 0.2f, 0.2f);

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private bool _isPlayerWin;
        private GameSummary _summary;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            SetupButtons();
        }

        private void OnEnable()
        {
            GameEvents.OnGameEndWithSummary += HandleGameEnd;
            // Also listen to the simple OnGameEnd in case summary isn't available
            GameEvents.OnGameEnd += HandleSimpleGameEnd;
        }

        private void OnDisable()
        {
            GameEvents.OnGameEndWithSummary -= HandleGameEnd;
            GameEvents.OnGameEnd -= HandleSimpleGameEnd;
        }

        private void SetupButtons()
        {
            if (_playAgainButton != null)
            {
                _playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleGameEnd(bool isPlayerWin, GameSummary summary)
        {
            _isPlayerWin = isPlayerWin;
            _summary = summary;

            DisplaySummary();
            Show();
        }

        private void HandleSimpleGameEnd(Owner winner)
        {
            // If we get the simple event, create a basic summary
            _isPlayerWin = (winner == Owner.Player);
            _summary = null;

            DisplayBasicOutcome();
            Show();
        }

        // ═══════════════════════════════════════════════════════════════
        // DISPLAY METHODS
        // ═══════════════════════════════════════════════════════════════

        private void DisplaySummary()
        {
            // Set outcome theme
            ApplyTheme(_isPlayerWin);

            // Display outcome text
            if (_outcomeText != null)
            {
                _outcomeText.text = _isPlayerWin ? "VICTORY!" : "DEFEAT";
            }

            if (_summary != null)
            {
                DisplayStatistics();
                DisplayKeyDecisions();
                DisplayLearningReflections();
                DisplayScorecard();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // LIFE GOALS SCORECARD
        // Pure renderer (Issue 5 / 5A) -- reads _summary.Scorecard which is
        // built by GameSummaryBuilder + RetirementEvaluator and contains the
        // realized vs missed split, retirement age, and bankruptcy_flag.
        // No formatting logic lives here beyond layout: any narrative copy
        // belongs in the renderer's strings, not in the data struct.
        // ═══════════════════════════════════════════════════════════════
        private void DisplayScorecard()
        {
            if (_summary == null || _summary.Scorecard == null) return;

            var card = _summary.Scorecard;
            string body = BuildScorecardText(card);

            // Completeness fallback: when none of the dedicated reflection
            // labels are wired in the prefab (the current state), the panel
            // would otherwise show only a bare goals list. Fold the narrative
            // reflections -- which GameSummaryBuilder always populates -- into
            // the (wired) scorecard label so the end screen reads as an
            // intentional recap. Self-disables the instant any dedicated
            // reflection label is wired: then those render it instead and
            // this block is skipped.
            if (HasNoDedicatedReflectionLabels())
            {
                string reflections = BuildReflectionsText(_summary);
                if (!string.IsNullOrEmpty(reflections))
                {
                    body = reflections + "\n\n" + body;
                }
            }

            if (_scorecardText != null)
            {
                _scorecardText.text = body;
                _scorecardText.gameObject.SetActive(true);
            }
            else if (_whatIfText != null)
            {
                // Fallback: append to the what-if reflection so the scorecard
                // is still visible until a dedicated label is wired.
                string current = _whatIfText.text ?? string.Empty;
                _whatIfText.text = string.IsNullOrEmpty(current)
                    ? body
                    : current + "\n\n" + body;
            }
        }

        private static string BuildScorecardText(GoalScorecard card)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Life Goals — ");
            sb.Append(card.RealizedCount);
            sb.Append("/");
            sb.Append(card.TotalGoalCount);
            sb.Append(" realized");
            if (card.bankruptcy_flag) sb.Append(" (bankruptcy on record)");
            sb.AppendLine();

            if (card.realized != null)
            {
                for (int i = 0; i < card.realized.Length; i++)
                {
                    sb.Append("  ✓ ");
                    sb.Append(card.realized[i].goal_id);
                    sb.AppendLine();
                }
            }
            if (card.missed != null)
            {
                for (int i = 0; i < card.missed.Length; i++)
                {
                    sb.Append("  ✗ ");
                    sb.Append(card.missed[i].goal_id);
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        private void DisplayBasicOutcome()
        {
            ApplyTheme(_isPlayerWin);

            if (_outcomeText != null)
            {
                _outcomeText.text = _isPlayerWin ? "VICTORY!" : "DEFEAT";
            }

            // Hide statistics if no summary
            HideStatistics();
        }

        private void DisplayStatistics()
        {
            // Days played
            if (_daysPlayedText != null)
            {
                _daysPlayedText.text = $"Game Duration: {_summary.DaysPlayed} days";
            }

            // Lots owned
            if (_lotsOwnedText != null)
            {
                _lotsOwnedText.text = $"Your Lots: {_summary.PlayerLots}/{_summary.TotalLots}";
                _lotsOwnedText.color = _summary.PlayerLots > _summary.RivalLots ? _gainColor : Color.white;
            }

            // Rival lots
            if (_rivalLotsText != null)
            {
                _rivalLotsText.text = $"Rival Lots: {_summary.RivalLots}/{_summary.TotalLots}";
                _rivalLotsText.color = _summary.RivalLots > _summary.PlayerLots ? _lossColor : Color.white;
            }

            // Net worth
            if (_netWorthText != null)
            {
                _netWorthText.text = $"Final Net Worth: ${_summary.FinalNetWorth:N0}";
            }

            // Investment gains (THE KEY LEARNING METRIC)
            if (_investmentGainsText != null)
            {
                string gainPrefix = _summary.TotalInvestmentGains >= 0 ? "+" : "";
                _investmentGainsText.text = $"Investment Gains: {gainPrefix}${_summary.TotalInvestmentGains:N0}";
                _investmentGainsText.color = _summary.TotalInvestmentGains >= 0 ? _gainColor : _lossColor;

                // Add learning note if they benefited from investing
                if (_summary.TotalInvestmentGains > 100)
                {
                    _investmentGainsText.text += "\nCompound interest helped you grow!";
                }
            }

            // Restaurant income
            if (_restaurantIncomeText != null)
            {
                _restaurantIncomeText.text = $"Restaurant Income: ${_summary.TotalRestaurantIncome:N0}";
            }
        }

        private void DisplayKeyDecisions()
        {
            if (_decisionsContainer == null || _decisionItemPrefab == null) return;
            if (_summary.KeyDecisions == null || _summary.KeyDecisions.Count == 0) return;

            // Clear existing
            foreach (Transform child in _decisionsContainer)
            {
                Destroy(child.gameObject);
            }

            // Add decision items
            foreach (string decision in _summary.KeyDecisions)
            {
                var item = Instantiate(_decisionItemPrefab, _decisionsContainer);
                item.text = $"• {decision}";
            }
        }

        private void DisplayLearningReflections()
        {
            if (_summary == null) return;

            if (_headlineText != null)
                _headlineText.text = _summary.Headline ?? "";
            if (_investmentInsightText != null)
                _investmentInsightText.text = _summary.InvestmentInsight ?? "";
            if (_opportunityCostText != null)
                _opportunityCostText.text = _summary.OpportunityCostInsight ?? "";
            if (_whatIfText != null)
                _whatIfText.text = _summary.WhatIfMessage ?? "";
        }

        // True when the prefab wires none of the four dedicated reflection
        // labels. Reading [SerializeField] refs for null is an allowed check
        // (no cross-layer call). When this is false the dedicated labels
        // render the reflections and the scorecard fallback skips them.
        private bool HasNoDedicatedReflectionLabels()
        {
            return _headlineText == null
                && _investmentInsightText == null
                && _opportunityCostText == null
                && _whatIfText == null;
        }

        // Static helper mirrors the existing BuildScorecardText convention so
        // the StringBuilder/loop-free composition stays out of instance
        // MonoBehaviour methods. Reads only the locally-held GameSummary
        // Domain entity (UI -> Domain, permitted).
        private static string BuildReflectionsText(GameSummary summary)
        {
            var sb = new System.Text.StringBuilder();
            AppendSection(sb, summary.Headline);
            AppendSection(sb, summary.InvestmentInsight);
            AppendSection(sb, summary.OpportunityCostInsight);
            AppendSection(sb, summary.WhatIfMessage);
            return sb.ToString();
        }

        private static void AppendSection(System.Text.StringBuilder sb, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append(value);
        }

        private void HideStatistics()
        {
            if (_daysPlayedText != null) _daysPlayedText.gameObject.SetActive(false);
            if (_lotsOwnedText != null) _lotsOwnedText.gameObject.SetActive(false);
            if (_rivalLotsText != null) _rivalLotsText.gameObject.SetActive(false);
            if (_netWorthText != null) _netWorthText.gameObject.SetActive(false);
            if (_investmentGainsText != null) _investmentGainsText.gameObject.SetActive(false);
            if (_restaurantIncomeText != null) _restaurantIncomeText.gameObject.SetActive(false);
            if (_decisionsContainer != null) _decisionsContainer.gameObject.SetActive(false);
        }

        private void ApplyTheme(bool isVictory)
        {
            Color bgColor = isVictory ? _victoryColor : _defeatColor;
            Color accentColor = isVictory ? _victoryAccent : _defeatAccent;

            if (_outcomeBackground != null)
            {
                _outcomeBackground.color = bgColor;
            }

            if (_outcomeText != null)
            {
                _outcomeText.color = accentColor;
            }

            if (_outcomeIcon != null)
            {
                _outcomeIcon.color = accentColor;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // BUTTON CALLBACKS
        // ═══════════════════════════════════════════════════════════════

        private void OnPlayAgainClicked()
        {
            // Hide via CanvasGroup so the GameObject stays active and never
            // loses its game-over subscription, then start the restart flow.
            // GameOverController reloads the scene on restart.
            Hide();
            GameEvents.RaiseRestartRequested();
        }

        private void OnMainMenuClicked()
        {
            Hide();
            GameEvents.RaiseReturnToTitleRequested();
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Show the end panel with specific data (for testing).
        /// </summary>
        public void ShowWithSummary(bool isWin, GameSummary summary)
        {
            _isPlayerWin = isWin;
            _summary = summary;
            DisplaySummary();
            Show();
        }
    }
}
