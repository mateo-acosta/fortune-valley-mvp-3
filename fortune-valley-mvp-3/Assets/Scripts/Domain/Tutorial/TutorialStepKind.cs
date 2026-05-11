namespace FortuneValley.Domain.Tutorial
{
    /// <summary>
    /// Discrete step kinds authored on a TutorialStepSO. Dialog steps are
    /// advanced by a tap; WaitForLifeGoalsSelected gates advancement on
    /// OnLifeGoalsSelected firing. Integer values are pinned so existing
    /// serialized assets keep their meaning if values are added or removed.
    /// </summary>
    public enum TutorialStepKind
    {
        Dialog = 0,
        WaitForLifeGoalsSelected = 9
    }
}
