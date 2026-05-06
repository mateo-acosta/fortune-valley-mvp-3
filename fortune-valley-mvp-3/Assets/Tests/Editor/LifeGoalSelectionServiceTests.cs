using NUnit.Framework;
using FortuneValley.Core;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LifeGoalSelectionServiceTests
    {
        private static LifeGoalEntry[] ValidTrio()
        {
            return new[]
            {
                new LifeGoalEntry("first_apartment", LifeGoalTier.Starter, 100_000f),
                new LifeGoalEntry("family_home", LifeGoalTier.Mid, 500_000f),
                new LifeGoalEntry("retire_early", LifeGoalTier.Ambitious, 2_000_000f),
            };
        }

        [TearDown]
        public void TearDown()
        {
            // Tests subscribe via the service constructor to a static event.
            // Clear so cross-test residue does not leak.
            GameEvents.ClearAllSubscriptions();
        }

        [Test]
        public void HasSelection_FalseAtConstruction()
        {
            using (var svc = new LifeGoalSelectionService())
            {
                Assert.IsFalse(svc.HasSelection);
                Assert.IsNull(svc.CurrentSelection);
            }
        }

        [Test]
        public void RaiseLifeGoalsSelected_StoresSelection()
        {
            using (var svc = new LifeGoalSelectionService())
            {
                var selection = new LifeGoalSelection(ValidTrio());

                GameEvents.RaiseLifeGoalsSelected(selection);

                Assert.IsTrue(svc.HasSelection);
                Assert.AreSame(selection, svc.CurrentSelection);
            }
        }

        [Test]
        public void HydrateFromDto_ValidEntries_PopulatesSelection()
        {
            using (var svc = new LifeGoalSelectionService())
            {
                bool ok = svc.HydrateFromDto(ValidTrio());

                Assert.IsTrue(ok);
                Assert.IsTrue(svc.HasSelection);
                Assert.AreEqual(LifeGoalSelection.RequiredEntryCount, svc.CurrentSelection.Entries.Length);
            }
        }

        [Test]
        public void HydrateFromDto_NullEntries_LeavesNoSelection()
        {
            using (var svc = new LifeGoalSelectionService())
            {
                bool ok = svc.HydrateFromDto(null);

                Assert.IsFalse(ok);
                Assert.IsFalse(svc.HasSelection);
            }
        }

        [Test]
        public void HydrateFromDto_DuplicateTier_LeavesNoSelection()
        {
            using (var svc = new LifeGoalSelectionService())
            {
                var bad = new[]
                {
                    new LifeGoalEntry("a", LifeGoalTier.Starter, 100_000f),
                    new LifeGoalEntry("b", LifeGoalTier.Starter, 100_000f),
                    new LifeGoalEntry("c", LifeGoalTier.Ambitious, 2_000_000f),
                };

                bool ok = svc.HydrateFromDto(bad);

                Assert.IsFalse(ok);
                Assert.IsFalse(svc.HasSelection);
            }
        }

        [Test]
        public void BuildDtoEntries_ReturnsNullWhenNoSelection()
        {
            using (var svc = new LifeGoalSelectionService())
            {
                Assert.IsNull(svc.BuildDtoEntries());
            }
        }

        [Test]
        public void BuildDtoEntries_RoundTrips()
        {
            using (var svc = new LifeGoalSelectionService())
            {
                svc.HydrateFromDto(ValidTrio());

                var dto = svc.BuildDtoEntries();

                Assert.IsNotNull(dto);
                Assert.AreEqual(LifeGoalSelection.RequiredEntryCount, dto.Length);
            }
        }

        [Test]
        public void Dispose_UnsubscribesFromEvent()
        {
            var svc = new LifeGoalSelectionService();
            svc.Dispose();

            GameEvents.RaiseLifeGoalsSelected(new LifeGoalSelection(ValidTrio()));

            // Service was disposed before the event fired; selection should remain unset.
            Assert.IsFalse(svc.HasSelection);
        }
    }
}
