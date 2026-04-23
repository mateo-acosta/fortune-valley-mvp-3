using System.Globalization;
using FortuneValley.Domain.Notifications.Contexts;

namespace FortuneValley.Managers.Notifications.Builders
{
    /// <summary>
    /// Formats an AccidentOccurredContext. Positional args:
    ///   {0} = accident name (e.g. "Fire")
    ///   {1} = lot id
    ///   {2} = damage cost, currency-formatted
    /// </summary>
    public class AccidentOccurredMessageBuilder : IBannerMessageBuilder<AccidentOccurredContext>
    {
        public (string title, string message) Build(string titleTemplate, string messageTemplate, AccidentOccurredContext context)
        {
            object[] args =
            {
                context.AccidentName ?? string.Empty,
                context.LotId ?? string.Empty,
                "$" + context.DamageCost.ToString("N0", CultureInfo.InvariantCulture)
            };
            return (
                string.Format(CultureInfo.InvariantCulture, titleTemplate ?? string.Empty, args),
                string.Format(CultureInfo.InvariantCulture, messageTemplate ?? string.Empty, args));
        }
    }
}
