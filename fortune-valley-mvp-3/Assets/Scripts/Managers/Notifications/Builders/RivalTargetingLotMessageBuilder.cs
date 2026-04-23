using System.Globalization;
using FortuneValley.Domain.Notifications.Contexts;

namespace FortuneValley.Managers.Notifications.Builders
{
    /// <summary>
    /// Formats a RivalTargetingLotContext. Positional args:
    ///   {0} = lot id
    /// </summary>
    public class RivalTargetingLotMessageBuilder : IBannerMessageBuilder<RivalTargetingLotContext>
    {
        public (string title, string message) Build(string titleTemplate, string messageTemplate, RivalTargetingLotContext context)
        {
            object[] args = { context.LotId ?? string.Empty };
            return (
                string.Format(CultureInfo.InvariantCulture, titleTemplate ?? string.Empty, args),
                string.Format(CultureInfo.InvariantCulture, messageTemplate ?? string.Empty, args));
        }
    }
}
