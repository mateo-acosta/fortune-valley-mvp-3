using TMPro;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Renders the persistent "+$X/day" total daily income readout on the
    /// homebase HUD. Pure subscriber to OnTotalDailyIncomeChanged; does not
    /// touch managers or game systems directly. The accumulator computes and
    /// raises the total; this component only formats and writes the text.
    /// </summary>
    public class DailyIncomeHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _dailyIncomeText;
        [SerializeField] private string _format = "+${0:N0}/day";

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

            int rounded = Mathf.FloorToInt(total);
            int lastRounded = Mathf.FloorToInt(_lastTotal);
            if (_lastTotal >= 0f && rounded == lastRounded) return;

            _lastTotal = total;
            _dailyIncomeText.text = string.Format(_format, rounded);
        }
    }
}
