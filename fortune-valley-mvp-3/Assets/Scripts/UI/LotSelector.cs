using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using FortuneValley.Domain.Enums;
using FortuneValley.Core;
using FortuneValley.UI.Popups;

namespace FortuneValley.UI
{
    /// <summary>
    /// Handles tap/click selection of lots in the 3D world.
    /// Raycasts to find lots and opens the purchase popup.
    /// </summary>
    public class LotSelector : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════
        // CONFIGURATION
        // ═══════════════════════════════════════════════════════════════

        [Header("Raycast Settings")]
        [Tooltip("Camera used for raycasting (main camera if not set)")]
        [SerializeField] private Camera _camera;

        [Tooltip("Layer mask for lot objects")]
        [SerializeField] private LayerMask _lotLayerMask = ~0;

        [Tooltip("Maximum raycast distance")]
        [SerializeField] private float _maxRayDistance = 100f;

        [Header("References")]
        [SerializeField] private UIManager _uiManager;

        [Header("Visual Feedback")]
        [Tooltip("Material to apply when hovering over a lot")]
        [SerializeField] private Material _hoverMaterial;

        [Tooltip("Material to apply when lot is selected")]
        [SerializeField] private Material _selectedMaterial;

        [Header("Debug")]
        [SerializeField] private bool _debugRaycast = false;

        // ═══════════════════════════════════════════════════════════════
        // RUNTIME STATE
        // ═══════════════════════════════════════════════════════════════

        private LotVisual _hoveredLot;
        private LotVisual _selectedLot;
        private int _currentTick;
        private bool _isEnabled = true;
        // Reusable list to avoid per-frame allocation in IsPointerOverUI
        private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void Start()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_uiManager == null) Debug.LogError("[LotSelector] _uiManager not wired in Inspector.");
        }

        private void OnEnable()
        {
            GameEvents.OnTick += HandleTick;
        }

        private void OnDisable()
        {
            GameEvents.OnTick -= HandleTick;
        }

        private void HandleTick(int tick)
        {
            _currentTick = tick;
        }

        private void Update()
        {
            if (!_isEnabled) return;

            // Don't process if UI is blocking
            if (IsPointerOverUI())
            {
                if (_debugRaycast)
                    UnityEngine.Debug.Log($"[LotSelector] Blocked by UI at {GetPointerPosition()}");
                return;
            }

            HandleHover();
            HandleClick();
        }

        // ═══════════════════════════════════════════════════════════════
        // INPUT HANDLING
        // ═══════════════════════════════════════════════════════════════

        private void HandleHover()
        {
            LotVisual hitLot = RaycastForLot(GetPointerPosition());

            if (hitLot != _hoveredLot)
            {
                // Unhover previous
                if (_hoveredLot != null && _hoveredLot != _selectedLot)
                {
                    _hoveredLot.SetHovered(false);
                }

                // Hover new
                _hoveredLot = hitLot;
                if (_hoveredLot != null && _hoveredLot != _selectedLot)
                {
                    _hoveredLot.SetHovered(true);
                }
            }
        }

        private void HandleClick()
        {
            // Check for tap/click using new Input System
            bool clicked = false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                clicked = true;
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                clicked = true;
            }

            if (clicked)
            {
                LotVisual hitLot = RaycastForLot(GetPointerPosition());

                if (hitLot != null)
                {
                    SelectLot(hitLot);
                }
                else
                {
                    DeselectLot();
                }
            }
        }

        private Vector3 GetPointerPosition()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            return Vector3.zero;
        }

        private bool IsPointerOverUI()
        {
            // Fresh raycast each frame instead of IsPointerOverGameObject(),
            // which has a stale-cache bug with InputSystemUIInputModule —
            // it keeps returning true after UI elements are deactivated.
            if (EventSystem.current == null) return false;

            var eventData = new PointerEventData(EventSystem.current);
            eventData.position = GetPointerPosition();

            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _uiRaycastResults);

            return _uiRaycastResults.Count > 0;
        }

        // ═══════════════════════════════════════════════════════════════
        // RAYCASTING
        // ═══════════════════════════════════════════════════════════════

        private LotVisual RaycastForLot(Vector3 screenPosition)
        {
            if (_camera == null) return null;

            Ray ray = _camera.ScreenPointToRay(screenPosition);

            if (_debugRaycast)
            {
                UnityEngine.Debug.DrawRay(ray.origin, ray.direction * _maxRayDistance, Color.red, 0.1f);
            }

            if (Physics.Raycast(ray, out RaycastHit hit, _maxRayDistance, _lotLayerMask))
            {
                // Look for LotVisual component on hit object or its parents
                LotVisual lotVisual = hit.collider.GetComponentInParent<LotVisual>();

                if (lotVisual != null && _debugRaycast)
                {
                    UnityEngine.Debug.Log($"[LotSelector] Hit lot: {lotVisual.LotDefinition?.DisplayName ?? "Unknown"}");
                }

                return lotVisual;
            }

            if (_debugRaycast)
            {
                UnityEngine.Debug.Log($"[LotSelector] Raycast MISS from {ray.origin} dir {ray.direction}");
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════════
        // SELECTION
        // ═══════════════════════════════════════════════════════════════

        private void SelectLot(LotVisual lot)
        {
            if (lot == null) return;

            // Deselect previous
            if (_selectedLot != null && _selectedLot != lot)
            {
                _selectedLot.SetSelected(false);
            }

            _selectedLot = lot;
            _selectedLot.SetSelected(true);

            // Check if lot can be purchased. LotVisual tracks owner via OnLotPurchased events.
            if (lot.LotDefinition != null)
            {
                Owner owner = lot.CurrentOwner;

                if (owner == Owner.None)
                {
                    // Show purchase popup for available lots
                    ShowPurchasePopup(lot.LotDefinition);
                }
                else
                {
                    // Could show info popup for owned lots
                    UnityEngine.Debug.Log($"[LotSelector] {lot.LotDefinition.DisplayName} is owned by {owner}");
                }
            }
        }

        private void DeselectLot()
        {
            if (_selectedLot != null)
            {
                _selectedLot.SetSelected(false);
                _selectedLot = null;
            }
        }

        private void ShowPurchasePopup(CityLotDefinition lot)
        {
            var popup = _uiManager.LotPurchasePopup as LotPurchasePopup;
            if (popup == null) return;

            // If popup is already visible (rapid clicks), reconfigure without re-pushing to stack
            if (popup.IsVisible)
            {
                popup.ConfigureForLot(lot, _currentTick);
                return;
            }

            popup.ConfigureForLot(lot, _currentTick);
            _uiManager.ShowPopup(popup);
        }

        // ═══════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Enable or disable lot selection.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;

            if (!enabled)
            {
                // Clear hover/selection when disabled
                if (_hoveredLot != null)
                {
                    _hoveredLot.SetHovered(false);
                    _hoveredLot = null;
                }
                DeselectLot();
            }
        }

        /// <summary>
        /// Select a lot programmatically.
        /// </summary>
        public void SelectLotById(string lotId)
        {
            // Find all LotVisuals and match by ID
            var allLots = FindObjectsByType<LotVisual>(FindObjectsSortMode.None);
            foreach (var lot in allLots)
            {
                if (lot.LotDefinition != null && lot.LotDefinition.LotId == lotId)
                {
                    SelectLot(lot);
                    return;
                }
            }
        }
    }
}
