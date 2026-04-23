using System.Globalization;
using FortuneValley.Domain.Notifications.Contexts;

namespace FortuneValley.Managers.Notifications.Builders
{
    /// <summary>
    /// Formats a LoanTakenContext into banner copy. Positional argument order
    /// (authors must match in their templates):
    ///   {0} = principal, formatted with thousands separators, e.g. "$5,000"
    ///   {1} = lot id (raw)
    ///   {2} = term in months, e.g. "24"
    ///   {3} = monthly payment, formatted, e.g. "$250"
    /// Uses InvariantCulture so classroom WebGL builds render identical copy
    /// regardless of browser locale.
    /// </summary>
    public class LoanTakenMessageBuilder : IBannerMessageBuilder<LoanTakenContext>
    {
        public (string title, string message) Build(
            string titleTemplate,
            string messageTemplate,
            LoanTakenContext context)
        {
            object[] args =
            {
                FormatCurrency(context.Principal),
                context.LotId ?? string.Empty,
                context.TermMonths.ToString(CultureInfo.InvariantCulture),
                FormatCurrency(context.MonthlyPayment)
            };

            string title = string.Format(CultureInfo.InvariantCulture, titleTemplate ?? string.Empty, args);
            string message = string.Format(CultureInfo.InvariantCulture, messageTemplate ?? string.Empty, args);
            return (title, message);
        }

        private static string FormatCurrency(float amount) =>
            "$" + amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
