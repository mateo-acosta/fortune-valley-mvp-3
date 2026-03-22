using UnityEngine;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Core
{
    /// <summary>
    /// Subscribes to GameEvents and constructs decision DTOs for the FDLS.
    /// Enqueues each decision via APIClient for batched sending.
    /// </summary>
    public class DecisionLogger : MonoBehaviour
    {
        [SerializeField] private APIClient _apiClient;

        private string _sessionId;
        private string _gameMode = "homebase";

        /// <summary>
        /// Set the active session ID (received from server on session start).
        /// </summary>
        public void SetSessionId(string sessionId)
        {
            _sessionId = sessionId;
        }

        /// <summary>
        /// Set the current game mode for tagging decisions.
        /// </summary>
        public void SetGameMode(string gameMode)
        {
            _gameMode = gameMode;
        }

        private void OnEnable()
        {
            GameEvents.OnInvestmentCreated += HandleInvestmentCreated;
            GameEvents.OnInvestmentWithdrawn += HandleInvestmentWithdrawn;
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnRestaurantUpgraded += HandleRestaurantUpgraded;
        }

        private void OnDisable()
        {
            GameEvents.OnInvestmentCreated -= HandleInvestmentCreated;
            GameEvents.OnInvestmentWithdrawn -= HandleInvestmentWithdrawn;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnRestaurantUpgraded -= HandleRestaurantUpgraded;
        }

        private void HandleInvestmentCreated(ActiveInvestment inv)
        {
            if (_apiClient == null || !_apiClient.CanPersist()) return;

            var dto = new DecisionEventDTO
            {
                session_id = _sessionId,
                game_mode = _gameMode,
                in_game_day = inv.StartTick,
                decision_type = "investment_buy",
                instrument_id = inv.InstrumentName,
                gross_amount = inv.Principal,
                category = "investment",
                line_items = new[]
                {
                    new DecisionLineItemDTO
                    {
                        account_affected = "investing",
                        change_amount = inv.Principal,
                        flow_category = "outflow"
                    }
                }
            };

            _apiClient.EnqueueDecision(dto);
        }

        private void HandleInvestmentWithdrawn(ActiveInvestment inv, float payout)
        {
            if (_apiClient == null || !_apiClient.CanPersist()) return;

            var dto = new DecisionEventDTO
            {
                session_id = _sessionId,
                game_mode = _gameMode,
                in_game_day = inv.StartTick,
                decision_type = "investment_sell",
                instrument_id = inv.InstrumentName,
                gross_amount = payout,
                category = "investment",
                line_items = new[]
                {
                    new DecisionLineItemDTO
                    {
                        account_affected = "checking",
                        change_amount = payout,
                        flow_category = "inflow"
                    }
                }
            };

            _apiClient.EnqueueDecision(dto);
        }

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (_apiClient == null || !_apiClient.CanPersist()) return;

            string decisionType = owner == Owner.Player ? "lot_purchase" : "rival_lot_taken";
            string category = owner == Owner.Player ? "expense" : "event";

            var dto = new DecisionEventDTO
            {
                session_id = _sessionId,
                game_mode = _gameMode,
                in_game_day = 0, // Will be set by caller or tick context
                decision_type = decisionType,
                instrument_id = lotId,
                category = category
            };

            _apiClient.EnqueueDecision(dto);
        }

        private void HandleRestaurantUpgraded(int newLevel)
        {
            if (_apiClient == null || !_apiClient.CanPersist()) return;

            var dto = new DecisionEventDTO
            {
                session_id = _sessionId,
                game_mode = _gameMode,
                in_game_day = 0,
                decision_type = "franchise_upgrade",
                category = "expense"
            };

            _apiClient.EnqueueDecision(dto);
        }
    }
}
