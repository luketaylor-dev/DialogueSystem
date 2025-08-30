using System.Collections.Generic;
using DialogueSystem.Elements;
using UnityEditor.Experimental.GraphView;

namespace DialogueSystem.Data.Error
{
    public class DSGroupErrorData
    {
        public DSErrorData ErrorData { get; } = new();
        public List<DSGroup> Groups { get; } = new();
    }
}
