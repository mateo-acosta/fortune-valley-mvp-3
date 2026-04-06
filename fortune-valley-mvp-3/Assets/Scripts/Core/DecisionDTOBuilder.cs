using System.Collections.Generic;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Fluent builder for DecisionEventDTO.
    /// Reduces boilerplate in DecisionLogger handlers from 15-20 lines to 3-5.
    /// </summary>
    public class DecisionDTOBuilder
    {
        private readonly string _sessionId;
        private readonly string _gameMode;
        private string _decisionType;
        private string _instrumentId;
        private float _grossAmount;
        private int _inGameDay;
        private string _category;
        private readonly List<DecisionLineItemDTO> _lineItems = new List<DecisionLineItemDTO>();

        public DecisionDTOBuilder(string sessionId, string gameMode)
        {
            _sessionId = sessionId;
            _gameMode = gameMode;
        }

        public DecisionDTOBuilder Type(string decisionType)
        {
            _decisionType = decisionType;
            return this;
        }

        public DecisionDTOBuilder Instrument(string instrumentId)
        {
            _instrumentId = instrumentId;
            return this;
        }

        public DecisionDTOBuilder Amount(float grossAmount)
        {
            _grossAmount = grossAmount;
            return this;
        }

        public DecisionDTOBuilder Day(int inGameDay)
        {
            _inGameDay = inGameDay;
            return this;
        }

        public DecisionDTOBuilder Category(string category)
        {
            _category = category;
            return this;
        }

        public DecisionDTOBuilder AddLineItem(string accountAffected, float changeAmount, string flowCategory)
        {
            _lineItems.Add(new DecisionLineItemDTO
            {
                account_affected = accountAffected,
                change_amount = changeAmount,
                flow_category = flowCategory
            });
            return this;
        }

        public DecisionEventDTO Build()
        {
            return new DecisionEventDTO
            {
                session_id = _sessionId,
                game_mode = _gameMode,
                in_game_day = _inGameDay,
                decision_type = _decisionType,
                instrument_id = _instrumentId,
                gross_amount = _grossAmount,
                category = _category,
                line_items = _lineItems.Count > 0 ? _lineItems.ToArray() : null
            };
        }
    }
}
