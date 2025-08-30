using System.Collections.Generic;
using DialogueSystem.Elements;
using DialogueSystem.Enums;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DialogueSystem.Windows
{
    public class DSSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private DSGraphView _graphView;
        private Texture2D _indentationIcon;

        public void Initialise(DSGraphView dsGraphView)
        {
            _graphView = dsGraphView;

            _indentationIcon = new Texture2D(1, 1);
            _indentationIcon.SetPixel(0, 0, Color.clear);
            _indentationIcon.Apply();
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> searchTreeEntries = new()
            {
                new SearchTreeGroupEntry(new GUIContent("Create Element")),
                new SearchTreeGroupEntry(new GUIContent("Dialogue Node"), 1),
                /*new SearchTreeEntry(new GUIContent("Single Choice", _indentationIcon))
                {
                    level = 2,
                    userData = DSDialogueType.SingleChoice,
                },*/
                new SearchTreeEntry(new GUIContent("Multiple Choice", _indentationIcon))
                {
                    level = 2,
                    userData = DSDialogueType.MultipleChoice,
                },
                new SearchTreeGroupEntry(new GUIContent("Dialogue Group"), 1),

                new SearchTreeEntry(new GUIContent("Single Group", _indentationIcon))
                {
                    level = 2,
                    userData = new Group()
                }
            };
            return searchTreeEntries;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            Vector2 localMousePosition = _graphView.GetLocalMousePosition(context.screenMousePosition, true);
            switch (searchTreeEntry.userData)
            {
                case DSDialogueType.SingleChoice:
                    DSSingleChoiceNode singleChoiceNode =
                        (DSSingleChoiceNode)_graphView.CreateNode("DialogueName", DSDialogueType.SingleChoice, localMousePosition);

                    _graphView.AddElement(singleChoiceNode);

                    return true;
                case DSDialogueType.MultipleChoice:
                    DSMultipleChoiceNode multipleChoiceNode =
                        (DSMultipleChoiceNode)_graphView.CreateNode("DialogueName", DSDialogueType.MultipleChoice, localMousePosition);

                    _graphView.AddElement(multipleChoiceNode);
                    return true;
                case Group _:
                    _graphView.CreateGroup("DialogueGroup", localMousePosition);
                    return true;
                default:
                    return false;
            }
        }
    }
}