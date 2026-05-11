using System;

namespace FortuneValley.Domain.Entities.WebPanels
{
    /// <summary>
    /// One row in the iframe's history list (Credit panel History tab).
    /// type values are pinned to the strings the HTML's HISTORY_TYPES
    /// map knows about: "loan-originated", "loan-payment", "missed-payment",
    /// "cc-statement", "cc-payment", "score-change".
    ///
    /// date is a display string. The HTML splits on a "Year N, ..." prefix
    /// when present and falls back to the raw string otherwise; we send a
    /// "Day N" form which renders cleanly with no year suffix.
    /// </summary>
    [Serializable]
    public class HistoryEntryDTO
    {
        public int id;
        public string date;
        public string type;
        public string description;
        public float amount;
        public string sublabel;
    }
}
