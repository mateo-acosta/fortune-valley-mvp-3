namespace FortuneValley.Domain.Tutorial
{
    /// <summary>
    /// Single-decision output of <c>BootFlowRouter.Decide</c>. Describes which
    /// path through scene startup a player should see when they click Start:
    /// the first-time tutorial, the returning-player carousel, or a direct
    /// skip into gameplay (teacher preview).
    /// </summary>
    public enum BootFlow
    {
        FirstTimeTutorial = 0,
        NormalCarousel,
        SkipTutorial
    }
}
