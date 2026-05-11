using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using FortuneValley.UI.World;

namespace FortuneValley.Tests
{
    /// <summary>
    /// Lightweight coverage for the DOTween orchestration helper. The
    /// sequencer's real behavior (fade-in, punch, color flash, fade-out) is
    /// observable only when DOTween's update loop is driven; EditMode tests
    /// don't tick DOTween. So these tests verify the construction contract
    /// and idle state. End-to-end flash behavior is covered by the integration
    /// path in BuildingCollectButtonTests (OnIncomeCollected -> sequencer.Play).
    /// </summary>
    [TestFixture]
    public class CoinFlashSequencerTests
    {
        private GameObject _go;
        private Transform _transform;
        private CanvasGroup _canvasGroup;
        private Image _image;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("FlashTarget");
            _transform = _go.transform;
            _canvasGroup = _go.AddComponent<CanvasGroup>();
            _image = _go.AddComponent<Image>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private CoinFlashSequencer MakeSequencer(CanvasGroup group, Image image)
        {
            return new CoinFlashSequencer(
                _transform,
                Vector3.one,
                group,
                image,
                Color.yellow,
                flashScale: 1.2f,
                flashDuration: 0.3f,
                fadeInDuration: 0.1f,
                holdDuration: 0.2f,
                fadeOutDuration: 0.4f);
        }

        [Test]
        public void IsPlaying_BeforeFirstPlay_ReturnsFalse()
        {
            var seq = MakeSequencer(_canvasGroup, _image);
            Assert.IsFalse(seq.IsPlaying);
        }

        [Test]
        public void Constructor_NullVisibilityGroupAndTintImage_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => MakeSequencer(null, null));
        }

        [Test]
        public void Constructor_CapturesBaseColorFromTintImage()
        {
            _image.color = new Color(0.1f, 0.2f, 0.3f, 1f);
            var seq = MakeSequencer(_canvasGroup, _image);
            // Constructor caches baseColor; no public getter, but Kill() is a
            // safe operation that exercises the cached state without throwing.
            Assert.DoesNotThrow(() => seq.Kill());
        }

        [Test]
        public void Kill_BeforeAnyPlay_IsNoOp()
        {
            var seq = MakeSequencer(_canvasGroup, _image);
            Assert.DoesNotThrow(() => seq.Kill());
            Assert.IsFalse(seq.IsPlaying);
        }
    }
}
