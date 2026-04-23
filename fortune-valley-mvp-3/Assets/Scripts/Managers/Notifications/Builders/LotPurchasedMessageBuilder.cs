using System.Globalization;
using FortuneValley.Domain.Notifications.Contexts;

namespace FortuneValley.Managers.Notifications.Builders
{
    /// <summary>
    /// Formats a LotPurchasedContext into banner copy. Positional args:
    ///   {0} = lot id (raw, e.g. "Lot_Bistro")
    /// </summary>
    public class LotPurchasedMessageBuilder : IBannerMessageBuilder<LotPurchasedContext>
    {
        public (string title, string message) Build(string titleTemplate, string messageTemplate, LotPurchasedContext context)
        {
            object[] args = { context.LotId ?? string.Empty };
            return (
                string.Format(CultureInfo.InvariantCulture, titleTemplate ?? string.Empty, args),
                string.Format(CultureInfo.InvariantCulture, messageTemplate ?? string.Empty, args));
        }
    }
}
