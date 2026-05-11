using System.Globalization;
using FortuneValley.Domain.Notifications.Contexts;

namespace FortuneValley.Managers.Notifications.Builders
{
    /// <summary>
    /// Formats a MonthlyCycleSummaryContext. Positional args:
    ///   {0} = day number
    ///   {1} = total paid, currency-formatted
    ///   {2} = loan payments, currency-formatted
    ///   {3} = credit card payment, currency-formatted
    ///   {4} = insurance premiums, currency-formatted
    ///   {5} = taxes, currency-formatted
    /// </summary>
    public class MonthlyCycleSummaryMessageBuilder : IBannerMessageBuilder<MonthlyCycleSummaryContext>
    {
        public (string title, string message) Build(string titleTemplate, string messageTemplate, MonthlyCycleSummaryContext context)
        {
            object[] args =
            {
                context.DayNumber.ToString(CultureInfo.InvariantCulture),
                FormatCurrency(context.TotalPaid),
                FormatCurrency(context.LoanPayments),
                FormatCurrency(context.CreditCardPayment),
                FormatCurrency(context.InsurancePremiums),
                FormatCurrency(context.Taxes)
            };
            return (
                string.Format(CultureInfo.InvariantCulture, titleTemplate ?? string.Empty, args),
                string.Format(CultureInfo.InvariantCulture, messageTemplate ?? string.Empty, args));
        }

        private static string FormatCurrency(float amount) =>
            "$" + amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
