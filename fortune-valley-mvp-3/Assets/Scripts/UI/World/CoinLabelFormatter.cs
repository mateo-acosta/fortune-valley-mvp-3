using UnityEngine;

namespace FortuneValley.UI.World
{
    /// <summary>
    /// Pure formatter for the world-space coin button label. Two contexts:
    /// hover shows the building's daily income rate (e.g. "+$1,000/day");
    /// the day-end flash shows what just landed in checking (e.g. "+$1,000").
    /// Distinct format strings let the UI distinguish "rate per day" from
    /// "amount just deposited" with different copy.
    /// </summary>
    public static class CoinLabelFormatter
    {
        public static string FormatRate(float dailyPayout, string format)
        {
            int rounded = Mathf.FloorToInt(dailyPayout);
            return string.Format(format, rounded);
        }

        public static string FormatDeposit(float amount, string format)
        {
            int rounded = Mathf.FloorToInt(amount);
            return string.Format(format, rounded);
        }
    }
}
