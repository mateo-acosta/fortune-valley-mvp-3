using System.Globalization;
using FortuneValley.Domain.Notifications.Contexts;

namespace FortuneValley.Managers.Notifications.Builders
{
    /// <summary>
    /// Formats an AccidentResolvedContext. Positional args:
    ///   {0} = accident name
    ///   {1} = lot id
    ///   {2} = total damage cost, currency-formatted
    ///   {3} = player cost, currency-formatted
    ///   {4} = coverage label ("covered" or "uncovered")
    /// </summary>
    public class AccidentResolvedMessageBuilder : IBannerMessageBuilder<AccidentResolvedContext>
    {
        public (string title, string message) Build(string titleTemplate, string messageTemplate, AccidentResolvedContext context)
        {
            object[] args =
            {
                context.AccidentName ?? string.Empty,
                context.LotId ?? string.Empty,
                FormatCurrency(context.TotalDamageCost),
                FormatCurrency(context.PlayerCost),
                context.WasCovered ? "covered" : "uncovered"
            };
            return (
                string.Format(CultureInfo.InvariantCulture, titleTemplate ?? string.Empty, args),
                string.Format(CultureInfo.InvariantCulture, messageTemplate ?? string.Empty, args));
        }

        private static string FormatCurrency(float amount) =>
            "$" + amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
