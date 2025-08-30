using DialogueSystem.Data;
using DialogueSystem.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem
{
    public class DialogueChoice : MonoBehaviour
    {
       [SerializeField] private TMP_Text choiceText;
        [SerializeField]private Button button;
        
        private DSDialogueChoiceData _choice;

        void Start()
        {
            button.onClick.AddListener(ButtonPressed);
        }

        public void SetChoice(DSDialogueChoiceData choice, int index)
        {
            _choice = choice;

            choiceText.text = $"{index}: {_choice.Text}";
        }

        private void ButtonPressed()
        {
            if (_choice.ActionType != DSActionType.None)
            {
                DSDialogueActionHandler.Instance?.HandleDialogueAction(_choice.ActionType, _choice.ActionParameter);
            }

            if (_choice.NextDialogue == null) {
                DialogueManager.Instance.EndDialogue();
            }
            else {
                DialogueManager.Instance.StartDialogue(_choice.NextDialogue);
            }
        }
    }
}