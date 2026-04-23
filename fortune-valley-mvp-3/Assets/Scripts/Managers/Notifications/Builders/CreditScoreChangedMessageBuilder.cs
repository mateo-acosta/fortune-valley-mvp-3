using System.Globalization;
using FortuneValley.Domain.Notifications.Contexts;

namespace FortuneValley.Managers.Notifications.Builders
{
    /// <summary>
    /// Formats a CreditScoreChangedContext. Positional args:
    ///   {0} = new score (integer)
    /// </summary>
    public class CreditScoreChangedMessageBuilder : IBannerMessageBuilder<CreditScoreChangedContext>
    {
        public (string title, string message) Build(string titleTemplate, string messageTemplate, CreditScoreChangedContext context)
        {
            object[] args = { context.NewScore.ToString(CultureInfo.InvariantCulture) };
            return (
                string.Format(CultureInfo.InvariantCulture, titleTemplate ?? string.Empty, args),
                string.Format(CultureInfo.InvariantCulture, messageTemplate ?? string.Empty, args));
        }
    }
}
