using System.Collections.Generic;
using FortuneValley.Core;
using FortuneValley.UI;

namespace FortuneValley.UI.Panels.Credit
{
    /// <summary>
    /// Mutable state for the credit explore panel.
    /// Extracted from CreditExploreSubPanel to keep
    /// non-serialized collections out of the MonoBehaviour.
    /// </summary>
    public class CreditExploreState
    {
        private readonly List<CityLotDefinition> _availableLots = new List<CityLotDefinition>();
        private readonly List<LoanEligibilityResult> _filteredLoans = new List<LoanEligibilityResult>();
        private int _selectedLoanIndex;
        private int _cachedLotCount = -1;

        public List<CityLotDefinition> AvailableLots => _availableLots;
        public List<LoanEligibilityResult> FilteredLoans => _filteredLoans;

        public int SelectedLoanIndex
        {
            get => _selectedLoanIndex;
            set => _selectedLoanIndex = value;
        }

        public int CachedLotCount
        {
            get => _cachedLotCount;
            set => _cachedLotCount = value;
        }

        /// <summary>
        /// Replace the filtered loans list contents.
        /// </summary>
        public void SetFilteredLoans(List<LoanEligibilityResult> results)
        {
            _filteredLoans.Clear();
            _filteredLoans.AddRange(results);
        }
    }
}
