using NUnit.Framework;
using UnityEngine;
using FortuneValley.Domain.Notifications;
using FortuneValley.UI.Notifications;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class BannerSeverityPaletteTests
    {
        private static BannerSeverityPalette MakePalette(params BannerSeverityPalette.Entry[] entries)
        {
            var p = ScriptableObject.CreateInstance<BannerSeverityPalette>();
            // _entries is private; populate via SerializedObject would require Editor.
            // Instead use reflection (the test asmdef has full access via NUnit reflection).
            var field = typeof(BannerSeverityPalette).GetField("_entries",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(p, entries);
            return p;
        }

        private static BannerSeverityPalette.Entry Entry(GuidanceSeverity sev, float duration)
        {
            return new BannerSeverityPalette.Entry
            {
                severity = sev,
                color = Color.white,
                defaultIcon = null,
                durationSeconds = duration
            };
        }

        [Test]
        public void TryGet_ReturnsTrueForKnownSeverity()
        {
            var palette = MakePalette(
                Entry(GuidanceSeverity.Info, 3f),
                Entry(GuidanceSeverity.Critical, 10f));

            Assert.IsTrue(palette.TryGet(GuidanceSeverity.Critical, out var entry));
            Assert.AreEqual(10f, entry.durationSeconds);
        }

        [Test]
        public void TryGet_ReturnsFalseForUnknownSeverity()
        {
            var palette = MakePalette(Entry(GuidanceSeverity.Info, 3f));
            Assert.IsFalse(palette.TryGet(GuidanceSeverity.Critical, out _));
        }

        [Test]
        public void Get_ThrowsForUnknownSeverity()
        {
            var palette = MakePalette(Entry(GuidanceSeverity.Info, 3f));
            Assert.Throws<System.InvalidOperationException>(() => palette.Get(GuidanceSeverity.Critical));
        }

        [Test]
        public void Get_ReturnsFirstMatchingEntry()
        {
            var palette = MakePalette(
                Entry(GuidanceSeverity.Warning, 5f),
                Entry(GuidanceSeverity.Warning, 99f)); // duplicate; first wins
            Assert.AreEqual(5f, palette.Get(GuidanceSeverity.Warning).durationSeconds);
        }

        [Test]
        public void EmptyPalette_TryGet_ReturnsFalse()
        {
            var palette = MakePalette();
            Assert.IsFalse(palette.TryGet(GuidanceSeverity.Info, out _));
        }
    }
}
