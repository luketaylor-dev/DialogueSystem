using System;
using System.IO;
using System.Linq;
using DialogueSystem.Utilities;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem.Windows
{
    public class DSEditorWindow : EditorWindow
    {
        private DSGraphView _graphView;
        private const string DEFAULT_FILE_NAME = "DialogueFileName";
        private static TextField _fileNameTextField;
        private Button _saveButton;
        private Button _miniMapButton;
        private Button _debugButton;
        private VisualElement _debugPanel;
        private bool _debugMode = false;

        [MenuItem("Window/DS/Dialogue System")]
        public static void ShowExample()
        {
            DSEditorWindow wnd = GetWindow<DSEditorWindow>("Dialogue System");
        }

        private void CreateGUI()
        {
            AddGraphView();
            AddToolbar();
            AddDebugPanel();

            AddStyles();
        }

        #region Elements Addition

        private void AddStyles()
        {
            // Load stylesheet from the module's Resources folder
            var variablesStyles = Resources.Load<StyleSheet>("DSVariables");
            if (variablesStyles != null)
                rootVisualElement.styleSheets.Add(variablesStyles);
        }

        private void AddGraphView()
        {
            _graphView = new(this);

            _graphView.StretchToParentSize();

            rootVisualElement.Add(_graphView);
        }

        private void AddToolbar()
        {
            Toolbar toolbar = new Toolbar();

            _fileNameTextField = DSElementUtility.CreateTextField(DEFAULT_FILE_NAME, "File Name:",
                callback => { _fileNameTextField.value = callback.newValue.RemoveWhitespaceAndSpecialCharacters(); });

            _saveButton = DSElementUtility.CreateButton("Save", () => Save());

            Button loadButton = DSElementUtility.CreateButton("Load", Load);
            Button clearButton = DSElementUtility.CreateButton("Clear", Clear);
            Button resetButton = DSElementUtility.CreateButton("Reset", ResetGraph);
            _miniMapButton = DSElementUtility.CreateButton("Minimap", ToggleMiniMap);
            _debugButton = DSElementUtility.CreateButton("Debug", ToggleDebug);

            toolbar.Add(_fileNameTextField);
            toolbar.Add(_saveButton);
            toolbar.Add(loadButton);
            toolbar.Add(clearButton);
            toolbar.Add(resetButton);
            toolbar.Add(_miniMapButton);
            toolbar.Add(_debugButton);

            // Load toolbar stylesheet from the module's Resources folder
            var toolbarStyles = Resources.Load<StyleSheet>("DSToolbarStyles");
            if (toolbarStyles != null)
                toolbar.styleSheets.Add(toolbarStyles);

            rootVisualElement.Add(toolbar);
        }

        private void AddDebugPanel()
        {
            _debugPanel = new VisualElement();
            _debugPanel.style.display = DisplayStyle.None;
            _debugPanel.style.position = Position.Absolute;
            _debugPanel.style.top = 40;
            _debugPanel.style.right = 10;
            _debugPanel.style.width = 250;
            _debugPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            _debugPanel.style.paddingLeft = 10;
            _debugPanel.style.paddingRight = 10;
            _debugPanel.style.paddingTop = 10;
            _debugPanel.style.paddingBottom = 10;
            _debugPanel.style.borderTopLeftRadius = 5;
            _debugPanel.style.borderTopRightRadius = 5;
            _debugPanel.style.borderBottomLeftRadius = 5;
            _debugPanel.style.borderBottomRightRadius = 5;
            _debugPanel.style.borderLeftWidth = 1;
            _debugPanel.style.borderRightWidth = 1;
            _debugPanel.style.borderTopWidth = 1;
            _debugPanel.style.borderBottomWidth = 1;
            _debugPanel.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
            _debugPanel.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            _debugPanel.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
            _debugPanel.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            rootVisualElement.Add(_debugPanel);
        }


        #endregion

        #region Toolbar Actions

        private void Save()
        {
            if (string.IsNullOrEmpty(_fileNameTextField.value))
            {
                EditorUtility.DisplayDialog(
                    "Inalid File Name",
                    "Please ensure the file name you've typed in is valid",
                    "OK");
                return;
            }

            DSIOUtility.Initialize(_graphView, _fileNameTextField.value);

            DSIOUtility.Save();
        }

        private void Load()
        {
            string filePath = EditorUtility.OpenFilePanel("Dialogue Graphs", "Assets/Editor/DialogueSystem/Graphs", "asset");
            if (string.IsNullOrEmpty(filePath)) return;

            Clear();

            DSIOUtility.Initialize(_graphView, Path.GetFileNameWithoutExtension(filePath));
            DSIOUtility.Load();
        }

        private void Clear()
        {
            _graphView.ClearGraph();
        }
        private void ResetGraph()
        {
            Clear();

            UpdateFileName(DEFAULT_FILE_NAME);
        }

        private void ToggleMiniMap()
        {
            _graphView.ToggleMiniMap();

            _miniMapButton.ToggleInClassList("ds-toolbar_button_selected");
        }

        private void ToggleDebug()
        {
            _debugMode = !_debugMode;
            
            if (_debugMode)
            {
                _debugPanel.style.display = DisplayStyle.Flex;
                _debugButton.ToggleInClassList("ds-toolbar_button_selected");
                UpdateDebugInfo();
                EditorApplication.update += OnEditorUpdate;
            }
            else
            {
                _debugPanel.style.display = DisplayStyle.None;
                _debugButton.RemoveFromClassList("ds-toolbar_button_selected");
                EditorApplication.update -= OnEditorUpdate;
            }
        }

        private float _lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 0.5f; // Update every 0.5 seconds

        private void OnEditorUpdate()
        {
            if (_debugMode && _debugPanel != null)
            {
                // Use EditorApplication.timeSinceStartup for reliable timing in editor
                float currentTime = (float)EditorApplication.timeSinceStartup;
                if (currentTime - _lastUpdateTime >= UPDATE_INTERVAL)
                {
                    UpdateDebugInfo();
                    _lastUpdateTime = currentTime;
                }
            }
        }

        private void UpdateDebugInfo()
        {
            if (!_debugMode || _debugPanel == null) return;

            _debugPanel.Clear();

            // Title
            var title = new Label("Debug Info");
            title.style.color = Color.white;
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 10;
            _debugPanel.Add(title);

            // Node statistics
            var nodeCount = _graphView.nodes.ToList().Count;
            var groupCount = _graphView.graphElements.ToList().OfType<Group>().Count();
            var edgeCount = _graphView.edges.ToList().Count;
            var selectedCount = _graphView.selection.Count;

            AddDebugLabel($"Nodes: {nodeCount}");
            AddDebugLabel($"Groups: {groupCount}");
            AddDebugLabel($"Connections: {edgeCount}");
            AddDebugLabel($"Selected: {selectedCount}");

            // Performance metrics
            var totalElements = nodeCount + groupCount + edgeCount;
            AddDebugLabel($"Total Elements: {totalElements}");

            // Memory usage estimation (more accurate calculation)
            var nodeMemoryKB = nodeCount * 1.5f; // UI elements, text, ports
            var groupMemoryKB = groupCount * 0.8f; // Container elements
            var edgeMemoryKB = edgeCount * 0.3f; // Line renderers
            var totalMemoryKB = nodeMemoryKB + groupMemoryKB + edgeMemoryKB;
            
            AddDebugLabel($"Memory - Nodes: {nodeMemoryKB:F1}KB");
            AddDebugLabel($"Memory - Groups: {groupMemoryKB:F1}KB");
            AddDebugLabel($"Memory - Edges: {edgeMemoryKB:F1}KB");
            AddDebugLabel($"Total Memory: ~{totalMemoryKB:F1}KB");

            // Graph complexity
            var complexity = nodeCount > 0 ? (float)edgeCount / nodeCount : 0;
            AddDebugLabel($"Complexity: {complexity:F2}");

            // Performance warnings and recommendations
            if (nodeCount > 100)
            {
                AddDebugLabel("⚠️ High node count - consider grouping");
                AddDebugLabel($"   → Grouping could reduce visible elements by ~{nodeCount - groupCount * 5} nodes");
            }
            if (edgeCount > 200)
            {
                AddDebugLabel("⚠️ Many connections - check for cycles");
                AddDebugLabel($"   → Each connection adds ~0.3KB memory");
            }
            if (complexity > 3)
            {
                AddDebugLabel("⚠️ High complexity - review structure");
                AddDebugLabel($"   → Average {complexity:F1} connections per node");
            }
            
            // Performance tips
            if (nodeCount > 50 && groupCount < nodeCount / 10)
            {
                AddDebugLabel("💡 Tip: Consider grouping related nodes");
            }
            if (totalMemoryKB > 100)
            {
                AddDebugLabel("💡 Tip: Large graphs may benefit from LOD (Level of Detail)");
            }

            // Current time for refresh tracking
            AddDebugLabel($"Last Update: {DateTime.Now:HH:mm:ss}");
            AddDebugLabel($"Auto-refresh: Every {UPDATE_INTERVAL}s");

            // Update button
            var updateButton = new Button(() => UpdateDebugInfo()) { text = "Refresh Now" };
            updateButton.style.marginTop = 10;
            updateButton.style.width = 100;
            _debugPanel.Add(updateButton);
        }

        private void AddDebugLabel(string text)
        {
            var label = new Label(text);
            label.style.color = Color.white;
            label.style.fontSize = 12;
            label.style.marginBottom = 2;
            _debugPanel.Add(label);
        }

        private void OnDestroy()
        {
            // Clean up event subscription to prevent memory leaks
            EditorApplication.update -= OnEditorUpdate;
        }
        #endregion

        #region Utility

        public static void UpdateFileName(string newFileName)
        {
            _fileNameTextField.value = newFileName;
        }
        public void EnableSaving()
        {
            _saveButton.SetEnabled(true);
        }

        public void DisableSaving()
        {
            _saveButton.SetEnabled(false);
        }

        #endregion
    }
}