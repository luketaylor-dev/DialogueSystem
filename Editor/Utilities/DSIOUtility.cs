using System;
using System.Collections.Generic;
using System.Linq;
using DialogueSystem.Data;
using DialogueSystem.Data.Save;
using DialogueSystem.Elements;
using DialogueSystem.ScriptableObjects;
using DialogueSystem.Windows;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using DSGroup = DialogueSystem.Elements.DSGroup;
using Edge = UnityEditor.Experimental.GraphView.Edge;

namespace DialogueSystem.Utilities
{
    public class DSIOUtility
    {
        private static DSGraphView _graphView;
        private static string _graphFileName;
        private static string _containerFolderPath;

        private static List<DSGroup> groups;
        private static List<DSNode> nodes;

        private static Dictionary<string, DSDialogueGroupSO> _createdDialogueGroups;
        private static Dictionary<string, DSDialogueSO> _createdDialogues;

        private static Dictionary<string, DSGroup> _loadedGroups;
        private static Dictionary<string, DSNode> _loadedNodes;

        public static void Initialize(DSGraphView graphView, string graphName)
        {
            _graphView = graphView;
            _graphFileName = graphName;
            _containerFolderPath = $"Assets/DialogueSystem/Dialogues/{_graphFileName}";
            groups = new List<DSGroup>();
            nodes = new List<DSNode>();

            _createdDialogueGroups = new Dictionary<string, DSDialogueGroupSO>();
            _createdDialogues = new Dictionary<string, DSDialogueSO>();

            _loadedGroups = new Dictionary<string, DSGroup>();
            _loadedNodes = new Dictionary<string, DSNode>();
        }

        #region Save Methods

        public static void Save()
        {
            CreateStaticFolders();

            GetElementsFromGraphView();

            DSGraphSaveDataSO graphData =
                CreateAsset<DSGraphSaveDataSO>("Assets/Editor/DialogueSystem/Graphs", $"{_graphFileName}Graph");

            graphData.Initialize(_graphFileName);

            DSDialogueContainerSO dialogueContainer =
                CreateAsset<DSDialogueContainerSO>(_containerFolderPath, _graphFileName);

            dialogueContainer.Initialize(_graphFileName);

            SaveGroups(graphData, dialogueContainer);
            SaveNodes(graphData, dialogueContainer);

            SaveAsset(graphData);
            SaveAsset(dialogueContainer);
        }

        #region Groups

        private static void SaveGroups(DSGraphSaveDataSO graphData, DSDialogueContainerSO dialogueContainer)
        {
            List<string> groupNames = new List<string>();
            foreach (var group in groups)
            {
                SaveGroupToGraph(group, graphData);
                SaveGroupToScriptableObject(group, dialogueContainer);

                groupNames.Add(group.title);
            }

            UpdateOldGroups(groupNames, graphData);
        }


        private static void SaveGroupToGraph(DSGroup group, DSGraphSaveDataSO graphData)
        {
            DSGroupSaveData groupData = new DSGroupSaveData()
            {
                ID = group.ID,
                Name = group.title,
                Position = group.GetPosition().position
            };

            graphData.Groups.Add(groupData);
        }

        private static void SaveGroupToScriptableObject(DSGroup group, DSDialogueContainerSO dialogueContainer)
        {
            string groupName = group.title;

            CreateFolder($"{_containerFolderPath}/Groups", groupName);
            CreateFolder($"{_containerFolderPath}/Groups/{groupName}", "Dialogues");

            DSDialogueGroupSO dialogueGroup =
                CreateAsset<DSDialogueGroupSO>($"{_containerFolderPath}/Groups/{groupName}", groupName);

            dialogueGroup.Initialize(groupName);

            _createdDialogueGroups.Add(group.ID, dialogueGroup);

            dialogueContainer.DialogueGroups[_createdDialogueGroups[group.ID]] = new List<DSDialogueSO>();

            SaveAsset(dialogueGroup);
        }

        private static void UpdateOldGroups(List<string> currentGroupNames, DSGraphSaveDataSO graphData)
        {
            if (graphData.OldGroupNames != null && graphData.OldGroupNames.Count != 0)
            {
                List<string> groupsToRemove = graphData.OldGroupNames.Except(currentGroupNames).ToList();

                foreach (var groupToRemove in groupsToRemove)
                {
                    RemoveFolder($"{_containerFolderPath}/Groups/{groupsToRemove}");
                }
            }

            graphData.OldGroupNames = new List<string>(currentGroupNames);
        }

        #endregion

        #region Nodes

        private static void SaveNodes(DSGraphSaveDataSO graphData, DSDialogueContainerSO dialogueContainer)
        {
            SerializableDictionary<string, List<string>> groupedNodeNames =
                new SerializableDictionary<string, List<string>>();
            List<string> ungroupedNodeNames = new List<string>();
            foreach (DSNode node in nodes)
            {
                SaveNodeToGraph(node, graphData);
                SaveNodeToScriptableObject(node, dialogueContainer);

                if (node.Group != null)
                {
                    groupedNodeNames.AddItem(node.Group.title, node.DialogueName);

                    continue;
                }

                ungroupedNodeNames.Add(node.DialogueName);
            }

            UpdateDialogueChoicesConnections();

            UpdateOldGroupedNodes(groupedNodeNames, graphData);
            UpdateOldUngroupedNodes(ungroupedNodeNames, graphData);
        }


        private static void SaveNodeToGraph(DSNode node, DSGraphSaveDataSO graphData)
        {
            List<DSChoiceSaveData> choices = CloneNodeChoices(node.Choices);

            DSNodeSaveData nodeData = new DSNodeSaveData()
            {
                ID = node.ID,
                Name = node.DialogueName,
                Choices = choices,
                DialogueSpeaker = node.DialogueSpeaker,
                Text = node.Text,
                GroupID = node.Group?.ID,
                DialogueType = node.DialogueType,
                Position = node.GetPosition().position
            };

            graphData.Nodes.Add(nodeData);
        }

        private static void SaveNodeToScriptableObject(DSNode node, DSDialogueContainerSO dialogueContainer)
        {
            DSDialogueSO dialogue;

            if (node.Group != null)
            {
                dialogue = CreateAsset<DSDialogueSO>($"{_containerFolderPath}/Groups/{node.Group.title}/Dialogues",
                    node.DialogueName);

                dialogueContainer.DialogueGroups[_createdDialogueGroups[node.Group.ID]].Add(dialogue);
            }
            else
            {
                dialogue = CreateAsset<DSDialogueSO>($"{_containerFolderPath}/Global/Dialogues", node.DialogueName);

                dialogueContainer.UngroupedDialogues.Add(dialogue);
            }

            dialogue.Initialize(node.DialogueName, node.DialogueSpeaker, node.Text, ConvertNodeChoicesToDialogueChoices(node.Choices),
                node.DialogueType, node.IsStartingNode());

            _createdDialogues.Add(node.ID, dialogue);

            SaveAsset(dialogue);
        }

        private static List<DSDialogueChoiceData> ConvertNodeChoicesToDialogueChoices(List<DSChoiceSaveData> nodeChoices)
        {
            List<DSDialogueChoiceData> dialogueChoices = new List<DSDialogueChoiceData>();

            foreach (DSChoiceSaveData nodeChoice in nodeChoices)
            {
                DSDialogueChoiceData choiceData = new DSDialogueChoiceData()
                {
                    Text = nodeChoice.Text,
                    ActionType = nodeChoice.ActionType,
                    ActionParameter = nodeChoice.ActionParameter
                };

                dialogueChoices.Add(choiceData);
            }

            return dialogueChoices;
        }

        private static void UpdateDialogueChoicesConnections()
        {
            foreach (DSNode node in nodes)
            {
                DSDialogueSO dialogue = _createdDialogues[node.ID];

                for (int choiceIndex = 0; choiceIndex < node.Choices.Count; choiceIndex++)
                {
                    DSChoiceSaveData nodeChoice = node.Choices[choiceIndex];

                    if (string.IsNullOrEmpty(nodeChoice.NodeID))
                    {
                        continue;
                    }

                    dialogue.Choices[choiceIndex].NextDialogue = _createdDialogues[nodeChoice.NodeID];

                    SaveAsset(dialogue);
                }
            }
        }

        private static void UpdateOldGroupedNodes(SerializableDictionary<string, List<string>> groupedNodeNames,
            DSGraphSaveDataSO graphData)
        {
            if (graphData.OldGroupNodeNames != null && graphData.OldGroupNodeNames.Count != 0)
            {
                foreach (KeyValuePair<string, List<string>> oldGroupNode in graphData.OldGroupNodeNames)
                {
                    List<string> nodesToRemove = new List<string>();

                    if (groupedNodeNames.ContainsKey(oldGroupNode.Key))
                    {
                        nodesToRemove = oldGroupNode.Value.Except(groupedNodeNames[oldGroupNode.Key]).ToList();
                    }

                    foreach (var nodeToRemove in nodesToRemove)
                    {
                        RemoveAsset($"{_containerFolderPath}/Groups/{oldGroupNode.Key}/Dialogues", nodeToRemove);
                    }
                }

                graphData.OldGroupNodeNames = new SerializableDictionary<string, List<string>>();

                foreach (var groupedNodeName in groupedNodeNames)
                {
                    graphData.OldGroupNodeNames.Add(groupedNodeName.Key, groupedNodeName.Value);
                }
            }
        }

        private static void UpdateOldUngroupedNodes(List<string> ungroupedNodeNames, DSGraphSaveDataSO graphData)
        {
            if (graphData.OldUngroupedNodeNames != null && graphData.OldUngroupedNodeNames.Count != 0)
            {
                List<string> nodesToRemove = graphData.OldUngroupedNodeNames.Except(ungroupedNodeNames).ToList();

                foreach (var nodeToRemove in nodesToRemove)
                {
                    RemoveAsset($"{_containerFolderPath}/Global/Dialogues", nodeToRemove);
                }
            }

            graphData.OldUngroupedNodeNames = new List<string>(ungroupedNodeNames);
        }

        #endregion

        #endregion

        #region Load Methods

        public static void Load()
        {
            DSGraphSaveDataSO graphData =
                LoadAsset<DSGraphSaveDataSO>("Assets/Editor/DialogueSystem/Graphs", _graphFileName);

            if (graphData == null)
            {
                EditorUtility.DisplayDialog(
                    "Couldn't load the file!",
                    "The file at the following path could not be found:\n\n" +
                    $"Assets/Editor/DialogueSystem/Graphs/{_graphFileName}\n\n" +
                    "Make sure you chose the right file and it's placed at the folder path mentioned above.",
                    "Thanks");
                return;
            }

            DSEditorWindow.UpdateFileName(graphData.FileName);

            LoadGroups(graphData.Groups);
            LoadNodes(graphData.Nodes);
            LoadNodesConnections();
        }


        private static void LoadGroups(List<DSGroupSaveData> groups)
        {
            foreach (var groupData in groups)
            {
                DSGroup group = (DSGroup)_graphView.CreateGroup(groupData.Name, groupData.Position);
                group.ID = groupData.ID;
                _loadedGroups.Add(group.ID, group);
            }
        }

        private static void LoadNodes(List<DSNodeSaveData> nodes)
        {
            foreach (DSNodeSaveData nodeData in nodes)
            {
                List<DSChoiceSaveData> choices = CloneNodeChoices(nodeData.Choices);
                DSNode node = _graphView.CreateNode(nodeData.Name, nodeData.DialogueType, nodeData.Position, false);

                node.ID = nodeData.ID;
                node.Choices = choices;
                node.DialogueSpeaker = nodeData.DialogueSpeaker;
                node.Text = nodeData.Text;

                node.Draw();

                _graphView.AddElement(node);

                _loadedNodes.Add(node.ID, node);

                if (string.IsNullOrEmpty(nodeData.GroupID))
                {
                    continue;
                }

                DSGroup group = _loadedGroups[nodeData.GroupID];

                node.Group = group;

                group.AddElement(node);
            }
        }

        private static void LoadNodesConnections()
        {
            foreach (KeyValuePair<string, DSNode> loadedNode in _loadedNodes)
            {
                foreach (Port choicePort in loadedNode.Value.outputContainer.Children())
                {
                    DSChoiceSaveData choiceData = (DSChoiceSaveData)choicePort.userData;

                    if (string.IsNullOrEmpty(choiceData.NodeID))
                    {
                        continue;
                    }

                    DSNode nextNode = _loadedNodes[choiceData.NodeID];
                    Port nextNodeInputPort = (Port)nextNode.inputContainer.Children().First();

                    Edge edge = choicePort.ConnectTo(nextNodeInputPort);

                    _graphView.AddElement(edge);

                    loadedNode.Value.RefreshPorts();
                }
            }
        }
        #endregion

        #region Fetch Methods

        private static void GetElementsFromGraphView()
        {
            Type groupType = typeof(DSGroup);
            _graphView.graphElements.ForEach(graphElement =>
            {
                if (graphElement is DSNode node)
                {
                    nodes.Add(node);
                    return;
                }

                if (graphElement.GetType() == groupType)
                {
                    DSGroup group = (DSGroup)graphElement;
                    groups.Add(group);
                    return;
                }
            });
        }

        #endregion

        #region Creation Methods

        private static void CreateStaticFolders()
        {
            CreateFolder("Assets/Editor/DialogueSystem", "Graphs");
            CreateFolder("Assets", "DialogueSystem");
            CreateFolder("Assets/DialogueSystem", "Dialogues");
            CreateFolder("Assets/DialogueSystem/Dialogues", _graphFileName);
            CreateFolder(_containerFolderPath, "Global");
            CreateFolder(_containerFolderPath, "Groups");
            CreateFolder($"{_containerFolderPath}/Global", "Dialogues");
        }

        #endregion

        #region Utility Methods

        private static void CreateFolder(string path, string folderName)
        {
            if (AssetDatabase.IsValidFolder($"{path}/{folderName}"))
            {
                return;
            }

            AssetDatabase.CreateFolder(path, folderName);
        }


        private static void RemoveFolder(string fullPath)
        {
            FileUtil.DeleteFileOrDirectory($"{fullPath}.meta");
            FileUtil.DeleteFileOrDirectory($"{fullPath}/");
        }

        private static T CreateAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            string fullPath = $"{path}/{assetName}.asset";

            T asset = LoadAsset<T>(path, assetName);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();

                AssetDatabase.CreateAsset(asset, fullPath);
            }

            return asset;
        }

        public static T LoadAsset<T>(string path, string assetName) where T : ScriptableObject
        {
            string fullPath = $"{path}/{assetName}.asset";
            return AssetDatabase.LoadAssetAtPath<T>(fullPath);
        }

        private static void SaveAsset(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void RemoveAsset(string path, string assetName)
        {
            AssetDatabase.DeleteAsset($"{path}/{assetName}.asset");
        }
        private static List<DSChoiceSaveData> CloneNodeChoices(List<DSChoiceSaveData> nodeChoices)
        {
            List<DSChoiceSaveData> choices = new List<DSChoiceSaveData>();

            foreach (DSChoiceSaveData choice in nodeChoices)
            {
                DSChoiceSaveData choiceData = new DSChoiceSaveData()
                {
                    Text = choice.Text,
                    NodeID = choice.NodeID,
                    ActionType = choice.ActionType,
                    ActionParameter = choice.ActionParameter
                };

                choices.Add(choiceData);
            }

            return choices;
        }
        #endregion
    }
}