using System.Collections.Generic;
using FortuneValley.Core;

namespace FortuneValley.UI
{
    /// <summary>
    /// Shared lot name lookup for UI panels.
    /// Accepts an already-read AllLots list (property read, no cross-layer method call)
    /// and returns the display name for a given lot ID.
    /// </summary>
    public static class LotDisplayHelper
    {
        /// <summary>
        /// Finds the display name for a lot. Returns the raw lotId as fallback
        /// if the lot is not found or the list is null.
        /// </summary>
        public static string GetDisplayName(IReadOnlyList<CityLotDefinition> lots, string lotId)
        {
            if (string.IsNullOrEmpty(lotId))
                return string.Empty;

            if (lots == null)
                return lotId;

            for (int i = 0; i < lots.Count; i++)
            {
                if (lots[i] != null && lots[i].LotId == lotId)
                    return lots[i].DisplayName;
            }

            return lotId;
        }
    }
}
