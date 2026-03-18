using UnityEngine;
using UnityEditor;
using FortuneValley.Grid;

namespace FortuneValley.Editor
{
    /// <summary>
    /// Main editor window for designing the city grid.
    /// Provides tools for painting tile types and placing assets.
    /// </summary>
    public class GridEditorWindow : EditorWindow
    {
        // ═══════════════════════════════════════════════════════════════
        // WINDOW STATE
        // ═══════════════════════════════════════════════════════════════

        private GridMapData _mapData;
        private IsometricGridConfig _config;

        // Tool state
        private TileType _selectedTileType = TileType.Road;
        private int _brushSize = 1;
        private bool _showLabels = true;
        private bool _isPainting = false;
        private bool _showPlacedAssets = true;
        private EditTool _currentTool = EditTool.Paint;

        // View state
        private Vector2 _scrollPosition;
        private Vector2Int _hoveredTile = new Vector2Int(-1, -1);
        private Vector2Int _selectedTile = new Vector2Int(-1, -1);

        // Styles
        private GUIStyle _tileButtonStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInitialized = false;

        // Tool modes
        private enum EditTool
        {
            Paint,      // Paint tile types
            Fill,       // Flood fill
            Line,       // Draw lines
            Rectangle   // Draw rectangles
        }

        // Line/rectangle tool state
        private Vector2Int _lineStart = new Vector2Int(-1, -1);
        private bool _isDrawingLine = false;

        // ═══════════════════════════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════════════════════════

        [MenuItem("Window/Fortune Valley/Grid Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<GridEditorWindow>();
            window.titleContent = new GUIContent("Grid Editor");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        // ═══════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= Repaint;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _tileButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedWidth = 60,
                fixedHeight = 30
            };

            _labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _stylesInitialized = true;
        }

        // ═══════════════════════════════════════════════════════════════
        // WINDOW GUI
        // ═══════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(5);

            // Data references
            DrawDataSection();

            if (_mapData == null || _config == null)
            {
                EditorGUILayout.HelpBox("Assign a Grid Map Data and Grid Config to begin editing.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(10);

            // Tile type toolbar
            DrawTileTypeToolbar();

            EditorGUILayout.Space(5);

            // Brush settings
            DrawBrushSettings();

            EditorGUILayout.Space(5);

            // View settings
            DrawViewSettings();

            EditorGUILayout.Space(10);

            // Actions
            DrawActions();

            EditorGUILayout.Space(10);

            // Status bar
            DrawStatusBar();

            // Grid preview (optional inline view)
            EditorGUILayout.Space(10);
            DrawGridPreview();
        }

        private void DrawDataSection()
        {
            EditorGUILayout.LabelField("Data References", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _mapData = (GridMapData)EditorGUILayout.ObjectField(
                "Map Data",
                _mapData,
                typeof(GridMapData),
                false
            );

            _config = (IsometricGridConfig)EditorGUILayout.ObjectField(
                "Grid Config",
                _config,
                typeof(IsometricGridConfig),
                false
            );

            if (EditorGUI.EndChangeCheck())
            {
                // Auto-assign config from map data if available
                if (_mapData != null && _mapData.Config != null && _config == null)
                {
                    _config = _mapData.Config;
                }

                SceneView.RepaintAll();
            }

            // Quick create buttons
            EditorGUILayout.BeginHorizontal();
            if (_mapData == null && GUILayout.Button("Create New Map", GUILayout.Height(25)))
            {
                CreateNewMapData();
            }
            if (_config == null && GUILayout.Button("Create New Config", GUILayout.Height(25)))
            {
                CreateNewConfig();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTileTypeToolbar()
        {
            EditorGUILayout.LabelField("Tile Type", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            var tileTypes = System.Enum.GetValues(typeof(TileType));
            foreach (TileType type in tileTypes)
            {
                Color originalColor = GUI.backgroundColor;
                if (_selectedTileType == type)
                {
                    GUI.backgroundColor = Color.yellow;
                }
                else
                {
                    GUI.backgroundColor = _config.GetTileColor(type);
                }

                if (GUILayout.Button(_config.GetTileLabel(type), GUILayout.Width(50), GUILayout.Height(30)))
                {
                    _selectedTileType = type;
                }

                GUI.backgroundColor = originalColor;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBrushSettings()
        {
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

            // Tool selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Mode:", GUILayout.Width(40));

            if (GUILayout.Toggle(_currentTool == EditTool.Paint, "Paint", EditorStyles.miniButtonLeft, GUILayout.Width(50)))
                _currentTool = EditTool.Paint;
            if (GUILayout.Toggle(_currentTool == EditTool.Fill, "Fill", EditorStyles.miniButtonMid, GUILayout.Width(40)))
                _currentTool = EditTool.Fill;
            if (GUILayout.Toggle(_currentTool == EditTool.Line, "Line", EditorStyles.miniButtonMid, GUILayout.Width(40)))
                _currentTool = EditTool.Line;
            if (GUILayout.Toggle(_currentTool == EditTool.Rectangle, "Rect", EditorStyles.miniButtonRight, GUILayout.Width(40)))
                _currentTool = EditTool.Rectangle;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Brush size (only for paint mode)
            if (_currentTool == EditTool.Paint || _currentTool == EditTool.Line)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Size:", GUILayout.Width(40));

                if (GUILayout.Toggle(_brushSize == 1, "1x1", EditorStyles.miniButtonLeft, GUILayout.Width(40)))
                    _brushSize = 1;
                if (GUILayout.Toggle(_brushSize == 3, "3x3", EditorStyles.miniButtonMid, GUILayout.Width(40)))
                    _brushSize = 3;
                if (GUILayout.Toggle(_brushSize == 5, "5x5", EditorStyles.miniButtonRight, GUILayout.Width(40)))
                    _brushSize = 5;

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            // Tool-specific hints
            EditorGUILayout.HelpBox(GetToolHint(), MessageType.None);
        }

        private string GetToolHint()
        {
            return _currentTool switch
            {
                EditTool.Paint => "Click and drag to paint tiles",
                EditTool.Fill => "Click to flood fill connected tiles",
                EditTool.Line => "Click start, then click end to draw line",
                EditTool.Rectangle => "Click corner, then click opposite corner",
                _ => ""
            };
        }

        private void DrawViewSettings()
        {
            EditorGUILayout.LabelField("View", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _showLabels = EditorGUILayout.Toggle("Show Labels", _showLabels);
            _showPlacedAssets = EditorGUILayout.Toggle("Show Assets", _showPlacedAssets);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear All", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear Grid",
                    "Are you sure you want to clear all tiles to Empty?",
                    "Clear", "Cancel"))
                {
                    Undo.RecordObject(_mapData, "Clear Grid");
                    _mapData.ClearAll();
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("Fill Selected", GUILayout.Height(25)))
            {
                Undo.RecordObject(_mapData, "Fill Grid");
                _mapData.FillRect(0, 0, _mapData.Width, _mapData.Height, _selectedTileType);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Save", GUILayout.Height(25)))
            {
                EditorUtility.SetDirty(_mapData);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.EndHorizontal();

            // Pattern generation
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Generators", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("City Grid Pattern", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Generate City Pattern",
                    "This will replace all tiles with a city grid pattern. Continue?",
                    "Generate", "Cancel"))
                {
                    GridPainter.GenerateBasicCityPattern(_mapData);
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            string status = "Ready";
            if (_hoveredTile.x >= 0 && _hoveredTile.y >= 0)
            {
                var type = _mapData.GetTileType(_hoveredTile);
                status = $"Hover: ({_hoveredTile.x}, {_hoveredTile.y}) | Type: {type}";
            }
            else if (_selectedTile.x >= 0 && _selectedTile.y >= 0)
            {
                var type = _mapData.GetTileType(_selectedTile);
                status = $"Selected: ({_selectedTile.x}, {_selectedTile.y}) | Type: {type}";
            }

            EditorGUILayout.LabelField(status);

            // Tile counts
            EditorGUILayout.LabelField(
                $"Lots: {_mapData.CountTilesOfType(TileType.Lot)} | " +
                $"Roads: {_mapData.CountTilesOfType(TileType.Road)} | " +
                $"Parks: {_mapData.CountTilesOfType(TileType.Park)}",
                EditorStyles.miniLabel,
                GUILayout.Width(200)
            );

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGridPreview()
        {
            EditorGUILayout.LabelField("Grid Preview (Use Scene View for full editing)", EditorStyles.boldLabel);

            // Simple minimap view
            float cellSize = 12f;
            float previewWidth = _mapData.Width * cellSize;
            float previewHeight = _mapData.Height * cellSize;

            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
            previewRect.width = Mathf.Min(previewRect.width, previewWidth);
            previewRect.height = previewHeight;

            _scrollPosition = GUI.BeginScrollView(
                new Rect(previewRect.x, previewRect.y, position.width - 20, 200),
                _scrollPosition,
                new Rect(0, 0, previewWidth, previewHeight)
            );

            for (int y = 0; y < _mapData.Height; y++)
            {
                for (int x = 0; x < _mapData.Width; x++)
                {
                    var type = _mapData.GetTileType(x, y);
                    Rect cellRect = new Rect(x * cellSize, y * cellSize, cellSize - 1, cellSize - 1);

                    EditorGUI.DrawRect(cellRect, _config.GetTileColor(type));

                    // Click handling
                    if (Event.current.type == EventType.MouseDown &&
                        cellRect.Contains(Event.current.mousePosition))
                    {
                        PaintTile(x, y);
                        Event.current.Use();
                    }
                }
            }

            GUI.EndScrollView();
        }

        // ═══════════════════════════════════════════════════════════════
        // SCENE VIEW DRAWING
        // ═══════════════════════════════════════════════════════════════

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_mapData == null || _config == null) return;

            // Draw the grid
            DrawGrid();

            // Draw placed assets
            if (_showPlacedAssets)
            {
                AssetPlacer.DrawPlacedAssets(_mapData, _config);
            }

            // Draw line/rectangle preview
            DrawToolPreview();

            // Handle asset drag and drop first
            if (AssetPlacer.HandleDragAndDrop(_mapData, _config))
            {
                sceneView.Repaint();
                return;
            }

            // Handle input
            HandleSceneInput();

            // Force repaint
            if (_isPainting || _isDrawingLine)
            {
                sceneView.Repaint();
            }
        }

        private void DrawToolPreview()
        {
            // Show preview for line/rectangle tools
            if (_isDrawingLine && _lineStart.x >= 0 && _hoveredTile.x >= 0)
            {
                Color previewColor = new Color(1f, 1f, 0f, 0.3f);

                if (_currentTool == EditTool.Line)
                {
                    // Draw line preview
                    DrawLinePreview(_lineStart, _hoveredTile, previewColor);
                }
                else if (_currentTool == EditTool.Rectangle)
                {
                    // Draw rectangle preview
                    DrawRectanglePreview(_lineStart, _hoveredTile, previewColor);
                }
            }
        }

        private void DrawLinePreview(Vector2Int start, Vector2Int end, Color color)
        {
            Vector3 startWorld = IsometricUtils.GridToWorld(start, _config);
            Vector3 endWorld = IsometricUtils.GridToWorld(end, _config);

            Handles.color = color;
            Handles.DrawLine(startWorld + Vector3.up * 0.1f, endWorld + Vector3.up * 0.1f, 3f);

            // Highlight endpoints
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(startWorld + Vector3.up * 0.1f, Vector3.up, 0.2f);
            Handles.DrawWireDisc(endWorld + Vector3.up * 0.1f, Vector3.up, 0.2f);
        }

        private void DrawRectanglePreview(Vector2Int start, Vector2Int end, Color color)
        {
            int minX = Mathf.Min(start.x, end.x);
            int maxX = Mathf.Max(start.x, end.x);
            int minY = Mathf.Min(start.y, end.y);
            int maxY = Mathf.Max(start.y, end.y);

            // Highlight all tiles in the rectangle
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector3[] corners = IsometricUtils.GetTileCorners(x, y, _config.TileWidth, _config.TileHeight);
                    Handles.color = color;
                    Handles.DrawAAConvexPolygon(corners);
                }
            }
        }

        private void DrawGrid()
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            for (int y = 0; y < _mapData.Height; y++)
            {
                for (int x = 0; x < _mapData.Width; x++)
                {
                    DrawTile(x, y);
                }
            }
        }

        private void DrawTile(int x, int y)
        {
            Vector3 center = IsometricUtils.GridToWorld(x, y, _config);
            Vector3[] corners = IsometricUtils.GetTileCorners(x, y, _config.TileWidth, _config.TileHeight);
            TileType type = _mapData.GetTileType(x, y);

            // Determine color
            Color tileColor = _config.GetTileColor(type);

            // Highlight hovered/selected tiles
            if (x == _hoveredTile.x && y == _hoveredTile.y)
            {
                tileColor = Color.Lerp(tileColor, Color.white, 0.5f);
            }
            else if (x == _selectedTile.x && y == _selectedTile.y)
            {
                tileColor = _config.SelectedTileColor;
            }

            // Draw filled tile
            Handles.color = tileColor;
            Handles.DrawAAConvexPolygon(corners);

            // Draw outline
            Handles.color = _config.GridLineColor;
            Handles.DrawAAPolyLine(2f, corners[0], corners[1], corners[2], corners[3], corners[0]);

            // Draw label
            if (_showLabels)
            {
                string label = _config.GetTileLabel(type);
                Handles.Label(center + Vector3.up * 0.1f, label, EditorStyles.whiteMiniLabel);
            }
        }

        private void HandleSceneInput()
        {
            Event e = Event.current;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            // Raycast to XZ plane (y = 0)
            float t = -ray.origin.y / ray.direction.y;
            if (t > 0)
            {
                Vector3 hitPoint = ray.origin + ray.direction * t;
                Vector2Int gridPos = IsometricUtils.WorldToGrid(hitPoint, _config);

                // Update hover state
                if (IsometricUtils.IsInBounds(gridPos, _config))
                {
                    if (_hoveredTile != gridPos)
                    {
                        _hoveredTile = gridPos;
                        Repaint();
                    }
                }
                else
                {
                    _hoveredTile = new Vector2Int(-1, -1);
                }

                // Handle input based on current tool
                switch (_currentTool)
                {
                    case EditTool.Paint:
                        HandlePaintInput(e, gridPos);
                        break;
                    case EditTool.Fill:
                        HandleFillInput(e, gridPos);
                        break;
                    case EditTool.Line:
                    case EditTool.Rectangle:
                        HandleLineRectInput(e, gridPos);
                        break;
                }
            }

            // Cancel line/rect drawing with escape
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && _isDrawingLine)
            {
                _isDrawingLine = false;
                _lineStart = new Vector2Int(-1, -1);
                e.Use();
            }

            // Prevent default behavior when over grid
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
        }

        private void HandlePaintInput(Event e, Vector2Int gridPos)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (IsometricUtils.IsInBounds(gridPos, _config))
                {
                    _isPainting = true;
                    PaintAtPosition(gridPos.x, gridPos.y);
                    _selectedTile = gridPos;
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && _isPainting)
            {
                if (IsometricUtils.IsInBounds(gridPos, _config))
                {
                    PaintAtPosition(gridPos.x, gridPos.y);
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                _isPainting = false;
            }
        }

        private void HandleFillInput(Event e, Vector2Int gridPos)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (IsometricUtils.IsInBounds(gridPos, _config))
                {
                    GridPainter.FloodFill(_mapData, gridPos.x, gridPos.y, _selectedTileType);
                    _selectedTile = gridPos;
                    SceneView.RepaintAll();
                    Repaint();
                    e.Use();
                }
            }
        }

        private void HandleLineRectInput(Event e, Vector2Int gridPos)
        {
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (!IsometricUtils.IsInBounds(gridPos, _config))
                {
                    return;
                }

                if (!_isDrawingLine)
                {
                    // Start the line/rect
                    _lineStart = gridPos;
                    _isDrawingLine = true;
                    e.Use();
                }
                else
                {
                    // Finish the line/rect
                    if (_currentTool == EditTool.Line)
                    {
                        GridPainter.DrawLine(_mapData, _lineStart.x, _lineStart.y,
                            gridPos.x, gridPos.y, _selectedTileType, _brushSize);
                    }
                    else
                    {
                        GridPainter.FillRectangle(_mapData, _lineStart.x, _lineStart.y,
                            gridPos.x, gridPos.y, _selectedTileType);
                    }

                    _isDrawingLine = false;
                    _lineStart = new Vector2Int(-1, -1);
                    _selectedTile = gridPos;
                    SceneView.RepaintAll();
                    Repaint();
                    e.Use();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PAINTING
        // ═══════════════════════════════════════════════════════════════

        private void PaintAtPosition(int centerX, int centerY)
        {
            int halfBrush = _brushSize / 2;

            Undo.RecordObject(_mapData, "Paint Tile");

            for (int dy = -halfBrush; dy <= halfBrush; dy++)
            {
                for (int dx = -halfBrush; dx <= halfBrush; dx++)
                {
                    PaintTile(centerX + dx, centerY + dy);
                }
            }

            SceneView.RepaintAll();
            Repaint();
        }

        private void PaintTile(int x, int y)
        {
            if (_mapData.IsValidPosition(x, y))
            {
                _mapData.SetTileType(x, y, _selectedTileType);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ASSET CREATION
        // ═══════════════════════════════════════════════════════════════

        private void CreateNewMapData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Grid Map Data",
                "CityMap",
                "asset",
                "Choose where to save the city map data"
            );

            if (!string.IsNullOrEmpty(path))
            {
                var mapData = CreateInstance<GridMapData>();

                if (_config != null)
                {
                    mapData.Initialize(_config);
                }

                AssetDatabase.CreateAsset(mapData, path);
                AssetDatabase.SaveAssets();

                _mapData = mapData;
                Selection.activeObject = mapData;
            }
        }

        private void CreateNewConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Grid Config",
                "IsometricGridConfig",
                "asset",
                "Choose where to save the grid configuration"
            );

            if (!string.IsNullOrEmpty(path))
            {
                var config = CreateInstance<IsometricGridConfig>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();

                _config = config;
                Selection.activeObject = config;

                // Initialize map data with new config
                if (_mapData != null)
                {
                    _mapData.Initialize(config);
                }
            }
        }
    }
}
