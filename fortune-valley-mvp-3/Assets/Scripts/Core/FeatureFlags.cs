namespace FortuneValley.Core
{
    /// <summary>
    /// POC scope flags. Single source of truth for systems that are
    /// authored but disabled until the POC matures. Flip a flag and
    /// the corresponding system's source guards re-enable it.
    /// </summary>
    public static class FeatureFlags
    {
        // Insurance is disabled for the POC: UI is too advanced and the
        // economy reads as opaque to first-time players. Flip to true to
        // re-enable the panel, premium charging, accident rolling, and
        // accident notifications.
        public static bool InsuranceEnabled = false;

        // Credit-card mechanic (balance, limit, statement, utilization) is
        // disabled because nothing in the game charges the card -- it was a
        // dead metric. The credit SCORE itself still flows; it is now driven
        // by loan-payment history and DTI, not by CC behavior. Flip to true
        // to re-enable the CC widgets, statement popup, and CC-side notifications.
        // Non-readonly so tests can flip the flag in SetUp / TearDown.
        public static bool CreditCardChargesEnabled = false;
    }
}
