using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;

namespace FortuneValley.Tests.Common
{
    /// <summary>
    /// Shared base for save/restore tests. Resets the static catch-up handles
    /// on GameEvents and destroys any leftover GameSaveBootstrapper GameObjects
    /// between tests so test order does not affect outcomes.
    ///
    /// Statics that survive ClearAllSubscriptions in production
    /// (LastLoadedSaveDto, HasSaveBeenRestored, SaveStateRestoredFromServer,
    /// HasServerConfirmedFreshUser, StartBarrierReleased) are explicitly reset
    /// here so each test starts from a clean slate. The last two feed
    /// GameEvents.SaveRoundTripResolved (the start/autosave barrier predicate),
    /// so a leak would let one test's resolved state unblock another's.
    /// </summary>
    public abstract class SaveTestsBase
    {
        // Track GameObjects spawned in the test; auto-destroyed in TearDown.
        private readonly List<GameObject> _trackedObjects = new List<GameObject>();

        [SetUp]
        public virtual void SetUp()
        {
            GameEvents.ClearAllSubscriptions();
            GameEvents.LastLoadedSaveDto = null;
            GameEvents.HasSaveBeenRestored = false;
            GameEvents.SaveStateRestoredFromServer = false;
            GameEvents.HasServerConfirmedFreshUser = false;
            GameEvents.StartBarrierReleased = false;
            GameSaveBootstrapper.ResetExistingForTests();

            DestroyLeftoverBootstrappers();
        }

        [TearDown]
        public virtual void TearDown()
        {
            for (int i = 0; i < _trackedObjects.Count; i++)
            {
                if (_trackedObjects[i] != null)
                {
                    Object.DestroyImmediate(_trackedObjects[i]);
                }
            }
            _trackedObjects.Clear();

            DestroyLeftoverBootstrappers();

            GameEvents.ClearAllSubscriptions();
            GameEvents.LastLoadedSaveDto = null;
            GameEvents.HasSaveBeenRestored = false;
            GameEvents.SaveStateRestoredFromServer = false;
            GameEvents.HasServerConfirmedFreshUser = false;
            GameEvents.StartBarrierReleased = false;
            GameSaveBootstrapper.ResetExistingForTests();
        }

        /// <summary>
        /// Spawn a GameObject + component of type T and register it for
        /// auto-destruction in TearDown. Lets each test focus on the system
        /// under test without the cleanup boilerplate.
        ///
        /// EditMode does not invoke OnEnable when AddComponent runs, so
        /// callers that depend on subscription side effects (catch-up via
        /// LastLoadedSaveDto, etc.) should pass <paramref name="invokeOnEnable"/>
        /// = true. Defaults to false to keep tests that exercise public
        /// methods directly free of subscription noise.
        /// </summary>
        protected T SpawnComponent<T>(string name = null, bool invokeOnEnable = false) where T : Component
        {
            var go = new GameObject(name ?? typeof(T).Name);
            _trackedObjects.Add(go);
            var comp = go.AddComponent<T>();
            if (invokeOnEnable) InvokeOnEnable(comp);
            return comp;
        }

        /// <summary>
        /// Invokes a component's private OnEnable via reflection. EditMode
        /// tests must do this explicitly because AddComponent does not run
        /// the standard lifecycle.
        /// </summary>
        protected static void InvokeOnEnable(Component component)
        {
            if (component == null) return;
            var method = component.GetType().GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(component, null);
        }

        /// <summary>
        /// Invokes a component's private OnDisable via reflection. Mirrors
        /// <see cref="InvokeOnEnable"/>.
        /// </summary>
        protected static void InvokeOnDisable(Component component)
        {
            if (component == null) return;
            var method = component.GetType().GetMethod("OnDisable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            method?.Invoke(component, null);
        }

        /// <summary>
        /// Explicitly registers an existing GameObject for cleanup in TearDown.
        /// </summary>
        protected void TrackForCleanup(GameObject go)
        {
            if (go != null) _trackedObjects.Add(go);
        }

        private static void DestroyLeftoverBootstrappers()
        {
            // GameSaveBootstrapper is DontDestroyOnLoad in production; in
            // EditMode the scene we inspect is the test runner's. FindObjects
            // with the include-inactive flag ensures we sweep both states.
            var bootstrappers = Object.FindObjectsByType<GameSaveBootstrapper>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < bootstrappers.Length; i++)
            {
                Object.DestroyImmediate(bootstrappers[i].gameObject);
            }
        }
    }
}
