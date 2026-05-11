using System.Globalization;
using FortuneValley.Domain.Notifications.Contexts;

namespace FortuneValley.Managers.Notifications.Builders
{
    /// <summary>
    /// Formats a RestaurantUpgradedContext into banner copy. Positional args:
    ///   {0} = new tier (integer, 1-based)
    ///   {1} = descriptive label for the new tier ("dilapidated", "finished", "thriving")
    /// </summary>
    public class RestaurantUpgradedMessageBuilder : IBannerMessageBuilder<RestaurantUpgradedContext>
    {
        public (string title, string message) Build(string titleTemplate, string messageTemplate, RestaurantUpgradedContext context)
        {
            object[] args =
            {
                context.NewLevel.ToString(CultureInfo.InvariantCulture),
                LabelFor(context.NewLevel)
            };
            return (
                string.Format(CultureInfo.InvariantCulture, titleTemplate ?? string.Empty, args),
                string.Format(CultureInfo.InvariantCulture, messageTemplate ?? string.Empty, args));
        }

        // Tier taxonomy matches the locked 2026-04-10 visual identity in CLAUDE.md.
        private static string LabelFor(int tier)
        {
            switch (tier)
            {
                case 1: return "dilapidated";
                case 2: return "finished";
                case 3: return "thriving";
                default: return "unknown";
            }
        }
    }
}
