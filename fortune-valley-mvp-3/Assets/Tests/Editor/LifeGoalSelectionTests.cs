using NUnit.Framework;
using FortuneValley.Domain.Entities;
using FortuneValley.Domain.Enums;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class LifeGoalSelectionTests
    {
        private static LifeGoalEntry MakeEntry(string id, LifeGoalTier tier, float threshold)
        {
            return new LifeGoalEntry(id, tier, threshold);
        }

        [Test]
        public void Constructor_SortsEntriesByThresholdAscending()
        {
            var entries = new[]
            {
                MakeEntry("retire_early", LifeGoalTier.Ambitious, 2_000_000f),
                MakeEntry("first_apartment", LifeGoalTier.Starter, 100_000f),
                MakeEntry("family_home", LifeGoalTier.Mid, 500_000f),
            };

            var selection = new LifeGoalSelection(entries);

            Assert.AreEqual("first_apartment", selection.Entries[0].goal_id);
            Assert.AreEqual("family_home", selection.Entries[1].goal_id);
            Assert.AreEqual("retire_early", selection.Entries[2].goal_id);
        }

        [Test]
        public void Constructor_RejectsWrongCount()
        {
            var twoEntries = new[]
            {
                MakeEntry("a", LifeGoalTier.Starter, 100_000f),
                MakeEntry("b", LifeGoalTier.Mid, 500_000f),
            };

            Assert.Throws<System.ArgumentException>(() => new LifeGoalSelection(twoEntries));
        }

        [Test]
        public void IsAllRealized_FalseWhenAnyUnrealized()
        {
            var entries = new[]
            {
                MakeEntry("a", LifeGoalTier.Starter, 100_000f),
                MakeEntry("b", LifeGoalTier.Mid, 500_000f),
                MakeEntry("c", LifeGoalTier.Ambitious, 2_000_000f),
            };
            entries[0].MarkRealized(50);
            entries[1].MarkRealized(120);

            var selection = new LifeGoalSelection(entries);

            Assert.IsFalse(selection.IsAllRealized());
        }

        [Test]
        public void IsAllRealized_TrueWhenAllRealized()
        {
            var entries = new[]
            {
                MakeEntry("a", LifeGoalTier.Starter, 100_000f),
                MakeEntry("b", LifeGoalTier.Mid, 500_000f),
                MakeEntry("c", LifeGoalTier.Ambitious, 2_000_000f),
            };
            entries[0].MarkRealized(50);
            entries[1].MarkRealized(120);
            entries[2].MarkRealized(900);

            var selection = new LifeGoalSelection(entries);

            Assert.IsTrue(selection.IsAllRealized());
        }

        [Test]
        public void NextUnrealized_ReturnsCheapestUnrealized()
        {
            var entries = new[]
            {
                MakeEntry("a", LifeGoalTier.Starter, 100_000f),
                MakeEntry("b", LifeGoalTier.Mid, 500_000f),
                MakeEntry("c", LifeGoalTier.Ambitious, 2_000_000f),
            };
            entries[0].MarkRealized(50); // starter realized

            var selection = new LifeGoalSelection(entries);

            var next = selection.NextUnrealized();

            Assert.IsNotNull(next);
            Assert.AreEqual("b", next.goal_id);
        }

        [Test]
        public void NextUnrealized_ReturnsNullWhenAllRealized()
        {
            var entries = new[]
            {
                MakeEntry("a", LifeGoalTier.Starter, 100_000f),
                MakeEntry("b", LifeGoalTier.Mid, 500_000f),
                MakeEntry("c", LifeGoalTier.Ambitious, 2_000_000f),
            };
            entries[0].MarkRealized(50);
            entries[1].MarkRealized(120);
            entries[2].MarkRealized(900);

            var selection = new LifeGoalSelection(entries);

            Assert.IsNull(selection.NextUnrealized());
        }

        [Test]
        public void PreviousRealizedThreshold_ReturnsZeroWhenNoneRealized()
        {
            var entries = new[]
            {
                MakeEntry("a", LifeGoalTier.Starter, 100_000f),
                MakeEntry("b", LifeGoalTier.Mid, 500_000f),
                MakeEntry("c", LifeGoalTier.Ambitious, 2_000_000f),
            };

            var selection = new LifeGoalSelection(entries);

            Assert.AreEqual(0f, selection.PreviousRealizedThreshold());
        }

        [Test]
        public void PreviousRealizedThreshold_ReturnsHighestRealized()
        {
            var entries = new[]
            {
                MakeEntry("a", LifeGoalTier.Starter, 100_000f),
                MakeEntry("b", LifeGoalTier.Mid, 500_000f),
                MakeEntry("c", LifeGoalTier.Ambitious, 2_000_000f),
            };
            entries[0].MarkRealized(50);
            entries[1].MarkRealized(120);

            var selection = new LifeGoalSelection(entries);

            Assert.AreEqual(500_000f, selection.PreviousRealizedThreshold());
        }

        [Test]
        public void IsValidTierComposition_AcceptsOnePerTier()
        {
            var entries = new[]
            {
                MakeEntry("a", LifeGoalTier.Starter, 100_000f),
                MakeEntry("b", LifeGoalTier.Mid, 500_000f),
                MakeEntry("c", LifeGoalTier.Ambitious, 2_000_000f),
            };

            Assert.IsTrue(LifeGoalSelection.IsValidTierComposition(entries));
        }

        [Test]
        public void IsValidTierComposition_RejectsDuplicateTier()
        {
            var entries = new[]
            {
                MakeEntry("a", LifeGoalTier.Starter, 100_000f),
                MakeEntry("b", LifeGoalTier.Starter, 100_000f),
                MakeEntry("c", LifeGoalTier.Ambitious, 2_000_000f),
            };

            Assert.IsFalse(LifeGoalSelection.IsValidTierComposition(entries));
        }

        [Test]
        public void IsValidTierComposition_RejectsMissingTier()
        {
            var entries = new[]
            {
                MakeEntry("a", LifeGoalTier.Starter, 100_000f),
                MakeEntry("b", LifeGoalTier.Mid, 500_000f),
                MakeEntry("c", LifeGoalTier.Mid, 500_000f),
            };

            Assert.IsFalse(LifeGoalSelection.IsValidTierComposition(entries));
        }

        [Test]
        public void IsValidTierComposition_RejectsNullEntries()
        {
            Assert.IsFalse(LifeGoalSelection.IsValidTierComposition(null));
        }

        [Test]
        public void LifeGoalEntry_MarkRealized_SetsFlagsAndDay()
        {
            var entry = MakeEntry("a", LifeGoalTier.Starter, 100_000f);

            Assert.IsFalse(entry.realized);
            Assert.AreEqual(-1, entry.realized_at_day);

            entry.MarkRealized(75);

            Assert.IsTrue(entry.realized);
            Assert.AreEqual(75, entry.realized_at_day);
        }
    }
}
