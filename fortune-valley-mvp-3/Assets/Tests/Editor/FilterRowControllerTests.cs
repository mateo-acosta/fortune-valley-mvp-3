using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using FortuneValley.UI.Components;

namespace FortuneValley.Tests
{
    /// <summary>
    /// EditMode unit tests for FilterRowController.
    /// Creates test GameObjects with Button children to verify selection logic.
    /// </summary>
    [TestFixture]
    public class FilterRowControllerTests
    {
        private GameObject _root;
        private FilterRowController _controller;
        private Button[] _buttons;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("FilterRow");
            _controller = _root.AddComponent<FilterRowController>();

            // Create 3 buttons: All, Option1, Option2
            _buttons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                var btnGo = new GameObject($"Button_{i}");
                btnGo.transform.SetParent(_root.transform);
                var image = btnGo.AddComponent<Image>();
                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = image;
                _buttons[i] = btn;
            }

            // Wire buttons via reflection (simulating Inspector wiring)
            var type = typeof(FilterRowController);
            var flags = System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance;
            type.GetField("_filterButtons", flags)?.SetValue(_controller, _buttons);
            type.GetField("_normalColor", flags)?.SetValue(
                _controller, Color.white);
            type.GetField("_selectedColor", flags)?.SetValue(
                _controller, Color.blue);

            // Call Awake via reflection (SendMessage triggers Unity assertions in EditMode)
            var awakeMethod = type.GetMethod("Awake", flags);
            awakeMethod?.Invoke(_controller, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void DefaultSelection_IsZero()
        {
            Assert.AreEqual(0, _controller.SelectedIndex);
        }

        [Test]
        public void ClickButton_SelectsIt()
        {
            int receivedIndex = -1;
            _controller.OnSelectionChanged += idx => receivedIndex = idx;

            // Simulate clicking button at index 1
            _buttons[1].onClick.Invoke();

            Assert.AreEqual(1, _controller.SelectedIndex);
            Assert.AreEqual(1, receivedIndex);
        }

        [Test]
        public void ClickSelectedButton_DeselectsToAll()
        {
            // First select button 2
            _buttons[2].onClick.Invoke();
            Assert.AreEqual(2, _controller.SelectedIndex);

            int receivedIndex = -1;
            _controller.OnSelectionChanged += idx => receivedIndex = idx;

            // Click button 2 again -- should deselect to 0 (All)
            _buttons[2].onClick.Invoke();

            Assert.AreEqual(0, _controller.SelectedIndex);
            Assert.AreEqual(0, receivedIndex);
        }

        [Test]
        public void ClickAll_WhenAlreadyAll_StaysAtAll()
        {
            int fireCount = 0;
            _controller.OnSelectionChanged += idx => fireCount++;

            // Click "All" when already at All -- should select 0, fire event
            _buttons[0].onClick.Invoke();

            Assert.AreEqual(0, _controller.SelectedIndex);
            // Event still fires (index 0 clicked = selects 0)
            Assert.AreEqual(1, fireCount);
        }

        [Test]
        public void ResetToAll_ResetsIndex_DoesNotFireEvent()
        {
            _buttons[1].onClick.Invoke();
            Assert.AreEqual(1, _controller.SelectedIndex);

            int fireCount = 0;
            _controller.OnSelectionChanged += idx => fireCount++;

            _controller.ResetToAll();

            Assert.AreEqual(0, _controller.SelectedIndex);
            Assert.AreEqual(0, fireCount); // No event fired
        }

        [Test]
        public void Select_ProgrammaticSelect_FiresEvent()
        {
            int receivedIndex = -1;
            _controller.OnSelectionChanged += idx => receivedIndex = idx;

            _controller.Select(2);

            Assert.AreEqual(2, _controller.SelectedIndex);
            Assert.AreEqual(2, receivedIndex);
        }

        [Test]
        public void Select_OutOfRange_DoesNothing()
        {
            _buttons[1].onClick.Invoke();
            Assert.AreEqual(1, _controller.SelectedIndex);

            _controller.Select(99);

            Assert.AreEqual(1, _controller.SelectedIndex); // Unchanged
        }

        [Test]
        public void VisualState_SelectedButtonGetsSelectedColor()
        {
            _buttons[1].onClick.Invoke();

            var selectedImage = _buttons[1].targetGraphic as Image;
            var unselectedImage = _buttons[0].targetGraphic as Image;

            Assert.AreEqual(Color.blue, selectedImage.color);
            Assert.AreEqual(Color.white, unselectedImage.color);
        }
    }
}
