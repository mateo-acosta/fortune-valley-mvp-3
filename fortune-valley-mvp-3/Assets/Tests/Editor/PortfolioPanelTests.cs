using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode tests for PortfolioPanel quantity-button color tinting logic.
    /// Tests the color math directly without needing the full panel hierarchy.
    /// </summary>
    [TestFixture]
    public class PortfolioPanelTests
    {
        private List<GameObject> _created = new List<GameObject>();
        private const float COLOR_TOLERANCE = 0.001f;

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
                Object.DestroyImmediate(go);
            _created.Clear();
        }

        private Image CreateButtonImage(Color normalColor)
        {
            var go = new GameObject("QtyButton");
            _created.Add(go);
            var img = go.AddComponent<Image>();
            img.color = normalColor;
            return img;
        }

        /// <summary>
        /// Mirrors PortfolioPanel.UpdateQuantityButtonColors logic.
        /// </summary>
        private void ApplyTint(List<Image> images, bool isBuying,
            Color buyNormalColor, Color sellNormalColor)
        {
            Color baseColor = isBuying ? buyNormalColor : sellNormalColor;
            Color tint = Color.Lerp(baseColor, Color.white, 0.3f);

            for (int i = 0; i < images.Count; i++)
            {
                if (images[i] != null)
                    images[i].color = tint;
            }
        }

        /// <summary>
        /// Mirrors PortfolioPanel.RestoreQuantityButtonColors logic.
        /// </summary>
        private void RestoreColors(List<Image> images, List<Color> normalColors)
        {
            for (int i = 0; i < images.Count; i++)
            {
                if (i < normalColors.Count && images[i] != null)
                    images[i].color = normalColors[i];
            }
        }

        private void AssertColorEqual(Color expected, Color actual, string message = "")
        {
            Assert.AreEqual(expected.r, actual.r, COLOR_TOLERANCE, $"{message} Red");
            Assert.AreEqual(expected.g, actual.g, COLOR_TOLERANCE, $"{message} Green");
            Assert.AreEqual(expected.b, actual.b, COLOR_TOLERANCE, $"{message} Blue");
            Assert.AreEqual(expected.a, actual.a, COLOR_TOLERANCE, $"{message} Alpha");
        }

        [Test]
        public void BuyMode_TintsQuantityButtons_WithLightenedBuyColor()
        {
            // Scene-like buy button color (greenish)
            var buyNormal = new Color(0.2f, 0.6f, 0.2f, 1f);
            var sellNormal = new Color(0.6f, 0.2f, 0.2f, 1f);
            Color expectedTint = Color.Lerp(buyNormal, Color.white, 0.3f);

            var images = new List<Image>();
            for (int i = 0; i < 4; i++)
                images.Add(CreateButtonImage(Color.gray));

            ApplyTint(images, isBuying: true, buyNormal, sellNormal);

            for (int i = 0; i < 4; i++)
                AssertColorEqual(expectedTint, images[i].color, $"Button {i}");
        }

        [Test]
        public void SellMode_TintsQuantityButtons_WithLightenedSellColor()
        {
            var buyNormal = new Color(0.2f, 0.6f, 0.2f, 1f);
            var sellNormal = new Color(0.6f, 0.2f, 0.2f, 1f);
            Color expectedTint = Color.Lerp(sellNormal, Color.white, 0.3f);

            var images = new List<Image>();
            for (int i = 0; i < 4; i++)
                images.Add(CreateButtonImage(Color.gray));

            ApplyTint(images, isBuying: false, buyNormal, sellNormal);

            for (int i = 0; i < 4; i++)
                AssertColorEqual(expectedTint, images[i].color, $"Button {i}");
        }

        [Test]
        public void Clear_RestoresOriginalColors()
        {
            var normalColors = new List<Color>
            {
                new Color(0.5f, 0.5f, 0.5f, 1f),
                new Color(0.6f, 0.6f, 0.6f, 1f),
                new Color(0.7f, 0.7f, 0.7f, 1f),
                new Color(0.8f, 0.8f, 0.8f, 1f),
            };

            var images = new List<Image>();
            for (int i = 0; i < 4; i++)
                images.Add(CreateButtonImage(normalColors[i]));

            // Tint to some other color first
            ApplyTint(images, isBuying: true,
                new Color(0.2f, 0.6f, 0.2f, 1f),
                new Color(0.6f, 0.2f, 0.2f, 1f));

            // Verify they changed
            for (int i = 0; i < 4; i++)
                Assert.AreNotEqual(normalColors[i], images[i].color);

            // Restore
            RestoreColors(images, normalColors);

            // Verify restored
            for (int i = 0; i < 4; i++)
                AssertColorEqual(normalColors[i], images[i].color, $"Button {i}");
        }
    }
}
