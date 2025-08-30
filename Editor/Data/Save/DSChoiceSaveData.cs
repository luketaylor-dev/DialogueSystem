using System;
using DialogueSystem.Enums;
using UnityEngine;

namespace DialogueSystem.Data.Save
{
    [Serializable]
    public class DSChoiceSaveData
    {
        [field: SerializeField] public string Text { get; set; }
        [field: SerializeField] public string NodeID { get; set; }
        [field: SerializeField] public DSActionType ActionType { get; set; }
        [field: SerializeField] public string ActionParameter { get; set; }
    }
}
