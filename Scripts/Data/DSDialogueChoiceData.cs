using System;
using DialogueSystem.ScriptableObjects;
using DialogueSystem.Enums;
using UnityEngine;

namespace DialogueSystem.Data
{
    [Serializable]
    public class DSDialogueChoiceData
    {
        [field: SerializeField] public string Text { get; set; }
        [field: SerializeField] public DSDialogueSO NextDialogue { get; set; }
        [field: SerializeField] public DSActionType ActionType { get; set; }
        [field: SerializeField] public string ActionParameter { get; set; } 
    }
}
