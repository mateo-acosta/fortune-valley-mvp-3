using UnityEngine;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Live HUD readout of the rival's cumulative per-tick income.
    /// Sums base rival income with every rival-owned lot's tier-scaled bonus
    /// via RivalAI.TotalIncomePerTick.
    /// </summary>
    public class RivalIncomeRateHUD : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private RivalAI _rivalAI;
        [SerializeField] private TimeManager _timeManager;

        [Header("Display")]
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private string _format = "+${0:N0}/day";

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnLotTierChanged += HandleLotTierChanged;
            GameEvents.OnGameStart += HandleGameStart;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnLotTierChanged -= HandleLotTierChanged;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void HandleTick(int tick) => Refresh();
        private void HandleLotPurchased(string lotId, FortuneValley.Domain.Enums.Owner owner) => Refresh();
        private void HandleLotTierChanged(string lotId, int newTier) => Refresh();
        private void HandleGameStart() => Refresh();

        private void Refresh()
        {
            if (_valueText == null) return;
            float ratePerTick = _rivalAI != null ? _rivalAI.TotalIncomePerTick : 0f;
            int ticksPerDay = _timeManager != null ? _timeManager.TicksPerDay : 1;
            _valueText.text = string.Format(_format, ratePerTick * ticksPerDay);
        }
    }
}
