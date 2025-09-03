using System;
using System.Collections.Generic;
using System.Text;
using DialogueSystem.Data.Error;
using DialogueSystem.Data.Save;
using DialogueSystem.Elements;
using DialogueSystem.Enums;
using DialogueSystem.Utilities;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq; // Added for FirstOrDefault

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
            RegisterKeyboardShortcuts();
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

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            
            // Add duplicate option for multiple selected nodes
            if (selection.Count > 1)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Duplicate Selection", _ => DuplicateSelection());
            }
            
            // Add copy/paste options
            if (selection.Count > 0)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Copy", _ => CopySelection());
            }
            
            // Add paste option if we have copied data
            if (HasCopiedData())
            {
                evt.menu.AppendAction("Paste", _ => PasteSelection());
            }
        }

        private void RegisterKeyboardShortcuts()
        {
            // Register keyboard shortcuts
            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            // Handle keyboard shortcuts
            if (evt.ctrlKey || evt.commandKey)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.C:
                        if (selection.Count > 0)
                        {
                            CopySelection();
                            evt.StopPropagation();
                        }
                        break;
                    case KeyCode.V:
                        if (HasCopiedData())
                        {
                            PasteSelection();
                            evt.StopPropagation();
                        }
                        break;
                    case KeyCode.D:
                        if (selection.Count > 0)
                        {
                            DuplicateSelection();
                            evt.StopPropagation();
                        }
                        break;
                }
            }
        }

        private bool HasCopiedData()
        {
            // Check if we have copied node data in the clipboard
            return !string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer) && 
                   EditorGUIUtility.systemCopyBuffer.StartsWith("DSNodeData:");
        }

        private void CopySelection()
        {
            var nodeDataList = new List<string>();
            
            foreach (var element in selection)
            {
                if (element is DSNode node)
                {
                    var nodeData = new
                    {
                        DialogueName = node.DialogueName,
                        DialogueType = node.DialogueType.ToString(),
                        Text = node.Text,
                        DialogueSpeaker = node.DialogueSpeaker,
                        Choices = node.Choices,
                        Position = node.GetPosition().position
                    };
                    
                    nodeDataList.Add(JsonUtility.ToJson(nodeData));
                }
            }
            
            if (nodeDataList.Count > 0)
            {
                EditorGUIUtility.systemCopyBuffer = "DSNodeData:" + string.Join("|", nodeDataList);
            }
        }

        private void PasteSelection()
        {
            if (!HasCopiedData()) return;
            
            var data = EditorGUIUtility.systemCopyBuffer.Substring("DSNodeData:".Length);
            var nodeDataStrings = data.Split('|');
            
            ClearSelection();
            
            foreach (var nodeDataString in nodeDataStrings)
            {
                try
                {
                    var nodeData = JsonUtility.FromJson<NodeData>(nodeDataString);
                    var dialogueType = (DSDialogueType)System.Enum.Parse(typeof(DSDialogueType), nodeData.DialogueType);
                    
                    Vector2 newPosition = nodeData.Position + new Vector2(50, 50);
                    DSNode pastedNode = CreateNode(nodeData.DialogueName + " Copy", dialogueType, newPosition, false);
                    
                    pastedNode.Text = nodeData.Text;
                    pastedNode.DialogueSpeaker = nodeData.DialogueSpeaker;
                    pastedNode.Choices = nodeData.Choices ?? new List<DSChoiceSaveData>();
                    
                    pastedNode.Draw();
                    AddToSelection(pastedNode);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to paste node: {e.Message}");
                }
            }
        }

        [System.Serializable]
        private class NodeData
        {
            public string DialogueName;
            public string DialogueType;
            public string Text;
            public string DialogueSpeaker;
            public List<DSChoiceSaveData> Choices;
            public Vector2 Position;
        }

        private void DuplicateSelection()
        {
            Debug.Log("DuplicateSelection called");
            var duplicatedNodes = new List<DSNode>();
            var originalToDuplicatedMap = new Dictionary<DSNode, DSNode>(); // Map original nodes to duplicated nodes
            
            Debug.Log($"Selection count: {selection.Count}");
            
            // First pass: Create all duplicated nodes
            foreach (var element in selection)
            {
                Debug.Log($"Processing element: {element.GetType()}");
                if (element is DSNode node)
                {
                    Debug.Log($"Duplicating node: {node.DialogueName}");
                    Vector2 newPosition = node.GetPosition().position + new Vector2(50, 50);
                    DSNode duplicatedNode = CreateNode(node.DialogueName + " Copy", node.DialogueType, newPosition, false);
                    
                    // Copy the node data
                    duplicatedNode.Text = node.Text;
                    duplicatedNode.DialogueSpeaker = node.DialogueSpeaker;
                    duplicatedNode.Choices = new List<DSChoiceSaveData>();
                    
                    // Deep copy the choices
                    foreach (var choice in node.Choices)
                    {
                        var newChoice = new DSChoiceSaveData
                        {
                            Text = choice.Text,
                            ActionType = choice.ActionType,
                            ActionParameter = choice.ActionParameter
                        };
                        duplicatedNode.Choices.Add(newChoice);
                    }
                    
                    // Store the mapping
                    originalToDuplicatedMap[node] = duplicatedNode;
                    duplicatedNodes.Add(duplicatedNode);
                    Debug.Log($"Successfully duplicated node: {duplicatedNode.DialogueName}");
                }
            }
            
            // Second pass: Restore connections between duplicated nodes
            foreach (var element in selection)
            {
                if (element is DSNode originalNode && originalToDuplicatedMap.ContainsKey(originalNode))
                {
                    DSNode duplicatedNode = originalToDuplicatedMap[originalNode];
                    
                    // Check each choice in the original node
                    for (int choiceIndex = 0; choiceIndex < originalNode.Choices.Count; choiceIndex++)
                    {
                        var originalChoice = originalNode.Choices[choiceIndex];
                        
                        // If this choice connects to another node in our selection
                        if (!string.IsNullOrEmpty(originalChoice.NodeID))
                        {
                            // Find the original target node
                            var targetOriginalNode = nodes.FirstOrDefault(n => n is DSNode dsNode && dsNode.ID == originalChoice.NodeID);
                            
                            // If the target is also in our selection, update the connection
                            if (targetOriginalNode != null && originalToDuplicatedMap.ContainsKey((DSNode)targetOriginalNode))
                            {
                                var duplicatedTargetNode = originalToDuplicatedMap[(DSNode)targetOriginalNode];
                                duplicatedNode.Choices[choiceIndex].NodeID = duplicatedTargetNode.ID;
                                Debug.Log($"Restored connection from {duplicatedNode.DialogueName} to {duplicatedTargetNode.DialogueName}");
                            }
                        }
                    }
                }
            }
            
            // Redraw all duplicated nodes to show the connections
            foreach (var duplicatedNode in duplicatedNodes)
            {
                duplicatedNode.Draw();
            }
            
            // Third pass: Create visual edges after all nodes are drawn
            foreach (var element in selection)
            {
                if (element is DSNode originalNode && originalToDuplicatedMap.ContainsKey(originalNode))
                {
                    DSNode duplicatedNode = originalToDuplicatedMap[originalNode];
                    
                    // Check each choice in the original node
                    for (int choiceIndex = 0; choiceIndex < originalNode.Choices.Count; choiceIndex++)
                    {
                        var originalChoice = originalNode.Choices[choiceIndex];
                        
                        // If this choice connects to another node in our selection
                        if (!string.IsNullOrEmpty(originalChoice.NodeID))
                        {
                            // Find the original target node
                            var targetOriginalNode = nodes.FirstOrDefault(n => n is DSNode dsNode && dsNode.ID == originalChoice.NodeID);
                            
                            // If the target is also in our selection, create the visual connection
                            if (targetOriginalNode != null && originalToDuplicatedMap.ContainsKey((DSNode)targetOriginalNode))
                            {
                                var duplicatedTargetNode = originalToDuplicatedMap[(DSNode)targetOriginalNode];
                                
                                // Create the visual edge using the existing connection system
                                CreateConnection(duplicatedNode, choiceIndex, duplicatedTargetNode);
                                Debug.Log($"Created visual connection from {duplicatedNode.DialogueName} to {duplicatedTargetNode.DialogueName}");
                            }
                        }
                    }
                }
            }
            
            // Clear selection and select the new nodes
            ClearSelection();
            foreach (var node in duplicatedNodes)
            {
                AddToSelection(node);
            }
            
            Debug.Log($"DuplicateSelection completed. Created {duplicatedNodes.Count} nodes with preserved connections.");
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
            {
                node.Draw();
            }

            AddElement(node); // Add the node to the graph view
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

        #region Connection Helpers

        private void CreateConnection(DSNode sourceNode, int choiceIndex, DSNode targetNode)
        {
            try
            {
                // Get the output port (choice port) from the source node
                var outputPorts = sourceNode.outputContainer.Children().OfType<Port>().ToList();
                if (choiceIndex < outputPorts.Count)
                {
                    var outputPort = outputPorts[choiceIndex];
                    
                    // Get the input port from the target node
                    var inputPorts = targetNode.inputContainer.Children().OfType<Port>().ToList();
                    if (inputPorts.Count > 0)
                    {
                        var inputPort = inputPorts[0];
                        
                        // Create the edge
                        var edge = outputPort.ConnectTo(inputPort);
                        if (edge != null)
                        {
                            AddElement(edge);
                            Debug.Log($"Successfully created edge from {sourceNode.DialogueName} to {targetNode.DialogueName}");
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to create edge from {sourceNode.DialogueName} to {targetNode.DialogueName}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"No input ports found on target node {targetNode.DialogueName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Choice index {choiceIndex} out of range for node {sourceNode.DialogueName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating connection: {e.Message}");
            }
        }

        #endregion
    }
}