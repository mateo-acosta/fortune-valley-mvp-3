using UnityEngine;
using TMPro;
using FortuneValley.Core;
using FortuneValley.Domain;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Live HUD readout of the player's cumulative per-tick income.
    /// Sums base restaurant income with every owned lot's tier-scaled bonus
    /// via RestaurantSystem.TotalIncomePerTick.
    /// </summary>
    public class TotalIncomeRateHUD : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private RestaurantSystem _restaurantSystem;
        [SerializeField] private TimeManager _timeManager;

        [Header("Display")]
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private string _format = "+${0:N0}/year";

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnLotTierChanged += HandleLotTierChanged;
            GameEvents.OnRestaurantUpgraded += HandleRestaurantUpgraded;
            GameEvents.OnGameStart += HandleGameStart;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnLotTierChanged -= HandleLotTierChanged;
            GameEvents.OnRestaurantUpgraded -= HandleRestaurantUpgraded;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void HandleTick(int tick) => Refresh();
        private void HandleLotPurchased(string lotId, FortuneValley.Domain.Enums.Owner owner) => Refresh();
        private void HandleLotTierChanged(string lotId, int newTier) => Refresh();
        private void HandleRestaurantUpgraded(int newLevel) => Refresh();
        private void HandleGameStart() => Refresh();

        private void Refresh()
        {
            if (_valueText == null) return;
            float ratePerTick = _restaurantSystem != null ? _restaurantSystem.TotalIncomePerTick : 0f;
            int ticksPerDay = _timeManager != null ? _timeManager.TicksPerDay : 1;
            float ratePerYear = ratePerTick * ticksPerDay * LifespanConstants.DaysPerYear;
            _valueText.text = string.Format(_format, ratePerYear);
        }
    }
}
