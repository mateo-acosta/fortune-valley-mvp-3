using UnityEngine;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;
using FortuneValley.City;

namespace FortuneValley.UI
{
    /// <summary>
    /// Component attached to lot GameObjects in the 3D world.
    /// Links the visual representation to the lot definition data.
    /// Handles visual feedback for hover, selection, and ownership states.
    /// </summary>
    public class LotVisual : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Lot Reference")]
        [Tooltip("The lot definition this visual represents")]
        [SerializeField] private CityLotDefinition _lotDefinition;

        [Header("Visual Elements")]
        [Tooltip("Renderer to modify for visual feedback")]
        [SerializeField] private Renderer _mainRenderer;

        [Tooltip("Outline effect object (enable/disable for selection)")]
        [SerializeField] private GameObject _outlineEffect;

        [Tooltip("Ownership indicator (changes color based on owner)")]
        [SerializeField] private Renderer _ownerIndicator;

        [Header("Colors")]
        [SerializeField] private Color _availableColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color _playerOwnedColor = new Color(0.1f, 0.9f, 0.2f, 0.8f);
        [SerializeField] private Color _rivalOwnedColor = new Color(0.9f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color _hoverColor = new Color(1f, 0.9f, 0.3f, 0.6f);
        [SerializeField] private Color _selectedColor = new Color(1f, 1f, 0f, 0.8f);

        [Header("Materials")]
        [Tooltip("Material to use when hovered")]
        [SerializeField] private Material _hoverMaterial;

        [Header("Rival Targeting")]
        [Tooltip("Visual effect when rival is targeting this lot")]
        [SerializeField] private GameObject _rivalTargetEffect;
        [Tooltip("Color for rival targeting outline")]
        [SerializeField] private Color _rivalTargetColor = new Color(1f, 0.2f, 0.2f, 0.8f);
        [Tooltip("Pulse speed when targeted")]
        [SerializeField] private float _targetPulseSpeed = 2f;

        [Header("Ownership Flags")]
        [Tooltip("Flag/banner showing ownership")]
        [SerializeField] private GameObject _ownershipFlag;
        [Tooltip("Renderer for the flag to set color")]
        [SerializeField] private Renderer _flagRenderer;
        [Tooltip("For Sale sign shown on available lots")]
        [SerializeField] private GameObject _forSaleSign;
        [Tooltip("Icon or mesh for player ownership")]
        [SerializeField] private GameObject _playerIcon;
        [Tooltip("Icon or mesh for rival ownership")]
        [SerializeField] private GameObject _rivalIcon;

        [Tooltip("Original material (cached on Start)")]
        private Material _originalMaterial;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private bool _isHovered;
        private bool _isSelected;
        private bool _isRivalTarget;
        private Owner _currentOwner = Owner.None;
        private float _targetPulseTimer;
        private int _daysUntilRivalPurchase;
        private LotEdgeGlow _edgeGlow;

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC ACCESSORS
        // ═══════════════════════════════════════════════════════════════

        public CityLotDefinition LotDefinition => _lotDefinition;
        public bool IsHovered => _isHovered;
        public bool IsSelected => _isSelected;
        public bool IsRivalTarget => _isRivalTarget;
        public Owner CurrentOwner => _currentOwner;

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Auto-wire renderer if not assigned in Inspector
            if (_mainRenderer == null)
            {
                _mainRenderer = GetComponent<MeshRenderer>();
            }

            // Cache original material
            if (_mainRenderer != null)
            {
                _originalMaterial = _mainRenderer.material;
            }

            // Auto-add edge glow if not already present
            _edgeGlow = GetComponent<LotEdgeGlow>();
            if (_edgeGlow == null)
            {
                _edgeGlow = gameObject.AddComponent<LotEdgeGlow>();
            }

            // Hide outline initially
            if (_outlineEffect != null)
            {
                _outlineEffect.SetActive(false);
            }

            // Hide rival target effect initially
            if (_rivalTargetEffect != null)
            {
                _rivalTargetEffect.SetActive(false);
            }

            // Initialize ownership flags
            InitializeOwnershipFlags();
        }

        private void InitializeOwnershipFlags()
        {
            // Hide all ownership indicators initially
            if (_ownershipFlag != null)
            {
                _ownershipFlag.SetActive(false);
            }
            if (_playerIcon != null)
            {
                _playerIcon.SetActive(false);
            }
            if (_rivalIcon != null)
            {
                _rivalIcon.SetActive(false);
            }
            // Show "For Sale" sign on available lots
            if (_forSaleSign != null)
            {
                _forSaleSign.SetActive(true);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnLotPurchased += HandleLotPurchased;
            GameEvents.OnRivalTargetChanged += HandleRivalTargetChanged;
            GameEvents.OnRivalPurchasedLot += HandleRivalPurchasedLot;
            GameEvents.OnGameStart += HandleGameStart;
        }

        private void OnDisable()
        {
            GameEvents.OnLotPurchased -= HandleLotPurchased;
            GameEvents.OnRivalTargetChanged -= HandleRivalTargetChanged;
            GameEvents.OnRivalPurchasedLot -= HandleRivalPurchasedLot;
            GameEvents.OnGameStart -= HandleGameStart;
        }

        private void Update()
        {
            // Pulse effect when rival is targeting this lot
            if (_isRivalTarget && _rivalTargetEffect != null)
            {
                _targetPulseTimer += Time.deltaTime * _targetPulseSpeed;

                // Increase intensity as deadline approaches
                float urgency = 1f - Mathf.Clamp01(_daysUntilRivalPurchase / 10f);
                float pulse = (Mathf.Sin(_targetPulseTimer * Mathf.PI) + 1f) / 2f;
                float intensity = 0.5f + (0.5f * urgency);

                // Scale the effect based on urgency
                _rivalTargetEffect.transform.localScale = Vector3.one * (1f + (pulse * 0.2f * intensity));
            }
        }

        private void Start()
        {
            UpdateVisuals();
        }

        // ═══════════════════════════════════════════════════════════════
        // EVENT HANDLERS
        // ═══════════════════════════════════════════════════════════════

        private void HandleLotPurchased(string lotId, Owner owner)
        {
            if (_lotDefinition != null && _lotDefinition.LotId == lotId)
            {
                _currentOwner = owner;
                _isRivalTarget = false; // No longer a target once purchased
                UpdateVisuals();
            }
        }

        private void HandleRivalTargetChanged(string lotId, int daysUntil)
        {
            if (_lotDefinition == null) return;

            bool wasTarget = _isRivalTarget;
            _isRivalTarget = (_lotDefinition.LotId == lotId);
            _daysUntilRivalPurchase = daysUntil;

            if (_isRivalTarget != wasTarget)
            {
                UpdateRivalTargetVisual();
            }
        }

        private void HandleRivalPurchasedLot(string lotId)
        {
            if (_lotDefinition != null && _lotDefinition.LotId == lotId)
            {
                _isRivalTarget = false;
                UpdateRivalTargetVisual();
            }
        }

        private void HandleGameStart()
        {
            if (GameEvents.SaveStateRestoredFromServer) return;
            // Reset all local ownership state so visuals reflect a fresh game
            _currentOwner = Owner.None;
            _isRivalTarget = false;
            _daysUntilRivalPurchase = 0;
            UpdateVisuals();
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Set the lot definition for this visual.
        /// </summary>
        public void SetLotDefinition(CityLotDefinition definition)
        {
            _lotDefinition = definition;
            UpdateVisuals();
        }

        /// <summary>
        /// Set hover state.
        /// </summary>
        public void SetHovered(bool hovered)
        {
            if (_isHovered == hovered) return;

            _isHovered = hovered;
            UpdateVisuals();
        }

        /// <summary>
        /// Set selected state.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;

            _isSelected = selected;
            UpdateVisuals();
        }

        /// <summary>
        /// Update ownership state from CityManager.
        /// </summary>
        public void RefreshOwnership(CityManager cityManager)
        {
            if (_lotDefinition == null || cityManager == null) return;

            _currentOwner = cityManager.GetOwner(_lotDefinition.LotId);
            UpdateVisuals();
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════

        private void UpdateVisuals()
        {
            UpdateOutline();
            UpdateOwnerIndicator();
            UpdateMaterial();
            UpdateRivalTargetVisual();
            UpdateOwnershipFlags();
        }

        private void UpdateOwnershipFlags()
        {
            // Update "For Sale" sign visibility
            if (_forSaleSign != null)
            {
                _forSaleSign.SetActive(_currentOwner == Owner.None);
            }

            // Update ownership flag
            bool showFlag = _currentOwner != Owner.None;
            if (_ownershipFlag != null)
            {
                _ownershipFlag.SetActive(showFlag);
            }

            // Update flag color based on owner
            if (_flagRenderer != null && showFlag)
            {
                Color flagColor = _currentOwner == Owner.Player ? _playerOwnedColor : _rivalOwnedColor;
                _flagRenderer.material.color = flagColor;
            }

            // Update owner-specific icons
            if (_playerIcon != null)
            {
                _playerIcon.SetActive(_currentOwner == Owner.Player);
            }
            if (_rivalIcon != null)
            {
                _rivalIcon.SetActive(_currentOwner == Owner.Rival);
            }
        }

        private void UpdateRivalTargetVisual()
        {
            if (_rivalTargetEffect != null)
            {
                // Only show target effect on available (unowned) lots
                bool shouldShow = _isRivalTarget && _currentOwner == Owner.None;
                _rivalTargetEffect.SetActive(shouldShow);

                if (shouldShow)
                {
                    _targetPulseTimer = 0f;
                }
            }
        }

        private void UpdateOutline()
        {
            if (_outlineEffect != null)
            {
                _outlineEffect.SetActive(_isSelected || _isHovered);
            }

            // Propagate ownership state to the edge glow
            if (_edgeGlow != null)
            {
                _edgeGlow.SetOwnershipColor(_currentOwner);
            }
        }

        private void UpdateOwnerIndicator()
        {
            if (_ownerIndicator == null) return;

            Color color;
            switch (_currentOwner)
            {
                case Owner.Player:
                    color = _playerOwnedColor;
                    break;
                case Owner.Rival:
                    color = _rivalOwnedColor;
                    break;
                default:
                    color = _availableColor;
                    break;
            }

            // Override with hover/selected colors
            if (_isSelected)
            {
                color = _selectedColor;
            }
            else if (_isHovered)
            {
                color = _hoverColor;
            }

            _ownerIndicator.material.color = color;
        }

        private void UpdateMaterial()
        {
            if (_mainRenderer == null) return;

            if (_isHovered && _hoverMaterial != null)
            {
                _mainRenderer.material = _hoverMaterial;
            }
            else if (_originalMaterial != null)
            {
                _mainRenderer.material = _originalMaterial;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // EDITOR
        // ═══════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_lotDefinition == null) return;

            // Draw label with lot info
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2f,
                $"{_lotDefinition.DisplayName}\n${_lotDefinition.BaseCost:N0}"
            );
        }
#endif
    }
}
