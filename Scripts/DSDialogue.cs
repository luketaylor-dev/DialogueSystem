using DialogueSystem.Data;
using DialogueSystem.ScriptableObjects;
using DialogueSystem.Enums;
using UnityEngine;
using UnityEngine.Serialization;

namespace DialogueSystem
{
    public class DSDialogue : MonoBehaviour
    {
        //Dialog Scripatble Objects
        [SerializeField] private DSDialogueContainerSO dialogueContainer;
        [SerializeField] private DSDialogueGroupSO dialogueGroup;
        [SerializeField] private DSDialogueSO dialogue;

        //Filters
        [SerializeField] private bool groupedDialogues;
        [SerializeField] private bool startingDialoguesOnly;

        // Indexes
        [SerializeField] private int selectedDialogueGroupIndex;
        [SerializeField] private int selectedDialogueIndex;

        public DSDialogueSO GetDialogueSO()
        {
            return dialogue;
        }

        public void HandleChoiceAction(DSDialogueChoiceData choiceData)
        {
            if (choiceData.ActionType != DSActionType.None)
            {
                DSDialogueActionHandler.Instance?.HandleDialogueAction(choiceData.ActionType, choiceData.ActionParameter);
            }
        }

        // Convenience method to trigger actions directly
        public void TriggerAction(DSActionType actionType, string actionParameter = "")
        {
            DSDialogueActionHandler.Instance?.HandleDialogueAction(actionType, actionParameter);
        }
    }
}