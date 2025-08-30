using System.Collections.Generic;
using DialogueSystem.Data;
using DialogueSystem.Enums;
using UnityEngine;

namespace DialogueSystem.ScriptableObjects
{
    public class DSDialogueSO : ScriptableObject
    {
        [field: SerializeField] public string DialogueName { get; set; }
        [field: SerializeField][field: TextArea] public string DialogueSpeaker { get; set; }
        [field: SerializeField][field: TextArea] public string Text { get; set; }
        [field: SerializeField] public List<DSDialogueChoiceData> Choices { get; set; }
        [field: SerializeField] public DSDialogueType DialogueType { get; set; }
        [field: SerializeField] public bool IsStartingDialogue { get; set; }

        public void Initialize(string dialogueName, string dialogueSpeaker, string text, List<DSDialogueChoiceData> choices, DSDialogueType dialogueType,
            bool isStartingDialog)
        {
            DialogueName = dialogueName;
            DialogueSpeaker = dialogueSpeaker;
            Text = text;
            Choices = choices;
            DialogueType = dialogueType;
            IsStartingDialogue = isStartingDialog;
        }
    }
}
