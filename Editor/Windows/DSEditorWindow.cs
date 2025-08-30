using System;
using System.IO;
using DialogueSystem.Utilities;
using UnityEditor;
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

        [MenuItem("Window/DS/Dialogue System")]
        public static void ShowExample()
        {
            DSEditorWindow wnd = GetWindow<DSEditorWindow>("Dialogue System");
        }

        private void CreateGUI()
        {
            AddGraphView();
            AddToolbar();

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

            toolbar.Add(_fileNameTextField);
            toolbar.Add(_saveButton);
            toolbar.Add(loadButton);
            toolbar.Add(clearButton);
            toolbar.Add(resetButton);
            toolbar.Add(_miniMapButton);

            // Load toolbar stylesheet from the module's Resources folder
            var toolbarStyles = Resources.Load<StyleSheet>("DSToolbarStyles");
            if (toolbarStyles != null)
                toolbar.styleSheets.Add(toolbarStyles);

            rootVisualElement.Add(toolbar);
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