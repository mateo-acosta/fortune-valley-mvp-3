namespace FortuneValley.Domain.Notifications
{
    /// <summary>
    /// Resolved banner ready for display. Built by a dispatcher + message builder
    /// from a typed event context; consumed by the GuidanceController and pushed
    /// onto a BannerQueue.
    /// </summary>
    public readonly struct GuidanceBannerRequest
    {
        public string Title { get; }
        public string Message { get; }
        public GuidanceSeverity Severity { get; }
        public GuidanceTargetIntent TargetIntent { get; }
        public string TargetData { get; }
        public string SourceTipId { get; }

        public GuidanceBannerRequest(
            string title,
            string message,
            GuidanceSeverity severity,
            GuidanceTargetIntent targetIntent,
            string targetData,
            string sourceTipId)
        {
            Title = title;
            Message = message;
            Severity = severity;
            TargetIntent = targetIntent;
            TargetData = targetData;
            SourceTipId = sourceTipId;
        }
    }
}
