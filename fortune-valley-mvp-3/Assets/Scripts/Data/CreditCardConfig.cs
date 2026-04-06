using UnityEngine;

namespace FortuneValley.Core
{
    /// <summary>
    /// Configuration for the credit card system.
    ///
    /// LEARNING DESIGN: These values control how expensive credit card
    /// debt becomes. High APR makes carrying a balance costly, teaching
    /// students to pay in full when possible.
    /// </summary>
    [CreateAssetMenu(fileName = "CreditCardConfig", menuName = "Fortune Valley/Credit Card Config")]
    public class CreditCardConfig : ScriptableObject
    {
        [Header("Credit Limit")]
        [Tooltip("Maximum amount the player can charge to the credit card")]
        [SerializeField] private float _creditLimit = 5000f;

        [Header("Interest")]
        [Tooltip("Annual Percentage Rate (e.g. 0.24 = 24% APR)")]
        [SerializeField] private float _apr = 0.24f;

        [Header("Minimum Payment")]
        [Tooltip("Minimum payment as a percentage of statement balance (e.g. 0.02 = 2%)")]
        [SerializeField] private float _minimumPaymentPercent = 0.02f;

        [Tooltip("Minimum payment floor in dollars (whichever is higher)")]
        [SerializeField] private float _minimumPaymentFloor = 25f;

        [Header("Billing Cycle")]
        [Tooltip("Number of in-game days per billing cycle")]
        [SerializeField] private int _billingCycleDays = 30;

        // Read-only accessors
        public float CreditLimit => _creditLimit;
        public float APR => _apr;
        public float MinimumPaymentPercent => _minimumPaymentPercent;
        public float MinimumPaymentFloor => _minimumPaymentFloor;
        public int BillingCycleDays => _billingCycleDays;
    }
}
