using NUnit.Framework;
using UnityEngine;
using FortuneValley.Core;
using FortuneValley.Domain.Tutorial;
using FortuneValley.Managers.Tutorial;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class TutorialTargetRegistryTests
    {
        private GameObject _hostGo;
        private TutorialTargetRegistry _registry;
        private Transform _buttonTransform;
        private Transform _restaurantTransform;
        private Transform _dynamicTransform;

        [SetUp]
        public void SetUp()
        {
            _hostGo = new GameObject("Registry");
            _registry = _hostGo.AddComponent<TutorialTargetRegistry>();
            _buttonTransform = new GameObject("LoanButton").transform;
            _restaurantTransform = new GameObject("Restaurant").transform;
            _dynamicTransform = new GameObject("DynamicLot").transform;
        }

        [TearDown]
        public void TearDown()
        {
            if (_hostGo != null) Object.DestroyImmediate(_hostGo);
            if (_buttonTransform != null) Object.DestroyImmediate(_buttonTransform.gameObject);
            if (_restaurantTransform != null) Object.DestroyImmediate(_restaurantTransform.gameObject);
            if (_dynamicTransform != null) Object.DestroyImmediate(_dynamicTransform.gameObject);
        }

        private class FakeResolver : ITutorialTargetResolver
        {
            public TutorialTargetKind HandledKind;
            public Transform Target;
            public int Calls;

            public bool TryResolve(TutorialTargetKind kind, out Transform target)
            {
                Calls++;
                if (kind == HandledKind)
                {
                    target = Target;
                    return Target != null;
                }
                target = null;
                return false;
            }
        }

        [Test]
        public void GetTarget_None_ReturnsNull()
        {
            _registry.Initialize(null, null);
            Assert.IsNull(_registry.GetTarget(TutorialTargetKind.None));
        }

        [Test]
        public void GetTarget_StaticHit_ReturnsWiredTransform()
        {
            _registry.Initialize(
                staticEntries: new[]
                {
                    new TutorialTargetRegistry.StaticEntry
                    {
                        kind = TutorialTargetKind.LoanPanelButton,
                        target = _buttonTransform
                    }
                },
                resolvers: null);

            Assert.AreSame(_buttonTransform, _registry.GetTarget(TutorialTargetKind.LoanPanelButton));
        }

        [Test]
        public void GetTarget_DynamicFallback_WhenNoStaticMatch()
        {
            var resolver = new FakeResolver
            {
                HandledKind = TutorialTargetKind.NextAvailableForSaleLot,
                Target = _dynamicTransform
            };
            _registry.Initialize(staticEntries: null, resolvers: new[] { (ITutorialTargetResolver)resolver });

            var hit = _registry.GetTarget(TutorialTargetKind.NextAvailableForSaleLot);
            Assert.AreSame(_dynamicTransform, hit);
            Assert.AreEqual(1, resolver.Calls);
        }

        [Test]
        public void GetTarget_StaticBeatsResolver_WhenBothMatch()
        {
            var resolver = new FakeResolver
            {
                HandledKind = TutorialTargetKind.LoanPanelButton,
                Target = _dynamicTransform
            };
            _registry.Initialize(
                staticEntries: new[]
                {
                    new TutorialTargetRegistry.StaticEntry
                    {
                        kind = TutorialTargetKind.LoanPanelButton,
                        target = _buttonTransform
                    }
                },
                resolvers: new[] { (ITutorialTargetResolver)resolver });

            var hit = _registry.GetTarget(TutorialTargetKind.LoanPanelButton);
            Assert.AreSame(_buttonTransform, hit, "Static entry should win");
            Assert.AreEqual(0, resolver.Calls, "Resolver should not be queried on static hit");
        }

        [Test]
        public void GetTarget_FirstResolverHit_ShortCircuits()
        {
            var first = new FakeResolver
            {
                HandledKind = TutorialTargetKind.NextAvailableForSaleLot,
                Target = _dynamicTransform
            };
            var second = new FakeResolver
            {
                HandledKind = TutorialTargetKind.NextAvailableForSaleLot,
                Target = _restaurantTransform
            };
            _registry.Initialize(
                staticEntries: null,
                resolvers: new ITutorialTargetResolver[] { first, second });

            Assert.AreSame(_dynamicTransform,
                _registry.GetTarget(TutorialTargetKind.NextAvailableForSaleLot));
            Assert.AreEqual(1, first.Calls);
            Assert.AreEqual(0, second.Calls);
        }

        [Test]
        public void GetTarget_NoMatch_ReturnsNull()
        {
            var resolver = new FakeResolver { HandledKind = TutorialTargetKind.LoanPanelButton };
            _registry.Initialize(
                staticEntries: null,
                resolvers: new[] { (ITutorialTargetResolver)resolver });

            Assert.IsNull(_registry.GetTarget(TutorialTargetKind.NextAvailableForSaleLot));
        }

        [Test]
        public void Initialize_ReplacesPreviousBindings()
        {
            _registry.Initialize(
                staticEntries: new[]
                {
                    new TutorialTargetRegistry.StaticEntry
                    {
                        kind = TutorialTargetKind.LoanPanelButton,
                        target = _buttonTransform
                    }
                },
                resolvers: null);

            _registry.Initialize(staticEntries: null, resolvers: null);

            Assert.IsNull(_registry.GetTarget(TutorialTargetKind.LoanPanelButton));
        }
    }
}
