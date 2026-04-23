using System.Globalization;
using FortuneValley.Domain.Notifications.Contexts;

namespace FortuneValley.Managers.Notifications.Builders
{
    /// <summary>
    /// Formats a CreditCardStatementContext. Positional args:
    ///   {0} = statement balance, currency-formatted (e.g. "$1,234")
    ///   {1} = minimum payment, currency-formatted
    ///   {2} = interest charged, currency-formatted
    /// </summary>
    public class CreditCardStatementMessageBuilder : IBannerMessageBuilder<CreditCardStatementContext>
    {
        public (string title, string message) Build(string titleTemplate, string messageTemplate, CreditCardStatementContext context)
        {
            object[] args =
            {
                FormatCurrency(context.StatementBalance),
                FormatCurrency(context.MinimumPayment),
                FormatCurrency(context.InterestCharged)
            };
            return (
                string.Format(CultureInfo.InvariantCulture, titleTemplate ?? string.Empty, args),
                string.Format(CultureInfo.InvariantCulture, messageTemplate ?? string.Empty, args));
        }

        private static string FormatCurrency(float amount) =>
            "$" + amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
