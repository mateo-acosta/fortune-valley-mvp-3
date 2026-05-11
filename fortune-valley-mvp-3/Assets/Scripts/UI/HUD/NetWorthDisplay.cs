using UnityEngine;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.HUD
{
    /// <summary>
    /// Displays the player's live Total Net Worth in the HomebaseHUD/UserInfo block.
    /// Subscribes to GameEvents.OnNetWorthChanged for delta updates and raises
    /// GameEvents.OnRequestNetWorthSnapshot in OnEnable so freshly bound HUDs
    /// (scene reload, save load) get the current value on the first frame
    /// without waiting for the next data change.
    ///
    /// Inspector wiring (BLOCKING per CLAUDE.md MCP rule):
    ///   - _netWorthText: HomebaseHUD/UserInfo/NetWorthText (TMP_Text)
    ///
    /// Negative net worth is rendered as "-$1,234" (sign before the dollar);
    /// kept visible as a learning signal in the financial-literacy game.
    /// </summary>
    public class NetWorthDisplay : MonoBehaviour
    {
        [Header("Bindings")]
        [Tooltip("HomebaseHUD/UserInfo/NetWorthText. Live total net worth display.")]
        [SerializeField] private TMP_Text _netWorthText;

        private void OnEnable()
        {
            GameEvents.OnNetWorthChanged += HandleNetWorthChanged;
            // Pull-pattern: ask NetWorthService to re-emit current cached values
            // immediately so the very first frame shows real NW, not the placeholder.
            GameEvents.RaiseRequestNetWorthSnapshot();
        }

        private void OnDisable()
        {
            GameEvents.OnNetWorthChanged -= HandleNetWorthChanged;
        }

        private void HandleNetWorthChanged(float totalNetWorth, float liquidNetWorth)
        {
            if (_netWorthText != null)
            {
                _netWorthText.text = CurrencyFormatter.FormatCurrency(totalNetWorth);
            }
        }
    }
}
