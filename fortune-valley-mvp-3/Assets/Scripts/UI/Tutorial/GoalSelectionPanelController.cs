using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.UI.Tutorial
{
    /// <summary>
    /// Full-screen tutorial panel that asks the new player to pick exactly
    /// three Life Goals -- one Starter, one Mid, one Ambitious.
    ///
    /// On confirm, fires GameEvents.OnLifeGoalsSelected(LifeGoalSelection).
    /// IntroTutorialController's WaitForLifeGoalsSelected step gates on this
    /// event and advances the tutorial to the next dialog beat.
    ///
    /// Inspector wiring (BLOCKING per CLAUDE.md's MCP Inspector rule):
    ///   - _catalog: LifeGoalCatalog asset (Assets/Data/LifeGoals/LifeGoalCatalog.asset)
    ///   - _starterCardPrefab / _midCardPrefab / _ambitiousCardPrefab:
    ///       optional per-tier prefabs. If null, _cardPrefab is used for all.
    ///   - _cardPrefab: shared GoalCard prefab (must contain a GoalCardView).
    ///   - _starterContainer / _midContainer / _ambitiousContainer: parent
    ///       Transforms for each tier column.
    ///   - _confirmButton: enabled only when exactly one goal per tier is selected.
    ///   - _selectionHelpText (optional): "Pick 1 Starter, 1 Mid, 1 Ambitious".
    ///
    /// All values are read from the catalog. The player's three picks become
    /// LifeGoalEntry instances that the LifeGoalSelectionService captures via
    /// the OnLifeGoalsSelected event.
    /// </summary>
    public class GoalSelectionPanelController : MonoBehaviour
    {
        [Header("Catalog")]
        [SerializeField] private LifeGoalCatalog _catalog;

        [Header("Card Layout")]
        [SerializeField] private Transform _starterContainer;
        [SerializeField] private Transform _midContainer;
        [SerializeField] private Transform _ambitiousContainer;
        [SerializeField] private GoalCardView _cardPrefab;

        [Header("Confirmation")]
        [SerializeField] private Button _confirmButton;
        [Tooltip("Optional. Updated to reflect 'X of 3 picked' as the player selects.")]
        [SerializeField] private TextMeshProUGUI _selectionHelpText;

        [Header("Guidance Copy")]
        [Tooltip("Optional. Subtitle shown under the main panel header to frame the goal selection process.")]
        [SerializeField] private TextMeshProUGUI _panelSubtitleText;
        [TextArea(2, 4)]
        [SerializeField] private string _panelSubtitle =
            "Pick one goal from each tier. You'll work toward all three across this life; reach the net worth shown to mark a goal realized.";

        [Tooltip("Optional. Sub-line shown under the Starter tier header.")]
        [SerializeField] private TextMeshProUGUI _starterSubheaderText;
        [TextArea(2, 3)]
        [SerializeField] private string _starterSubheader =
            "Foundational milestones for the early years of your career.";

        [Tooltip("Optional. Sub-line shown under the Mid tier header.")]
        [SerializeField] private TextMeshProUGUI _midSubheaderText;
        [TextArea(2, 3)]
        [SerializeField] private string _midSubheader =
            "Mid-career milestones that take real saving and planning.";

        [Tooltip("Optional. Sub-line shown under the Ambitious tier header.")]
        [SerializeField] private TextMeshProUGUI _ambitiousSubheaderText;
        [TextArea(2, 3)]
        [SerializeField] private string _ambitiousSubheader =
            "Long-horizon ambitions that need consistent investing.";

        private readonly Dictionary<LifeGoalTier, LifeGoalSO> _selected = new Dictionary<LifeGoalTier, LifeGoalSO>();
        private readonly List<GoalCardView> _spawnedCards = new List<GoalCardView>();

        private void OnEnable()
        {
            if (_confirmButton != null)
            {
                _confirmButton.interactable = false;
                _confirmButton.onClick.AddListener(OnConfirmClicked);
            }
        }

        private void OnDisable()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }
        }

        private void Start()
        {
            if (_catalog == null)
            {
                Debug.LogError("[GoalSelectionPanelController] _catalog not wired.");
                return;
            }
            ApplyGuidanceCopy();
            BuildCards();
            UpdateConfirmState();
        }

        // Pushes inspector-authored copy into the wired TMP fields. Each pair is null-safe so any
        // unwired ref simply skips its line, leaving existing scene text untouched.
        private void ApplyGuidanceCopy()
        {
            if (_panelSubtitleText != null) _panelSubtitleText.text = _panelSubtitle;
            if (_starterSubheaderText != null) _starterSubheaderText.text = _starterSubheader;
            if (_midSubheaderText != null) _midSubheaderText.text = _midSubheader;
            if (_ambitiousSubheaderText != null) _ambitiousSubheaderText.text = _ambitiousSubheader;
        }

        private void BuildCards()
        {
            ClearSpawnedCards();

            var goals = _catalog.AllGoals;
            if (goals == null) return;

            for (int i = 0; i < goals.Length; i++)
            {
                var goal = goals[i];
                if (goal == null) continue;

                Transform parent = ResolveContainer(goal.Tier);
                if (parent == null || _cardPrefab == null) continue;

                GoalCardView view = Instantiate(_cardPrefab, parent);
                view.Bind(goal, OnCardClicked);
                _spawnedCards.Add(view);
            }
        }

        private void OnCardClicked(LifeGoalSO goal)
        {
            if (goal == null) return;

            _selected[goal.Tier] = goal;

            // Refresh visuals: highlight the picked card for this tier; un-highlight peers.
            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                var card = _spawnedCards[i];
                if (card == null) continue;
                bool isSelected = card.Goal != null && card.Goal == _selected.GetValueOrDefault(card.Goal.Tier);
                card.SetSelected(isSelected);
            }

            UpdateConfirmState();
        }

        private void UpdateConfirmState()
        {
            int picked = _selected.Count;
            bool allThree = picked == 3
                && _selected.ContainsKey(LifeGoalTier.Starter)
                && _selected.ContainsKey(LifeGoalTier.Mid)
                && _selected.ContainsKey(LifeGoalTier.Ambitious);

            if (_confirmButton != null) _confirmButton.interactable = allThree;
            if (_selectionHelpText != null)
            {
                _selectionHelpText.text = allThree
                    ? "Ready -- press Confirm."
                    : "Pick one goal from each tier (" + picked + " of 3).";
            }
        }

        private void OnConfirmClicked()
        {
            if (_selected.Count != 3) return;

            var entries = new LifeGoalEntry[3];
            entries[0] = MakeEntry(LifeGoalTier.Starter);
            entries[1] = MakeEntry(LifeGoalTier.Mid);
            entries[2] = MakeEntry(LifeGoalTier.Ambitious);

            if (!LifeGoalSelection.IsValidTierComposition(entries))
            {
                Debug.LogError("[GoalSelectionPanelController] Tier composition invalid; not firing OnLifeGoalsSelected.");
                return;
            }

            var selection = new LifeGoalSelection(entries);
            GameEvents.RaiseLifeGoalsSelected(selection);
        }

        private LifeGoalEntry MakeEntry(LifeGoalTier tier)
        {
            var so = _selected[tier];
            return new LifeGoalEntry(so.GoalId, so.Tier, so.NetWorthThreshold);
        }

        private Transform ResolveContainer(LifeGoalTier tier)
        {
            switch (tier)
            {
                case LifeGoalTier.Starter: return _starterContainer;
                case LifeGoalTier.Mid: return _midContainer;
                case LifeGoalTier.Ambitious: return _ambitiousContainer;
                default: return null;
            }
        }

        private void ClearSpawnedCards()
        {
            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                if (_spawnedCards[i] != null) Destroy(_spawnedCards[i].gameObject);
            }
            _spawnedCards.Clear();
            _selected.Clear();
        }
    }
}
