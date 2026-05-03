namespace FortuneValley.Managers.WebPanels
{
    /// <summary>
    /// Plain-C# logic layer for a WebPanel bridge. Holds system references
    /// and populates a DTO from live property reads. Pure C# so that
    /// EditMode tests can exercise PopulateDTO without spinning up a
    /// scene or PlayMode.
    ///
    /// Subclasses are responsible for:
    ///   - Reusing the passed-in DTO (clear / overwrite fields, do not
    ///     allocate a new instance) so per-tick allocation stays bounded.
    ///   - Tolerating null system references (return early without
    ///     throwing); the bridge handles that case by returning a null
    ///     payload from BuildPayloadJson.
    /// </summary>
    public abstract class WebPanelBridgeLogic<TDTO> where TDTO : class
    {
        /// <summary>
        /// Fill the supplied DTO with current panel state. Returns true if
        /// the DTO is populated and ready to serialize, false if a
        /// dependency is missing and the caller should skip this push.
        /// </summary>
        public abstract bool PopulateDTO(TDTO target);
    }
}
