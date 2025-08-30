using System.Collections.Generic;
using DialogueSystem.Elements;
using UnityEngine;

namespace DialogueSystem.Data.Error
{
    public class DSNodeErrorData
    {
        public DSErrorData ErrorData { get; } = new();
        public List<DSNode> Nodes { get; } = new();
    }
}
