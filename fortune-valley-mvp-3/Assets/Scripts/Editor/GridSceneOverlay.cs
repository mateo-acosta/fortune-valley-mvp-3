using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using FortuneValley.Grid;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Scene view overlay for quick grid editing access.
    /// Provides a toolbar in the scene view for common operations.
    /// </summary>
    [Overlay(typeof(SceneView), "Grid Editor Tools", true)]
    public class GridSceneOverlay : ToolbarOverlay
    {
        public GridSceneOverlay() : base(
            GridToolToggle.id,
            TileTypePicker.id,
            BrushSizePicker.id
        )
        {
        }
    }

    /// <summary>
    /// Toggle for enabling/disabling grid editing mode.
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    public class GridToolToggle : EditorToolbarToggle
    {
        public const string id = "GridEditor/GridToolToggle";

        private static bool _isActive = false;
        public static bool IsActive => _isActive;

        public GridToolToggle()
        {
            text = "Grid Edit";
            tooltip = "Toggle grid editing mode";
            value = _isActive;

            this.RegisterValueChangedCallback(evt =>
            {
                _isActive = evt.newValue;
                SceneView.RepaintAll();
            });
        }
    }

    /// <summary>
    /// Dropdown for selecting tile type to paint.
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    public class TileTypePicker : EditorToolbarDropdown
    {
        public const string id = "GridEditor/TileTypePicker";

        private static TileType _selectedType = TileType.Road;
        public static TileType SelectedType => _selectedType;

        public TileTypePicker()
        {
            text = _selectedType.ToString();
            tooltip = "Select tile type to paint";

            clicked += ShowDropdown;
        }

        private void ShowDropdown()
        {
            var menu = new GenericMenu();

            foreach (TileType type in System.Enum.GetValues(typeof(TileType)))
            {
                bool isSelected = type == _selectedType;
                menu.AddItem(new GUIContent(type.ToString()), isSelected, () =>
                {
                    _selectedType = type;
                    text = type.ToString();
                });
            }

            menu.ShowAsContext();
        }
    }

    /// <summary>
    /// Dropdown for selecting brush size.
    /// </summary>
    [EditorToolbarElement(id, typeof(SceneView))]
    public class BrushSizePicker : EditorToolbarDropdown
    {
        public const string id = "GridEditor/BrushSizePicker";

        private static int _brushSize = 1;
        public static int BrushSize => _brushSize;

        private static readonly int[] Sizes = { 1, 3, 5 };

        public BrushSizePicker()
        {
            text = $"{_brushSize}x{_brushSize}";
            tooltip = "Select brush size";

            clicked += ShowDropdown;
        }

        private void ShowDropdown()
        {
            var menu = new GenericMenu();

            foreach (int size in Sizes)
            {
                bool isSelected = size == _brushSize;
                menu.AddItem(new GUIContent($"{size}x{size}"), isSelected, () =>
                {
                    _brushSize = size;
                    text = $"{size}x{size}";
                });
            }

            menu.ShowAsContext();
        }
    }
}
