using UnityEngine;
using FortuneValley.City;
using FortuneValley.Core;
using FortuneValley.Domain.Enums;
using FortuneValley.Domain.Tutorial;

namespace FortuneValley.UI.Tutorial
{
    /// <summary>
    /// UI-layer resolver for <see cref="TutorialTargetKind.NextAvailableForSaleLot"/>.
    /// Walks the inspector-wired <c>BlockController</c> array and returns the
    /// transform of the first block whose CityManager ownership is
    /// <see cref="Owner.None"/>. Drag the 7 interactive block GameObjects
    /// into <c>_blocks</c> at scene design time; ambient blocks (those with
    /// no CityLotDefinition assigned) are skipped automatically.
    ///
    /// No runtime discovery (FindFirstObjectByType, etc.); every reference
    /// is inspector-wired per the project's layer rules in CLAUDE.md.
    /// </summary>
    public class LotTutorialTargetResolver : MonoBehaviour, ITutorialTargetResolver
    {
        [SerializeField] private CityManager _cityManager;
        [Tooltip("Drag in the interactive city blocks (7 of them). Blocks without a CityLotDefinition are skipped at runtime.")]
        [SerializeField] private BlockController[] _blocks;

        public void Initialize(CityManager cityManager, BlockController[] blocks)
        {
            _cityManager = cityManager;
            _blocks = blocks;
        }

        public bool TryResolve(TutorialTargetKind kind, out Transform target)
        {
            target = null;
            if (kind != TutorialTargetKind.NextAvailableForSaleLot) return false;
            if (_cityManager == null || _blocks == null) return false;

            for (int i = 0; i < _blocks.Length; i++)
            {
                var block = _blocks[i];
                if (block == null) continue;
                var lotDef = block.OwnedLot;
                if (lotDef == null) continue; // ambient block (no lot assignment)
                if (_cityManager.GetOwner(lotDef.LotId) != Owner.None) continue;

                target = block.transform;
                return true;
            }
            return false;
        }
    }
}
