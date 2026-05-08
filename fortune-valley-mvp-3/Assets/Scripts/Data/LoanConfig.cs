using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Defines a loan option available to the player.
    /// Different configs offer different terms, APR, and requirements.
    ///
    /// LEARNING DESIGN: Students compare loan options to understand
    /// how APR, term length, and down payment affect total cost.
    /// Higher credit scores unlock better terms.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLoanConfig", menuName = "Fortune Valley/Loan Config")]
    public class LoanConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique identifier for this loan option")]
        [SerializeField] private string _loanId;

        [Tooltip("Display name (e.g., 'Standard 12-Month')")]
        [SerializeField] private string _displayName;

        [Tooltip("Short marketing tagline shown under the loan name (e.g., 'Lower monthly payment, longer term')")]
        [TextArea(1, 2)]
        [SerializeField] private string _tagline;

        [Header("Terms")]
        [Tooltip("Annual percentage rate (e.g., 0.08 for 8%)")]
        [SerializeField] private float _apr = 0.08f;

        [Tooltip("Loan duration in in-game years (1 cycle = 1 in-game year = 30 in-game days)")]
        [SerializeField] private int _termYears = 12;

        [Tooltip("Required down payment as fraction of price (e.g., 0.20 for 20%)")]
        [SerializeField] private float _downPaymentPercent = 0.20f;

        [Header("Visual")]
        [Tooltip("Image shown in the loan carousel browser")]
        [SerializeField] private Sprite _loanImage;

        [Header("Requirements")]
        [Tooltip("Minimum credit score to qualify")]
        [SerializeField] private int _minimumCreditScore = 600;

        [Tooltip("Maximum debt-to-income ratio for approval (e.g., 0.40 for 40%)")]
        [SerializeField] private float _maxDtiRatio = 0.40f;

        [Tooltip("Maximum principal this loan can finance. Used to filter loans per lot cost.")]
        [SerializeField] private float _maxPrincipal = 1000000f;

        // Read-only accessors
        public string LoanId => _loanId;
        public string DisplayName => _displayName;
        public string Tagline => _tagline;
        public Sprite LoanImage => _loanImage;
        public float APR => _apr;
        public int TermYears => _termYears;
        public float DownPaymentPercent => _downPaymentPercent;
        public int MinimumCreditScore => _minimumCreditScore;
        public float MaxDtiRatio => _maxDtiRatio;
        public float MaxPrincipal => _maxPrincipal;
    }
}
