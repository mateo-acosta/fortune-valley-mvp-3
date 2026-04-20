using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using FortuneValley.Domain.Entities;

namespace FortuneValley.Core
{
    /// <summary>
    /// Fluent builder for DecisionEventDTO.
    /// Reduces boilerplate in DecisionLogger handlers from 15-20 lines to 3-5.
    /// Metadata is built via AddMeta* calls and serialized to a JSON string on Build,
    /// so Unity's JsonUtility (which cannot serialize arbitrary dictionaries) is sidestepped.
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
        private string _quizCategory;
        private readonly List<DecisionLineItemDTO> _lineItems = new List<DecisionLineItemDTO>();
        private readonly List<KeyValuePair<string, string>> _metaEntries = new List<KeyValuePair<string, string>>();

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

        public DecisionDTOBuilder QuizCategory(string quizCategory)
        {
            _quizCategory = quizCategory;
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

        public DecisionDTOBuilder AddLineItem(string accountAffected, float changeAmount, string flowCategory, float runningBalance)
        {
            _lineItems.Add(new DecisionLineItemDTO
            {
                account_affected = accountAffected,
                change_amount = changeAmount,
                flow_category = flowCategory,
                running_balance = runningBalance
            });
            return this;
        }

        public DecisionDTOBuilder AddMetaString(string key, string value)
        {
            if (value == null) return this;
            _metaEntries.Add(new KeyValuePair<string, string>(key, "\"" + EscapeJsonString(value) + "\""));
            return this;
        }

        public DecisionDTOBuilder AddMetaInt(string key, int value)
        {
            _metaEntries.Add(new KeyValuePair<string, string>(key, value.ToString(CultureInfo.InvariantCulture)));
            return this;
        }

        public DecisionDTOBuilder AddMetaFloat(string key, float value)
        {
            _metaEntries.Add(new KeyValuePair<string, string>(key, value.ToString("R", CultureInfo.InvariantCulture)));
            return this;
        }

        public DecisionDTOBuilder AddMetaBool(string key, bool value)
        {
            _metaEntries.Add(new KeyValuePair<string, string>(key, value ? "true" : "false"));
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
                quiz_category = _quizCategory,
                client_timestamp_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                metadata_json = BuildMetadataJson(),
                line_items = _lineItems.Count > 0 ? _lineItems.ToArray() : null
            };
        }

        private string BuildMetadataJson()
        {
            if (_metaEntries.Count == 0) return null;
            var sb = new StringBuilder(64);
            sb.Append('{');
            for (int i = 0; i < _metaEntries.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(EscapeJsonString(_metaEntries[i].Key)).Append("\":").Append(_metaEntries[i].Value);
            }
            sb.Append('}');
            return sb.ToString();
        }

        // Minimal JSON string escape. Adequate for the small identifiers and
        // enum-like strings we put into metadata (loan ids, lot ids, category names).
        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)c);
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
