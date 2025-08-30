using System;
using System.Collections.Generic;
using System.Linq;
using DialogueSystem.Data.Save;
using DialogueSystem.Enums;
using DialogueSystem.Utilities;
using DialogueSystem.Windows;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem.Elements
{
    public class DSNode : Node
    {
        public string ID { get; set; }
        public string DialogueName { get; set; }
        public List<DSChoiceSaveData> Choices { get; set; }
        public string DialogueSpeaker { get; set; }
        public string Text { get; set; }
        public DSDialogueType DialogueType { get; set; }
        public DSGroup Group { get; set; }

        protected DSGraphView GraphView;

        private Color _defaultBackgroundColor;

        public virtual void Initialize(string nodeName, DSGraphView dsGraphView, Vector2 position)
        {
            ID = Guid.NewGuid().ToString();
            DialogueName = nodeName;
            Choices = new List<DSChoiceSaveData>();
            Text = "";
            DialogueSpeaker = "";

            GraphView = dsGraphView;
            _defaultBackgroundColor = new Color(29f / 255f, 29f / 255f, 30f / 255f);

            SetPosition(new Rect(position, Vector2.zero));

            mainContainer.AddToClassList("ds-node_main-container");
            extensionContainer.AddToClassList("ds-node_extension-container");
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Disconnect Input Ports", _ => DisconnectInputPorts());
            evt.menu.AppendAction("Disconnect Output Ports", _ => DisconnectOutputPorts());
            base.BuildContextualMenu(evt);
        }

        public virtual void Draw()
        {
            TextField dialogueNameTextField = DSElementUtility.CreateTextField(DialogueName, null, callback =>
            {
                TextField target = (TextField)callback.target;

                target.value = callback.newValue.RemoveWhitespaceAndSpecialCharacters();

                if (string.IsNullOrEmpty(target.value))
                {
                    if (!string.IsNullOrEmpty(DialogueName))
                    {
                        GraphView.NameErrorAmount++;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(DialogueName))
                    {
                        GraphView.NameErrorAmount--;
                    }
                }

                if (Group == null)
                {
                    GraphView.RemoveUngroupedNode(this);

                    DialogueName = target.value;

                    GraphView.AddUngroupedNode(this);
                    return;
                }

                DSGroup currentGroup = Group;

                GraphView.RemoveGroupedNode(this, Group);

                DialogueName = callback.newValue;

                GraphView.AddGroupedNode(this, currentGroup);
            });
            dialogueNameTextField.AddClasses(
                "ds-node_textfield",
                "ds-node_filename-textfield",
                "ds-node_textfield_hidden");

            titleContainer.Insert(0, dialogueNameTextField);

            // INPUT CONTAINER

            Port inputPort = this.CreatePort("Dialogue Connection", Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);

            inputPort.portName = "Dialogue Connection";
            inputContainer.Add(inputPort);

            // EXTENSIONS CONTAINER

            VisualElement customDataContainer = new VisualElement();

            customDataContainer.AddToClassList("ds-node_custom-data-container");


            Foldout textFoldout = DSElementUtility.CreateFoldout("Dialogue Text");

            Label nameLabel =
                DSElementUtility.CreateLabel("Speaker Name");
            nameLabel.style.fontSize = 14;
            Label nameSubLabel =
                DSElementUtility.CreateLabel("(leave blank if same as previous connected node)");
            nameSubLabel.style.fontSize = 10;

            TextField nameTextField = DSElementUtility.CreateTextArea(DialogueSpeaker, null, callback =>
            {
                DialogueSpeaker = callback.newValue;
            });


            Label textLabel =
                DSElementUtility.CreateLabel("Dialogue Text");
            textLabel.style.fontSize = 14;

            TextField textTextField = DSElementUtility.CreateTextArea(Text, null, callback =>
            {
                Text = callback.newValue;
            });

            nameLabel.AddClasses("ds-node_label");
            nameSubLabel.AddClasses("ds-node_sub-label");

            textLabel.AddClasses("ds-node_label");

            nameTextField.AddClasses(
                "ds-node_textfield",
                "ds-node_quote-textfield"
            );
            textTextField.AddClasses(
                "ds-node_textfield",
                "ds-node_quote-textfield"
            );

            textFoldout.Add(nameLabel);
            textFoldout.Add(nameSubLabel);
            textFoldout.Add(nameTextField);
            textFoldout.Add(textLabel);
            textFoldout.Add(textTextField);

            customDataContainer.Add(textFoldout);

            extensionContainer.Add(customDataContainer);
        }

        #region Utility Methods

        public void DisconnectAllPorts()
        {
            DisconnectPorts(inputContainer);
            DisconnectPorts(outputContainer);
        }

        private void DisconnectPorts(VisualElement container)
        {
            foreach (Port port in container.Children())
            {
                if (!port.connected)
                {
                    continue;
                }
                GraphView.DeleteElements(port.connections);
            }
        }

        public bool IsStartingNode()
        {
            Port inputPort = (Port)inputContainer.Children().First();

            return !inputPort.connected;
        }

        private void DisconnectInputPorts()
        {
            DisconnectPorts(inputContainer);
        }

        private void DisconnectOutputPorts()
        {
            DisconnectPorts(outputContainer);
        }

        public void SetErrorStyle(Color color)
        {
            mainContainer.style.backgroundColor = color;
        }

        public void ResetStyle()
        {
            mainContainer.style.backgroundColor = _defaultBackgroundColor;
        }

        #endregion
    }
}