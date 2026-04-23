using System.Globalization;
using FortuneValley.Domain.Notifications.Contexts;

namespace FortuneValley.Managers.Notifications.Builders
{
    /// <summary>
    /// Formats a LoanHeldWithoutLotContext. Positional args:
    ///   {0} = loan principal, currency-formatted
    ///   {1} = lot id (the lot the loan was intended for)
    ///   {2} = ticks aged (raw integer)
    /// </summary>
    public class LoanHeldWithoutLotMessageBuilder : IBannerMessageBuilder<LoanHeldWithoutLotContext>
    {
        public (string title, string message) Build(string titleTemplate, string messageTemplate, LoanHeldWithoutLotContext context)
        {
            object[] args =
            {
                "$" + context.Principal.ToString("N0", CultureInfo.InvariantCulture),
                context.LotId ?? string.Empty,
                context.TicksAged.ToString(CultureInfo.InvariantCulture)
            };
            return (
                string.Format(CultureInfo.InvariantCulture, titleTemplate ?? string.Empty, args),
                string.Format(CultureInfo.InvariantCulture, messageTemplate ?? string.Empty, args));
        }
    }
}
