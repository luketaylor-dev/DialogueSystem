using System;
using System.Collections.Generic;
using System.Linq;
using DialogueSystem.ScriptableObjects;
using TMPro;
using UnityEngine;

namespace DialogueSystem
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance;

        public bool DialogueActive = false;

        [SerializeField] private GameObject dialogueChoice;
        [SerializeField] private Transform dialogueChoiceContainer;
        [SerializeField] private TMP_Text currentSpokenLine; //TODO: probably need a better name for this
        [SerializeField] private TMP_Text speaker;

        private List<GameObject> activeChoices;

        /// <summary>Raised whenever the dialogue closes, whether a choice ended it or <see cref="EndDialogue"/> was called directly.</summary>
        public event Action DialogueEnded;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        
        private void Start()
        {
            Debug.Log("Dialogue Manager Initialized.");
            gameObject.SetActive(false);
        }

        public void StartDialogue(DSDialogue dialogue)
        {
            Debug.Log("Starting Dialogue...");
            var dialogueSO = dialogue.GetDialogueSO();
            if (dialogueSO.IsStartingDialogue)
            {
                DialogueActive = true;
                gameObject.SetActive(true);
            }
            StartDialogue(dialogueSO);
        }

        /// <summary>Shows the dialogue UI and starts at <paramref name="dialogue"/>, for games that pick the starting node themselves.</summary>
        public void OpenDialogue(DSDialogueSO dialogue)
        {
            DialogueActive = true;
            gameObject.SetActive(true);
            StartDialogue(dialogue);
        }

        public void StartDialogue(DSDialogueSO dialogue)
        {
            currentSpokenLine.text = dialogue.Text;
            speaker.text = dialogue.DialogueSpeaker;

            if (dialogue.Choices.Any())
            {
                GenerateChoices(dialogue.Choices);
            }
            else
            {
                EndDialogue();
            }
        }

        private void GenerateChoices(List<Data.DSDialogueChoiceData> choices)
        {
            ClearChoices();
            activeChoices = new List<GameObject>();
            for (int i = 0; i < choices.Count; i++)
            {
                Data.DSDialogueChoiceData choice = choices[i];
                //TODO: make this a pooling system to stop excessive instantiations
                var go = Instantiate(dialogueChoice, dialogueChoiceContainer);
                go.GetComponent<DialogueChoice>().SetChoice(choice, i + 1);
                activeChoices.Add(go);
            }
        }

        private void ClearChoices()
        {
            if (activeChoices == null) return;
            foreach (var go in activeChoices.ToList())
            {
                Destroy(go);
            }
        }

        public void EndDialogue()
        {
            //TODO: show a generic choice here, "end dialogue" or something
            bool wasActive = DialogueActive;
            gameObject.SetActive(false);
            DialogueActive = false;
            if (wasActive)
            {
                DialogueEnded?.Invoke();
            }
        }
    }
}