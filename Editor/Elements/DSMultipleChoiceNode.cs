using DialogueSystem.Data.Save;
using DialogueSystem.Enums;
using DialogueSystem.Utilities;
using DialogueSystem.Windows;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace DialogueSystem.Elements
{
    public class DSMultipleChoiceNode : DSNode
    {
        public override void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 position)
        {
            base.Initialize(nodeName, dsGraphView, position);
            DialogueType = DSDialogueType.MultipleChoice;

            DSChoiceSaveData choiceData = new()
            {
                Text = "New Choice"
            };

            Choices.Add(choiceData);
        }

        public override void Draw()
        {
            base.Draw();

            //MAIN CONTAINER

            Button addChoiceButton = DSElementUtility.CreateButton("Add Choice", () =>
            {
                DSChoiceSaveData choiceData = new()
                {
                    Text = "New Choice"
                };
                Choices.Add(choiceData);
                
                var choicePort = CreateChoicePort(choiceData);
                outputContainer.Add(choicePort);
                
                RefreshExpandedState();
            });
            addChoiceButton.AddToClassList("ds-node_button");
            addChoiceButton.AddToClassList("ds-node_button-add-choice");
            addChoiceButton.tooltip = "Add Choice";

            mainContainer.Insert(1, addChoiceButton);

            //OUTPUT CONTAINER

            foreach (DSChoiceSaveData choice in Choices)
            {
                var choicePort = CreateChoicePort(choice);

                outputContainer.Add(choicePort);
            }

            RefreshExpandedState();
        }

        #region Element Creation

        private Port CreateChoicePort(object userData)
        {
            Port choicePort = this.CreatePort();

            choicePort.userData = userData;

            DSChoiceSaveData choiceData = (DSChoiceSaveData)userData;

            choicePort.portName = "";

            Button deleteChoiceButton = DSElementUtility.CreateButton("X", () =>
            {
                if (Choices.Count == 1)
                {
                    return;
                }

                if (choicePort.connected)
                {
                    GraphView.DeleteElements(choicePort.connections);
                }

                Choices.Remove(choiceData);

                GraphView.RemoveElement(choicePort);
                
                RefreshExpandedState();
            });
            deleteChoiceButton.AddToClassList("ds-node_button");
            deleteChoiceButton.AddToClassList("ds-node_button-delete");
            deleteChoiceButton.tooltip = "Delete Choice";

            VisualElement choiceContainer = new VisualElement();
            choiceContainer.style.flexDirection = FlexDirection.Row;
            choiceContainer.style.alignItems = Align.Center;
            choiceContainer.style.flexWrap = Wrap.Wrap;

            TextField choiceTextField = DSElementUtility.CreateTextField(choiceData.Text, null,
                callback => { choiceData.Text = callback.newValue; });

            choiceTextField.AddClasses(
                "ds-node_textfield",
                "ds-node_choice-textfield",
                "ds-node_textfield_hidden");

            // Icon-based Action Button with dynamic icon based on action type
            Button actionButton = new Button();
            actionButton.text = GetActionIcon(choiceData.ActionType);
            actionButton.tooltip = $"Action: {choiceData.ActionType}\nParam: {choiceData.ActionParameter ?? "None"}";
            actionButton.tooltip = "Action Settings";
            actionButton.style.minWidth = 24;
            actionButton.style.maxWidth = 24;
            actionButton.style.minHeight = 20;
            actionButton.style.maxHeight = 20;
            actionButton.AddToClassList("ds-node_button");
            actionButton.AddToClassList("ds-node_button-action");
            
            // Set the click event after the button is fully created
            actionButton.clicked += () => OpenActionPopup(choiceData, actionButton);

            choiceContainer.Add(choiceTextField);
            choiceContainer.Add(actionButton);

            choicePort.Add(choiceContainer);
            choicePort.Add(deleteChoiceButton);
            return choicePort;
        }

        #endregion

        #region Action Icons

        private string GetActionIcon(DSActionType actionType)
        {
            // Generic gear icon for all actions - popup will show the specific details
            return "⚙";
        }

        #endregion

        #region Action Popup

        private void OpenActionPopup(DSChoiceSaveData choiceData, Button actionButton)
        {
            var mousePos = UnityEditor.EditorGUIUtility.GUIToScreenRect(new Rect(Event.current.mousePosition, Vector2.zero));
            
            // Create a new window instance to prevent auto-closing
            var window = UnityEditor.EditorWindow.CreateInstance<ActionPopupWindow>();
            window.Initialize(choiceData, () => RefreshChoicePorts(), actionButton);
            window.position = new Rect(mousePos.x, mousePos.y, 300, 200);
            window.titleContent = new GUIContent("Action Settings");
            window.Show();
        }

        private void RefreshChoicePorts()
        {
            // Force a redraw of the node to update icons and tooltips
            RefreshExpandedState();
            
            // Also update the tooltips for all choice buttons
            foreach (var choice in Choices)
            {
                UpdateChoiceButtonTooltip(choice);
            }
        }

        private void UpdateChoiceButtonTooltip(DSChoiceSaveData choiceData)
        {
            // Find the button for this choice and update its tooltip
            // This will be called after the popup applies changes
            var choicePort = outputContainer.Children().OfType<Port>()
                .FirstOrDefault(port => port.userData == choiceData);
            
            if (choicePort != null)
            {
                var actionButton = choicePort.Children().OfType<Button>()
                    .FirstOrDefault(btn => btn.text == "⚙");
                
                if (actionButton != null)
                {
                    actionButton.tooltip = $"Action: {choiceData.ActionType}\nParam: {choiceData.ActionParameter ?? "None"}";
                }
            }
        }

        private class ActionPopupWindow : UnityEditor.EditorWindow
        {
            private DSChoiceSaveData _choiceData;
            private System.Action _onApplyCallback;
            private DSActionType _actionType;
            private string _actionParameter;
            private Button _actionButton;

            public void Initialize(DSChoiceSaveData choiceData, System.Action onApplyCallback, Button actionButton)
            {
                _choiceData = choiceData;
                _onApplyCallback = onApplyCallback;
                _actionType = choiceData.ActionType;
                _actionParameter = choiceData.ActionParameter ?? "";
                _actionButton = actionButton;
                
                // Set window properties to prevent auto-closing
                wantsMouseMove = true;
                autoRepaintOnSceneChange = false;
                
                // Force the window size
                minSize = new Vector2(300, 200);
                maxSize = new Vector2(300, 200);
            }

            private void OnGUI()
            {
                if (_choiceData == null) return;
                
                // Force the window size
                if (position.width != 300 || position.height != 200)
                {
                    position = new Rect(position.x, position.y, 300, 200);
                }
                
                GUILayout.Label("Action Settings", UnityEditor.EditorStyles.boldLabel);
                
                GUILayout.Space(5);
                
                // Action Type Dropdown
                GUILayout.Label("Action Type:");
                _actionType = (DSActionType)UnityEditor.EditorGUILayout.EnumPopup(_actionType);
                
                GUILayout.Space(5);
                
                // Action Parameter Field
                GUILayout.Label("Parameter:");
                _actionParameter = GUILayout.TextField(_actionParameter);
                
                GUILayout.Space(10);
                
                // Apply Button
                if (GUILayout.Button("Apply"))
                {
                    _choiceData.ActionType = _actionType;
                    _choiceData.ActionParameter = _actionParameter;
                    
                    // Update the button tooltip immediately
                    if (_actionButton != null)
                    {
                        _actionButton.tooltip = $"Action: {_actionType}\nParam: {_actionParameter ?? "None"}";
                    }
                    
                    _onApplyCallback?.Invoke();
                    Close();
                }
                
                // Cancel Button
                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }
            }
        }

        #endregion
    }
}