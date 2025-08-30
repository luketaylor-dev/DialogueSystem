using System;
using System.Collections.Generic;
using System.Text;
using DialogueSystem.Data.Error;
using DialogueSystem.Data.Save;
using DialogueSystem.Elements;
using DialogueSystem.Enums;
using DialogueSystem.Utilities;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem.Windows
{
    public class DSGraphView : GraphView
    {
        private readonly DSEditorWindow _editorWindow;
        private DSSearchWindow _searchWindow;

        private MiniMap _miniMap;

        private readonly SerializableDictionary<string, DSNodeErrorData> _ungroupedNodes;
        private readonly SerializableDictionary<string, DSGroupErrorData> _groups;
        private readonly SerializableDictionary<Group, SerializableDictionary<string, DSNodeErrorData>> _groupedNodes;

        private int _nameErrorAmount;

        public int NameErrorAmount
        {
            get => _nameErrorAmount;

            set
            {
                _nameErrorAmount = value;
                switch (_nameErrorAmount)
                {
                    case 0:
                        _editorWindow.EnableSaving();
                        break;
                    case 1:
                        _editorWindow.DisableSaving();
                        break;
                }
            }
        }

        public DSGraphView(DSEditorWindow dsEditorWindow)
        {
            _editorWindow = dsEditorWindow;

            _ungroupedNodes = new SerializableDictionary<string, DSNodeErrorData>();
            _groups = new SerializableDictionary<string, DSGroupErrorData>();
            _groupedNodes = new SerializableDictionary<Group, SerializableDictionary<string, DSNodeErrorData>>();

            AddSearchWindow();
            AddManipulators();
            AddMiniMap();
            AddGridBackground();

            OnElementsDeleted();
            OnGroupElementsAdded();
            OnGroupElementsRemoved();
            OnGroupRenamed();
            OnGraphViewChanged();

            AddStyles();
            AddMiniMapStyles();
        }



        #region Overrided Methods

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatabilePorts = new();

            foreach (var port in ports)
            {
                if (startPort == port)
                {
                    continue;
                }

                if (startPort.node == port.node)
                {
                    continue;
                }

                if (startPort.direction == port.direction)
                {
                    continue;
                }

                compatabilePorts.Add(port);
            }

            return compatabilePorts;
        }

        #endregion

        #region Manipulators

        private void AddManipulators()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            //this.AddManipulator(CreateNodeContextualMenu("Add Node (Single Choice)", DSDialogueType.SingleChoice));
            this.AddManipulator(CreateNodeContextualMenu("Add Node (Multiple Choice)", DSDialogueType.MultipleChoice));

            this.AddManipulator(CreateGroupContextualMenu());
        }

        private IManipulator CreateGroupContextualMenu()
        {
            ContextualMenuManipulator contextualMenuManipulator = new(
                menuEvent => menuEvent.menu.AppendAction("Add Group",
                    actionEvent => CreateGroup("DialogueGroup",
                        GetLocalMousePosition(actionEvent.eventInfo.localMousePosition)))
            );
            return contextualMenuManipulator;
        }


        private IManipulator CreateNodeContextualMenu(string actionTitle, DSDialogueType dialogueType)
        {
            ContextualMenuManipulator contextualMenuManipulator = new(
                menuEvent => menuEvent.menu.AppendAction(actionTitle,
                    actionEvent => AddElement(CreateNode("DialogueName", dialogueType,
                        GetLocalMousePosition(actionEvent.eventInfo.localMousePosition))))
            );
            return contextualMenuManipulator;
        }

        #endregion

        #region Element Creation

        public GraphElement CreateGroup(string title, Vector2 localMousePosition)
        {
            DSGroup group = new(title, localMousePosition);

            AddGroup(group);
            AddElement(group);


            foreach (GraphElement selectedElement in selection)
            {
                if (selectedElement is not DSNode)
                {
                    continue;
                }

                DSNode node = (DSNode)selectedElement;
                group.AddElement(node);
            }

            return group;
        }


        public DSNode CreateNode(string nodeName, DSDialogueType dialogueType, Vector2 position, bool shouldDraw = true)
        {
            Type nodeType = Type.GetType($"DialogueSystem.Elements.DS{dialogueType}Node");
            DSNode node = (DSNode)Activator.CreateInstance(nodeType);

            node.Initialize(nodeName, this, position);
            if (shouldDraw)
                node.Draw();

            AddUngroupedNode(node);

            return node;
        }

        #endregion

        #region Repeated Elements

        public void AddUngroupedNode(DSNode node)
        {
            string nodeName = node.DialogueName.ToLower();

            if (!_ungroupedNodes.ContainsKey(nodeName))
            {
                DSNodeErrorData nodeErrorData = new DSNodeErrorData();

                nodeErrorData.Nodes.Add(node);

                _ungroupedNodes.Add(nodeName, nodeErrorData);
                return;
            }

            List<DSNode> ungroupedNodesList = _ungroupedNodes[nodeName].Nodes;

            ungroupedNodesList.Add(node);

            Color errorColor = _ungroupedNodes[nodeName].ErrorData.Color;

            node.SetErrorStyle(errorColor);

            if (ungroupedNodesList.Count == 2)
            {
                NameErrorAmount++;
                ungroupedNodesList[0].SetErrorStyle(errorColor);
            }
        }


        private void AddGroup(DSGroup group)
        {
            string groupName = group.title.ToLower();

            if (!_groups.ContainsKey(groupName))
            {
                DSGroupErrorData groupErrorData = new DSGroupErrorData();

                groupErrorData.Groups.Add(group);

                _groups.Add(groupName, groupErrorData);
                return;
            }

            List<DSGroup> groupsList = _groups[groupName].Groups;

            groupsList.Add(group);

            Color errorColor = _groups[groupName].ErrorData.Color;

            group.SetErrorStyle(errorColor);

            if (groupsList.Count == 2)
            {
                NameErrorAmount++;
                groupsList[0].SetErrorStyle(errorColor);
            }
        }
        private void RemoveGroup(DSGroup group)
        {
            string oldGroupName = group.OldTitle.ToLower();

            List<DSGroup> groupsList = _groups[oldGroupName].Groups;

            groupsList.Remove(group);

            group.ResetStyle();

            if (groupsList.Count == 1)
            {
                NameErrorAmount--;
                groupsList[0].ResetStyle();
                return;
            }

            if (groupsList.Count == 0)
            {
                _groups.Remove(oldGroupName);
            }
        }
        public void RemoveUngroupedNode(DSNode node)
        {
            string nodeName = node.DialogueName.ToLower();

            List<DSNode> ungroupedNodesList = _ungroupedNodes[nodeName].Nodes;

            ungroupedNodesList.Remove(node);

            node.ResetStyle();

            if (ungroupedNodesList.Count == 1)
            {
                NameErrorAmount--;
                ungroupedNodesList[0].ResetStyle();
                return;
            }

            if (ungroupedNodesList.Count == 0)
            {
                _ungroupedNodes.Remove(nodeName);
            }
        }

        public void AddGroupedNode(DSNode node, DSGroup group)
        {
            string nodeName = node.DialogueName.ToLower();

            node.Group = group;

            if (!_groupedNodes.ContainsKey((group)))
            {
                _groupedNodes.Add(group, new SerializableDictionary<string, DSNodeErrorData>());
            }

            if (!_groupedNodes[group].ContainsKey(nodeName))
            {
                DSNodeErrorData nodeErrorData = new DSNodeErrorData();

                nodeErrorData.Nodes.Add(node);

                _groupedNodes[group].Add(nodeName, nodeErrorData);

                return;
            }

            List<DSNode> groupedNodesList = _groupedNodes[group][nodeName].Nodes;

            groupedNodesList.Add(node);

            Color errorColor = _groupedNodes[group][nodeName].ErrorData.Color;

            node.SetErrorStyle(errorColor);

            if (groupedNodesList.Count == 2)
            {
                NameErrorAmount++;
                groupedNodesList[0].SetErrorStyle(errorColor);
            }
        }

        public void RemoveGroupedNode(DSNode node, Group group)
        {
            string nodeName = node.DialogueName.ToLower();

            node.Group = null;

            List<DSNode> groupedNodeList = _groupedNodes[group][nodeName].Nodes;

            groupedNodeList.Remove(node);

            node.ResetStyle();

            if (groupedNodeList.Count == 1)
            {
                NameErrorAmount--;
                groupedNodeList[0].ResetStyle();

                return;
            }

            if (groupedNodeList.Count == 0)
            {
                _groupedNodes[group].Remove(nodeName);

                if (_groupedNodes[group].Count == 0)
                {
                    _groupedNodes.Remove(group);
                }
            }

        }

        #endregion

        #region Element Addition

        private void AddStyles()
        {
            // Load stylesheets from the module's Resources folder
            var graphViewStyles = Resources.Load<StyleSheet>("DSGraphViewStyles");
            var nodeStyles = Resources.Load<StyleSheet>("DSNodeStyles");
            
            if (graphViewStyles != null)
                styleSheets.Add(graphViewStyles);
            if (nodeStyles != null)
                styleSheets.Add(nodeStyles);
        }

        private void AddMiniMapStyles()
        {
            StyleColor backgroundColor = new StyleColor(new Color32(29, 29, 30, 255));
            StyleColor borderColor = new StyleColor(new Color32(51, 51, 51, 255));

            _miniMap.style.backgroundColor = backgroundColor;
            _miniMap.style.borderBottomColor = borderColor;
            _miniMap.style.borderTopColor = borderColor;
            _miniMap.style.borderRightColor = borderColor;
            _miniMap.style.borderLeftColor = borderColor;
        }
        private void AddGridBackground()
        {
            GridBackground gridBackground = new GridBackground();

            gridBackground.StretchToParentSize();

            Insert(0, gridBackground);
        }

        private void AddSearchWindow()
        {
            if (_searchWindow == null)
            {
                _searchWindow = ScriptableObject.CreateInstance<DSSearchWindow>();

                _searchWindow.Initialise(this);
            }

            nodeCreationRequest = context =>
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
        }
        private void AddMiniMap()
        {
            _miniMap = new MiniMap()
            {
                anchored = true,
            };

            _miniMap.SetPosition(new Rect(15, 50, 200, 180));

            Add(_miniMap);

            _miniMap.visible = false;
        }
        #endregion

        #region Callbacks

        private void OnElementsDeleted()
        {
            deleteSelection = (operation, askUser) =>
            {
                Type groupType = typeof(DSGroup);
                Type edgeType = typeof(Edge);

                List<DSNode> nodesToDelete = new List<DSNode>();
                List<Edge> edgesToDelete = new List<Edge>();
                List<DSGroup> groupsToDelete = new List<DSGroup>();
                foreach (GraphElement element in selection)
                {
                    if (element is DSNode node)
                    {
                        nodesToDelete.Add(node);
                        continue;
                    }

                    if (element.GetType() == edgeType)
                    {
                        Edge edge = (Edge)element;
                        edgesToDelete.Add(edge);
                        continue;
                    }

                    if (element.GetType() != groupType)
                    {
                        continue;
                    }

                    DSGroup group = (DSGroup)element;

                    groupsToDelete.Add(group);
                }



                foreach (var group in groupsToDelete)
                {
                    List<DSNode> groupNodes = new List<DSNode>();

                    foreach (GraphElement groupElement in group.containedElements)
                    {
                        if (groupElement is not DSNode node)
                        {
                            continue;
                        }

                        groupNodes.Add(node);
                    }

                    group.RemoveElements(groupNodes);
                    RemoveGroup(group);
                    RemoveElement(group);
                }

                DeleteElements(edgesToDelete);

                foreach (var node in nodesToDelete)
                {
                    if (node.Group != null)
                    {
                        node.Group.RemoveElement(node);
                    }
                    RemoveUngroupedNode(node);

                    node.DisconnectAllPorts();

                    RemoveElement(node);
                }
            };
        }



        private void OnGroupElementsAdded()
        {
            elementsAddedToGroup = (group, elements) =>
            {
                foreach (GraphElement element in elements)
                {
                    if (element is not DSNode)
                    {
                        continue;
                    }

                    DSGroup nodeGroup = (DSGroup)group;
                    DSNode node = (DSNode)element;

                    RemoveUngroupedNode(node);
                    AddGroupedNode(node, nodeGroup);
                }
            };
        }

        private void OnGroupElementsRemoved()
        {
            elementsRemovedFromGroup = (group, elements) =>
            {
                foreach (GraphElement element in elements)
                {
                    if (element is not DSNode)
                    {
                        continue;
                    }

                    DSNode node = (DSNode)element;

                    RemoveGroupedNode(node, group);
                    AddUngroupedNode(node);

                }
            };
        }

        private void OnGroupRenamed()
        {
            groupTitleChanged = (group, newTitle) =>
            {
                DSGroup dsGroup = (DSGroup)group;

                dsGroup.title = newTitle.RemoveWhitespaceAndSpecialCharacters();


                if (string.IsNullOrEmpty(dsGroup.title))
                {
                    if (!string.IsNullOrEmpty(dsGroup.OldTitle))
                    {
                        NameErrorAmount++;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(dsGroup.OldTitle))
                    {
                        NameErrorAmount--;
                    }
                }

                RemoveGroup(dsGroup);

                dsGroup.OldTitle = dsGroup.title;

                AddGroup(dsGroup);
            };
        }

        private void OnGraphViewChanged()
        {
            graphViewChanged = changes =>
            {
                if (changes.edgesToCreate != null)
                {
                    foreach (Edge edge in changes.edgesToCreate)
                    {
                        DSNode nextNode = (DSNode)edge.input.node;

                        DSChoiceSaveData choiceData = (DSChoiceSaveData)edge.output.userData;

                        choiceData.NodeID = nextNode.ID;
                    }
                }

                if (changes.elementsToRemove != null)
                {
                    Type edgeType = typeof(Edge);

                    foreach (GraphElement element in changes.elementsToRemove)
                    {
                        if (element.GetType() != edgeType)
                        {
                            continue;
                        }

                        Edge edge = (Edge)element;
                        DSChoiceSaveData choiceData = (DSChoiceSaveData)edge.output.userData;

                        choiceData.NodeID = string.Empty;
                    }
                }
                return changes;
            };
        }

        #endregion

        #region Utilities

        public Vector2 GetLocalMousePosition(Vector2 mousePosition, bool isSearchWindow = false)
        {
            Vector2 worldMousePosition = mousePosition;

            if (isSearchWindow)
            {
                worldMousePosition -= _editorWindow.position.position;
            }

            Vector2 localMousePosition = contentViewContainer.WorldToLocal(worldMousePosition);

            return localMousePosition;
        }

        public void ClearGraph()
        {
            graphElements.ForEach(RemoveElement);

            _groups.Clear();
            _groupedNodes.Clear();
            _ungroupedNodes.Clear();

            NameErrorAmount = 0;


        }

        public void ToggleMiniMap()
        {
            _miniMap.visible = !_miniMap.visible;
        }
        #endregion
    }
}