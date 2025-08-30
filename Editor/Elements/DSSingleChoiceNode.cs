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
    public class DSSingleChoiceNode : DSNode
    {
        public override void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 position)
        {
            base.Initialize(nodeName, dsGraphView, position);

            DialogueType = DSDialogueType.SingleChoice;

            DSChoiceSaveData choiceData = new()
            {
                Text = "Next Dialogue"
            };

            Choices.Add(choiceData);
        }

        public override void Draw()
        {
            base.Draw();

            // OUTPUT CONTAINER
            var choice = Choices[0];
            Port choicePort = this.CreatePort(choice.Text);

            choicePort.userData = choice;
            choicePort.portName = "";
            
            VisualElement choiceContainer = new VisualElement();
            choiceContainer.style.flexDirection = FlexDirection.Row;
            choiceContainer.style.alignItems = Align.Center;
            choiceContainer.style.flexWrap = Wrap.Wrap;

            TextField choiceTextField = DSElementUtility.CreateTextField(choice.Text, null,
                callback => { choice.Text = callback.newValue; });
            choiceTextField.AddClasses(
                "ds-node_textfield",
                "ds-node_choice-textfield",
                "ds-node_textfield_hidden");

            // Icon-based Action Button with dynamic icon based on action type
            Button actionButton = new Button();
            actionButton.text = GetActionIcon(choice.ActionType);
            actionButton.tooltip = $"Action: {choice.ActionType}\nParam: {choice.ActionParameter ?? "None"}";
            actionButton.style.minWidth = 24;
            actionButton.style.maxWidth = 24;
            actionButton.style.minHeight = 20;
            actionButton.style.maxHeight = 20;
            actionButton.AddToClassList("ds-node_button");
            
            // Set the click event after the button is fully created
            actionButton.clicked += () => OpenActionPopup(choice, actionButton);

            choiceContainer.Add(choiceTextField);
            choiceContainer.Add(actionButton);

            choicePort.Add(choiceContainer);
            outputContainer.Add(choicePort);
            RefreshExpandedState();
        }

        #region Action Popup

        private void OpenActionPopup(DSChoiceSaveData choiceData, Button actionButton)
        {
            var popup = new ActionPopupWindow(choiceData, () => RefreshChoicePorts(), actionButton);
            var mousePos = UnityEditor.EditorGUIUtility.GUIToScreenRect(new Rect(Event.current.mousePosition, Vector2.zero));
            UnityEditor.PopupWindow.Show(mousePos, popup);
        }

        private void RefreshChoicePorts()
        {
            // Force a redraw of the node to update icons and tooltips
            RefreshExpandedState();
            
            // Also update the tooltip for the choice button
            if (Choices.Count > 0)
            {
                UpdateChoiceButtonTooltip(Choices[0]);
            }
        }

        private void UpdateChoiceButtonTooltip(DSChoiceSaveData choiceData)
        {
            // Find the button for this choice and update its tooltip
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

        private class ActionPopupWindow : UnityEditor.PopupWindowContent
        {
            private readonly DSChoiceSaveData _choiceData;
            private readonly System.Action _onApplyCallback;
            private DSActionType _actionType;
            private string _actionParameter;
            private Button _actionButton;

            public ActionPopupWindow(DSChoiceSaveData choiceData, System.Action onApplyCallback, Button actionButton)
            {
                _choiceData = choiceData;
                _onApplyCallback = onApplyCallback;
                _actionType = choiceData.ActionType;
                _actionParameter = choiceData.ActionParameter ?? "";
                _actionButton = actionButton;
            }

            public override void OnGUI(Rect rect)
            {
                GUILayout.BeginArea(rect);
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
                    editorWindow.Close();
                }
                
                GUILayout.EndArea();
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(250, 150);
            }
        }

        #endregion

        #region Action Icons

        private string GetActionIcon(DSActionType actionType)
        {
            // Generic gear icon for all actions - popup will show the specific details
            return "⚙";
        }

        #endregion
    }
}