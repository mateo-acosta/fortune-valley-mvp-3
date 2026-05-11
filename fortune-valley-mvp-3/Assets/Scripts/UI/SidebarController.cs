using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FortuneValley.UI
{
    /// <summary>
    /// Generic sidebar controller for tabbed panels.
    /// Wire the sidebar buttons and their matching sub-panels
    /// in the Inspector. Clicking a button shows its sub-panel
    /// and hides all others.
    ///
    /// All colors are set in the Inspector. No runtime color reading.
    /// </summary>
    public class SidebarController : MonoBehaviour
    {
        [Header("Sidebar")]
        [Tooltip("Sidebar buttons in order. Index must match _subPanels.")]
        [SerializeField] private Button[] _sidebarButtons;

        [Tooltip("Sub-panels in order. Index must match _sidebarButtons.")]
        [SerializeField] private GameObject[] _subPanels;

        [Tooltip("Which sub-panel to show by default (0-based index)")]
        [SerializeField] private int _defaultIndex;

        [Header("Button Colors")]
        [Tooltip("Background color for the selected button")]
        [SerializeField] private Color _activeButtonColor = new Color(0.28f, 0.32f, 0.60f, 1f);

        [Tooltip("Background color for unselected buttons")]
        [SerializeField] private Color _inactiveButtonColor = Color.white;

        [Header("Text Colors")]
        [Tooltip("Text color for the selected button")]
        [SerializeField] private Color _activeTextColor = Color.white;

        [Tooltip("Text color for unselected buttons")]
        [SerializeField] private Color _inactiveTextColor = new Color(0.3f, 0.3f, 0.4f, 1f);

        // Cached references
        private Image[] _buttonImages;
        private TextMeshProUGUI[] _buttonTexts;
        private int _activeIndex = -1;
        private bool _initialized;

        // One-shot override so callers can open the panel on a non-default tab
        // (e.g. "buy but can't afford" routes to Explore). Cleared after use.
        private int _pendingInitialOverride = -1;

        /// <summary>
        /// Sets the tab to show on the next initialization/open instead of the default.
        /// Consumed once by Start() or OnEnable(), whichever fires first.
        /// </summary>
        public void SetInitialIndexOverride(int index)
        {
            _pendingInitialOverride = index;
        }

        private void Start()
        {
            CacheButtonImages();
            DebugCachedRefs();
            WireButtons();
            SwitchTo(ResolveStartIndex());
            _initialized = true;
        }

        private void OnEnable()
        {
            // Reset to the default tab each time the panel is opened, unless an override is pending.
            if (_initialized)
                SwitchTo(ResolveStartIndex());
        }

        private int ResolveStartIndex()
        {
            int index = _pendingInitialOverride >= 0 ? _pendingInitialOverride : _defaultIndex;
            _pendingInitialOverride = -1;
            return index;
        }

        /// <summary>
        /// Show a sub-panel by index and hide all others.
        /// Can be called externally to navigate programmatically.
        /// </summary>
        public void SwitchTo(int index)
        {
            if (_subPanels == null || index < 0 || index >= _subPanels.Length)
                return;

            _activeIndex = index;

            for (int i = 0; i < _subPanels.Length; i++)
            {
                if (_subPanels[i] != null)
                    _subPanels[i].SetActive(i == index);
            }

            UpdateButtonColors();
        }

        private void UpdateButtonColors()
        {
            if (_buttonImages == null) return;

            for (int i = 0; i < _buttonImages.Length; i++)
            {
                bool isActive = (i == _activeIndex);

                if (_buttonImages[i] != null)
                    _buttonImages[i].color = isActive ? _activeButtonColor : _inactiveButtonColor;

                if (_buttonTexts != null && _buttonTexts[i] != null)
                    _buttonTexts[i].color = isActive ? _activeTextColor : _inactiveTextColor;
            }
        }

        private void CacheButtonImages()
        {
            if (_sidebarButtons == null) return;

            _buttonImages = new Image[_sidebarButtons.Length];
            _buttonTexts = new TextMeshProUGUI[_sidebarButtons.Length];

            for (int i = 0; i < _sidebarButtons.Length; i++)
            {
                if (_sidebarButtons[i] == null) continue;

                _buttonImages[i] = _sidebarButtons[i].GetComponent<Image>();
                _buttonTexts[i] = _sidebarButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void DebugCachedRefs()
        {
            if (_sidebarButtons == null)
            {
                Debug.Log($"[SidebarController] {gameObject.name}: _sidebarButtons is NULL");
                return;
            }

            Debug.Log($"[SidebarController] {gameObject.name}: {_sidebarButtons.Length} buttons");

            for (int i = 0; i < _sidebarButtons.Length; i++)
            {
                string btnName = _sidebarButtons[i] != null ? _sidebarButtons[i].name : "NULL";
                bool hasImage = _buttonImages != null && _buttonImages[i] != null;
                bool hasText = _buttonTexts != null && _buttonTexts[i] != null;
                string textContent = hasText ? $"'{_buttonTexts[i].text}' color={_buttonTexts[i].color}" : "NO TEXT FOUND";

                Debug.Log($"[SidebarController] Button[{i}] '{btnName}' | Image={hasImage} | Text={textContent}");

                // Check all TMP children
                if (_sidebarButtons[i] != null)
                {
                    var allTexts = _sidebarButtons[i].GetComponentsInChildren<TextMeshProUGUI>(true);
                    Debug.Log($"[SidebarController] Button[{i}] has {allTexts.Length} TMP children total");
                    for (int j = 0; j < allTexts.Length; j++)
                    {
                        Debug.Log($"[SidebarController]   TMP[{j}]: '{allTexts[j].text}' on GO '{allTexts[j].gameObject.name}' color={allTexts[j].color} alpha={allTexts[j].color.a}");
                    }
                }
            }

            Debug.Log($"[SidebarController] Colors: activeBtn={_activeButtonColor} inactiveBtn={_inactiveButtonColor} activeTxt={_activeTextColor} inactiveTxt={_inactiveTextColor}");
        }

        private void WireButtons()
        {
            if (_sidebarButtons == null) return;

            for (int i = 0; i < _sidebarButtons.Length; i++)
            {
                if (_sidebarButtons[i] == null) continue;

                int capturedIndex = i;
                _sidebarButtons[i].onClick.AddListener(() => SwitchTo(capturedIndex));
            }
        }
    }
}
