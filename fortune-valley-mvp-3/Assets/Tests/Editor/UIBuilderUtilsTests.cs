using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace FortuneValley.Tests
{
    [TestFixture]
    public class UIBuilderUtilsTests
    {
        [Test]
        public void DefaultFont_LoadsSuccessfully()
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            Assert.IsNotNull(font, "Default TMP font not found — chat text will be invisible");
        }
    }
}
