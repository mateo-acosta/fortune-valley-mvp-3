using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FortuneValley.Core;

namespace FortuneValley.UI.Panels
{
    /// <summary>
    /// 3-slide rules carousel explaining how the game works.
    /// Shown between the title screen and gameplay start.
    /// </summary>
    public class RulesCarouselPanel : UIPanel
    {
        [Header("Slide Content")]
        [SerializeField] private TextMeshProUGUI _slideTitle;
        [SerializeField] private TextMeshProUGUI _slideBody;

        [Header("Dot Indicators")]
        [SerializeField] private Image[] _dots;

        [Header("Dot Sprites")]
        [SerializeField] private Sprite _dotActive;
        [SerializeField] private Sprite _dotInactive;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private TextMeshProUGUI _nextButtonText;

        [Header("Character Display")]
        [SerializeField] private GameObject _characterDisplayRig;

        [Serializable]
        public struct SlideData
        {
            public string Title;
            [TextArea(3, 6)] public string Body;
        }

        [Header("Slides")]
        [SerializeField] private SlideData[] _slides = new SlideData[]
        {
            new SlideData
            {
                Title = "Your Goal",
                Body = "Buy all 5 city lots before your rival does.\n\n" +
                       "Each lot costs money and gives you a daily income bonus. " +
                       "The player who owns the most lots when all are sold wins!"
            },
            new SlideData
            {
                Title = "How You Earn",
                Body = "Your restaurant earns money every day. Upgrade it to earn more.\n\n" +
                       "Lots you own also generate bonus income. " +
                       "The more lots you have, the faster your money grows."
            },
            new SlideData
            {
                Title = "The Secret Weapon",
                Body = "Invest your money to grow it over time.\n\n" +
                       "Stocks can grow fast but are risky. Bonds are safer but slower. " +
                       "This is compound interest \u2014 your money makes money!\n\n" +
                       "But be careful: your rival won't wait."
            }
        };
        private int _currentSlide;

        private void Awake()
        {
            if (_backButton != null)
                _backButton.onClick.AddListener(GoBack);
            if (_nextButton != null)
                _nextButton.onClick.AddListener(GoNext);
            if (_skipButton != null)
                _skipButton.onClick.AddListener(HandleSkip);
        }

        private void OnEnable()
        {
            GameEvents.OnShowRulesCarousel += Show;
            GameEvents.OnHideRulesCarousel += Hide;
        }

        private void OnDisable()
        {
            GameEvents.OnShowRulesCarousel -= Show;
            GameEvents.OnHideRulesCarousel -= Hide;
        }

        protected override void OnShow()
        {
            _currentSlide = 0;
            RefreshSlide();

            // Show the 3D character rig and replay the wave animation
            if (_characterDisplayRig != null)
            {
                _characterDisplayRig.SetActive(true);
                var animator = _characterDisplayRig.GetComponentInChildren<Animator>();
                if (animator != null)
                    animator.Play("Wave", 0, 0f);
            }
        }

        protected override void OnHide()
        {
            // Hide the 3D character rig when carousel closes
            if (_characterDisplayRig != null)
                _characterDisplayRig.SetActive(false);
        }

        private void GoBack()
        {
            if (_currentSlide > 0)
            {
                _currentSlide--;
                RefreshSlide();
            }
        }

        private void GoNext()
        {
            if (_currentSlide < _slides.Length - 1)
            {
                _currentSlide++;
                RefreshSlide();
            }
            else
            {
                // Last slide — "Let's Go!" was clicked
                GameEvents.RaiseCarouselComplete();
            }
        }

        private void HandleSkip()
        {
            GameEvents.RaiseCarouselComplete();
        }

        private void RefreshSlide()
        {
            // Update text
            if (_slideTitle != null)
                _slideTitle.text = _slides[_currentSlide].Title;
            if (_slideBody != null)
                _slideBody.text = _slides[_currentSlide].Body;

            // Update dots
            if (_dots != null)
            {
                for (int i = 0; i < _dots.Length; i++)
                    UpdateDot(_dots[i], i);
            }

            // Update Back button visibility
            if (_backButton != null)
                _backButton.gameObject.SetActive(_currentSlide > 0);

            // Update Next button text
            if (_nextButtonText != null)
            {
                _nextButtonText.text = _currentSlide == _slides.Length - 1 ? "Let's Go!" : "Next";
            }
        }

        private void UpdateDot(Image dot, int index)
        {
            if (dot == null) return;
            dot.sprite = index == _currentSlide ? _dotActive : _dotInactive;
        }

        private void OnDestroy()
        {
            if (_backButton != null)
                _backButton.onClick.RemoveListener(GoBack);
            if (_nextButton != null)
                _nextButton.onClick.RemoveListener(GoNext);
            if (_skipButton != null)
                _skipButton.onClick.RemoveListener(HandleSkip);
        }
    }
}
