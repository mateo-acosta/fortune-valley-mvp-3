using UnityEngine;
using FortuneValley.Domain.Tutorial;

namespace FortuneValley.Core
{
    /// <summary>
    /// Dynamic tutorial target lookup. Implementations answer TryResolve for
    /// the kinds they handle and return false for anything else.
    /// TutorialTargetRegistry walks its resolver list in order after failing
    /// its static table, so resolvers can be prioritized by placement.
    ///
    /// Lives in Core so UI-layer resolvers (which know how to find lot
    /// visuals, HUD displays, etc.) can implement it without violating the
    /// project's layer direction (UI may depend on Core; Managers may not
    /// depend on UI).
    /// </summary>
    public interface ITutorialTargetResolver
    {
        bool TryResolve(TutorialTargetKind kind, out Transform target);
    }
}
