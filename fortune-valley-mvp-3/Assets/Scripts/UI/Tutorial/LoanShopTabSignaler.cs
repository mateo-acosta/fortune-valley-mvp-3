using UnityEngine;
using UnityEngine.UI;
using FortuneValley.Core;

namespace FortuneValley.UI.Tutorial
{
    /// <summary>
    /// Tiny UI hook that raises <see cref="GameEvents.OnLoanShopTabSelected"/>
    /// whenever the player clicks the Shop tab inside the Credit &amp; Loans
    /// panel. Sits on the loan panel root and listens to the Shop button's
    /// onClick. Independent of <c>SidebarController</c>'s own switch logic;
    /// both listeners run in parallel.
    /// </summary>
    public class LoanShopTabSignaler : MonoBehaviour
    {
        [SerializeField] private Button _shopTabButton;

        private void Awake()
        {
            if (_shopTabButton != null) _shopTabButton.onClick.AddListener(HandleShopClicked);
        }

        private void OnDestroy()
        {
            if (_shopTabButton != null) _shopTabButton.onClick.RemoveListener(HandleShopClicked);
        }

        private void HandleShopClicked() => GameEvents.RaiseLoanShopTabSelected();
    }
}
