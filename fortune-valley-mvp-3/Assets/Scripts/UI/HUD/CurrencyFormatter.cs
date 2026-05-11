using System;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Shared currency-formatting helper used by HUD displays (BalanceDisplay,
    /// NetWorthDisplay, etc.). Format rules:
    ///  - magnitude >= $1,000     -> "$1,234,567" (N0)
    ///  - magnitude <  $1,000     -> "$123.45"    (F2)
    ///  - negatives keep the sign before the dollar: "-$1,234"
    /// Negative net worth is intentionally surfaced (not floored) so the player
    /// sees they are underwater. Sign placement matches conventional accounting.
    /// </summary>
    public static class CurrencyFormatter
    {
        public static string FormatCurrency(float amount)
        {
            bool negative = amount < 0f;
            float magnitude = Math.Abs(amount);
            string sign = negative ? "-" : string.Empty;
            if (magnitude >= 1000f)
            {
                return $"{sign}${magnitude:N0}";
            }
            return $"{sign}${magnitude:F2}";
        }
    }
}
