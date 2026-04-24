using System.Collections.Generic;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Domain.Interfaces
{
    /// <summary>
    /// Narrow abstraction over the city lot registry for coin-collection.
    /// Avoids exposing CityLotDefinition (Core layer) so tests can stub
    /// lot state without referencing ScriptableObjects.
    /// </summary>
    public interface ILotRegistry
    {
        string PlayerStarterLotId { get; }
        Owner GetOwner(string lotId);
        int GetTier(string lotId);
        bool LotExists(string lotId);
        float GetIncomeAtTier(string lotId, int tier);
        IEnumerable<(string lotId, float income)> EnumeratePlayerLotIncomes();
    }
}
