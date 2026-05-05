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
        public static readonly bool InsuranceEnabled = false;
    }
}
