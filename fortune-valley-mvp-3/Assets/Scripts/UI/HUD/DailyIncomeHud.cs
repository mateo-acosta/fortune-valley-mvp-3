using TMPro;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Renders the persistent "+$X/year" total income readout on the
    /// homebase HUD. Subscribes to OnTotalDailyIncomeChanged for the daily
    /// total and scales it to per-year for display so days never appear in
    /// the UI; the underlying tick stays daily.
    /// </summary>
    public class DailyIncomeHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _dailyIncomeText;
        [SerializeField] private string _format = "+${0:N0}/year";

        private float _lastTotal = -1f;

        private void OnEnable()
        {
            GameEvents.OnTotalDailyIncomeChanged += HandleTotalChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnTotalDailyIncomeChanged -= HandleTotalChanged;
        }

        private void HandleTotalChanged(float total)
        {
            if (_dailyIncomeText == null) return;

            float yearly = total * LifespanConstants.DaysPerYear;
            int rounded = Mathf.FloorToInt(yearly);
            int lastRounded = Mathf.FloorToInt(_lastTotal * LifespanConstants.DaysPerYear);
            if (_lastTotal >= 0f && rounded == lastRounded) return;

            _lastTotal = total;
            _dailyIncomeText.text = string.Format(_format, rounded);
        }
    }
}
