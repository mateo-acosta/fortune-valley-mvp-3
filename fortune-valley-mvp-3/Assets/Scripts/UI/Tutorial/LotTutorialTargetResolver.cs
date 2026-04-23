using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Tutorial;

namespace FortuneValley.UI.Tutorial
{
    /// <summary>
    /// UI-layer resolver for <see cref="TutorialTargetKind.NextAvailableForSaleLot"/>.
    /// Owns the mapping from CityManager ownership data to scene-side lot
    /// transforms (via the inspector-wired <c>LotVisual</c> array). Lives
    /// in UI because LotVisual is a UI-layer component; Managers may not
    /// reference UI types directly, so the resolver interface sits in Core
    /// and both layers can implement it.
    /// </summary>
    public class LotTutorialTargetResolver : MonoBehaviour, ITutorialTargetResolver
    {
        [SerializeField] private CityManager _cityManager;
        [SerializeField] private LotVisual[] _lotVisuals;

        public void Initialize(CityManager cityManager, LotVisual[] lotVisuals)
        {
            _cityManager = cityManager;
            _lotVisuals = lotVisuals;
        }

        public bool TryResolve(TutorialTargetKind kind, out Transform target)
        {
            target = null;
            if (kind != TutorialTargetKind.NextAvailableForSaleLot) return false;
            if (_cityManager == null || _lotVisuals == null) return false;

            for (int i = 0; i < _lotVisuals.Length; i++)
            {
                var visual = _lotVisuals[i];
                if (visual == null || visual.LotDefinition == null) continue;
                if (_cityManager.GetOwner(visual.LotDefinition.LotId) != Owner.None) continue;

                target = visual.transform;
                return true;
            }
            return false;
        }
    }
}
