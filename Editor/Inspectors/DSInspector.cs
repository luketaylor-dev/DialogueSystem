using System;
using System.Collections.Generic;
using System.Linq;
using DialogueSystem.ScriptableObjects;
using DialogueSystem.Utilities;
using UnityEditor;
using UnityEditor.VersionControl;

namespace DialogueSystem.Inspectors
{
    [CustomEditor(typeof(DSDialogue))]
    public class DSInspector : Editor
    {
        //Dialogue Scriptable Objects
        private SerializedProperty _dialogueContainerProperty;
        private SerializedProperty _dialogueGroupProperty;
        private SerializedProperty _dialogueProperty;

        //Filters
        private SerializedProperty _groupedDialoguesProperty;
        private SerializedProperty _startingDialoguesOnlyProperty;

        // Indexes
        private SerializedProperty _selectedDialogueGroupIndexProperty;
        private SerializedProperty _selectedDialogueIndexProperty;


        private void OnEnable()
        {
            _dialogueContainerProperty = serializedObject.FindProperty("dialogueContainer");
            _dialogueGroupProperty = serializedObject.FindProperty("dialogueGroup");
            _dialogueProperty = serializedObject.FindProperty("dialogue");

            _groupedDialoguesProperty = serializedObject.FindProperty("groupedDialogues");
            _startingDialoguesOnlyProperty = serializedObject.FindProperty("startingDialoguesOnly");

            _selectedDialogueGroupIndexProperty = serializedObject.FindProperty("selectedDialogueGroupIndex");
            _selectedDialogueIndexProperty = serializedObject.FindProperty("selectedDialogueIndex");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDialogueContainerArea();

            DSDialogueContainerSO dialogueContainer =
                (DSDialogueContainerSO)_dialogueContainerProperty.objectReferenceValue;

            if (dialogueContainer == null)
            {
                StopDrawing("Select a Dialogue Container to see the rest of the Inspector");
                return;
            }

            DrawFiltersArea();

            bool currentStartingDialoguesOnlyFilter = _startingDialoguesOnlyProperty.boolValue;

            List<string> dialogueNames;
            string dialogueFolderPath = $"Assets/DialogueSystem/Dialogues/{dialogueContainer.FileName}";
            string dialogueInfoMessage;


            if (_groupedDialoguesProperty.boolValue)
            {
                List<string> dialogueGroupNames = dialogueContainer.DialogueGroupNames;

                if (dialogueGroupNames.Count == 0)
                {
                    StopDrawing("There are no Dialogue Groups in this Dialogue Container");
                    return;
                }

                DrawDialogGroupArea(dialogueContainer, dialogueGroupNames);

                DSDialogueGroupSO dialogueGroup = (DSDialogueGroupSO)_dialogueGroupProperty.objectReferenceValue;
                dialogueNames = dialogueContainer.GetGroupedDialogueNames(dialogueGroup, currentStartingDialoguesOnlyFilter);

                dialogueFolderPath += $"/Groups/{dialogueGroup.GroupName}/Dialogues";

                dialogueInfoMessage = "There are no" + (currentStartingDialoguesOnlyFilter ? " Starting" : string.Empty) + " Dialogues in this Dialogue Group";
            }
            else
            {
                dialogueNames = dialogueContainer.GetUngroupedDialogueNames(currentStartingDialoguesOnlyFilter);

                dialogueFolderPath += $"/Global/Dialogues";

                dialogueInfoMessage = "There are no" + (currentStartingDialoguesOnlyFilter ? " Starting" : string.Empty) + " Ungrouped Dialogues in this Dialogue Container";
            }

            if (dialogueNames.Count == 0)
            {
                StopDrawing(dialogueInfoMessage);
                return;
            }

            DrawDialogueArea(dialogueNames, dialogueFolderPath);
            serializedObject.ApplyModifiedProperties();
        }

        private void StopDrawing(string reason, MessageType messageType = MessageType.Info)
        {
            DSInspectorUtility.DrawHelpBox(reason, messageType);
            DSInspectorUtility.DrawSpace();
            DSInspectorUtility.DrawHelpBox("You need to select a Dialogue for this component to work properly at Runtime!", MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }


        #region Draw Methods

        private void DrawDialogueContainerArea()
        {
            DSInspectorUtility.DrawHeader("Dialogue Container");
            _dialogueContainerProperty.DrawPropertyField();
            DSInspectorUtility.DrawSpace();
        }

        private void DrawFiltersArea()
        {
            DSInspectorUtility.DrawHeader("Filters");
            _groupedDialoguesProperty.DrawPropertyField();
            _startingDialoguesOnlyProperty.DrawPropertyField();
            DSInspectorUtility.DrawSpace();
        }

        private void DrawDialogGroupArea(DSDialogueContainerSO dialogueContainer, List<string> dialogueGroupNames)
        {
            DSInspectorUtility.DrawHeader("Dialogue Group");

            int oldSelectedDialogueGroupIndex = _selectedDialogueGroupIndexProperty.intValue;

            DSDialogueGroupSO oldDialogueGroup = (DSDialogueGroupSO)_dialogueGroupProperty.objectReferenceValue;

            bool isOldDialogueGroupNull = oldDialogueGroup == null;
            string oldDialogueGroupName = isOldDialogueGroupNull ? string.Empty : oldDialogueGroup.GroupName;

            UpdateIndexNamesListUpdate(dialogueGroupNames, _selectedDialogueGroupIndexProperty, oldDialogueGroupName,
                isOldDialogueGroupNull, oldSelectedDialogueGroupIndex);

            _selectedDialogueGroupIndexProperty.intValue = DSInspectorUtility.DrawPopup("Dialog Group",
                _selectedDialogueGroupIndexProperty, dialogueGroupNames.ToArray());

            string selectedDialogueGroupName = dialogueGroupNames[_selectedDialogueGroupIndexProperty.intValue];

            DSDialogueGroupSO selectedDialogueGroup = DSIOUtility.LoadAsset<DSDialogueGroupSO>(
                $"Assets/DialogueSystem/Dialogues/{dialogueContainer.FileName}/Groups/{selectedDialogueGroupName}",
                selectedDialogueGroupName);

            _dialogueGroupProperty.objectReferenceValue = selectedDialogueGroup;

            DSInspectorUtility.DrawDisabledFields(() => _dialogueGroupProperty.DrawPropertyField());
            DSInspectorUtility.DrawSpace();
        }


        private void DrawDialogueArea(List<string> dialogueNames, string dialogueFolderPath)
        {
            DSInspectorUtility.DrawHeader("Dialogue");

            int oldSelectedDialogueIndex = _selectedDialogueIndexProperty.intValue;
            DSDialogueSO oldDialogue = (DSDialogueSO)_dialogueProperty.objectReferenceValue;

            bool isOldDialogueNull = oldDialogue == null;
            string oldDialogueName = isOldDialogueNull ? string.Empty : oldDialogue.DialogueName;

            UpdateIndexNamesListUpdate(dialogueNames, _selectedDialogueIndexProperty, oldDialogueName,
                isOldDialogueNull, oldSelectedDialogueIndex);

            _selectedDialogueIndexProperty.intValue = DSInspectorUtility.DrawPopup("Dialog",
                _selectedDialogueIndexProperty, dialogueNames.ToArray());

            string selectedDialogueName = dialogueNames[_selectedDialogueIndexProperty.intValue];

            DSDialogueSO selectedDialogue =
                DSIOUtility.LoadAsset<DSDialogueSO>(dialogueFolderPath, selectedDialogueName);

            _dialogueProperty.objectReferenceValue = selectedDialogue;

            DSInspectorUtility.DrawDisabledFields(() => _dialogueProperty.DrawPropertyField());
        }

        #endregion

        #region Index Methods

        private void UpdateIndexNamesListUpdate(List<string> optionNames, SerializedProperty indexProperty,
            string oldPropertyName, bool isOldPropertyNull,
            int oldSelectedPropertyIndex)
        {
            if (isOldPropertyNull)
            {
                indexProperty.intValue = 0;

                return;
            }

            bool oldIndexIsOutOfBoundsOfNamesListCount = oldSelectedPropertyIndex > optionNames.Count - 1;
            bool oldNameIsDifferentThanSelectedName = oldIndexIsOutOfBoundsOfNamesListCount ||
                                                      oldPropertyName != optionNames[oldSelectedPropertyIndex];

            if (!oldNameIsDifferentThanSelectedName) return;

            indexProperty.intValue = optionNames.Contains(oldPropertyName)
                ? optionNames.IndexOf(oldPropertyName)
                : 0;
        }

        #endregion
    }
}