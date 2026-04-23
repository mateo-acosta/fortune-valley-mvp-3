using System;
using System.Collections.Generic;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Tutorial;

namespace FortuneValley.Managers.Tutorial
{
    /// <summary>
    /// Single point of wiring for tutorial step targets. Static entries are
    /// inspector-set Transform refs keyed by TutorialTargetKind and cover
    /// fixed scene objects (UI buttons, TopBar money display, the player's
    /// starter restaurant). Dynamic kinds (e.g. NextAvailableForSaleLot)
    /// fall through to a list of ITutorialTargetResolver implementations
    /// wired as MonoBehaviour components on the same GameObject hierarchy.
    /// </summary>
    public class TutorialTargetRegistry : MonoBehaviour
    {
        [Serializable]
        public struct StaticEntry
        {
            public TutorialTargetKind kind;
            public Transform target;
        }

        [Tooltip("Inspector-wired static target bindings (UI buttons, fixed scene objects).")]
        [SerializeField] private StaticEntry[] _staticEntries;

        [Tooltip("MonoBehaviours implementing ITutorialTargetResolver for dynamic target lookup.")]
        [SerializeField] private MonoBehaviour[] _resolverBehaviours;

        private readonly Dictionary<TutorialTargetKind, Transform> _static = new Dictionary<TutorialTargetKind, Transform>();
        private readonly List<ITutorialTargetResolver> _resolvers = new List<ITutorialTargetResolver>();

        private void Awake() => Rebuild();

        /// <summary>
        /// Test/manual injection of static and dynamic bindings. Clears
        /// whatever was already registered.
        /// </summary>
        public void Initialize(StaticEntry[] staticEntries, ITutorialTargetResolver[] resolvers)
        {
            _static.Clear();
            _resolvers.Clear();

            if (staticEntries != null)
            {
                foreach (var e in staticEntries)
                {
                    if (e.kind == TutorialTargetKind.None || e.target == null) continue;
                    _static[e.kind] = e.target;
                }
            }

            if (resolvers != null) _resolvers.AddRange(resolvers);
        }

        private void Rebuild()
        {
            _static.Clear();
            _resolvers.Clear();

            if (_staticEntries != null)
            {
                foreach (var e in _staticEntries)
                {
                    if (e.kind == TutorialTargetKind.None || e.target == null) continue;
                    _static[e.kind] = e.target;
                }
            }

            if (_resolverBehaviours != null)
            {
                foreach (var mb in _resolverBehaviours)
                {
                    if (mb is ITutorialTargetResolver r) _resolvers.Add(r);
                }
            }
        }

        /// <summary>
        /// Resolve a target kind. Checks the static table first, then asks
        /// each registered resolver in order. Returns null if nothing matches;
        /// callers typically treat null as a soft fail (skip the highlight).
        /// </summary>
        public Transform GetTarget(TutorialTargetKind kind)
        {
            if (kind == TutorialTargetKind.None) return null;

            if (_static.TryGetValue(kind, out var staticHit) && staticHit != null) return staticHit;

            foreach (var resolver in _resolvers)
            {
                if (resolver.TryResolve(kind, out var hit) && hit != null) return hit;
            }

            return null;
        }
    }
}
