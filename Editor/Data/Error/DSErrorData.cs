using UnityEngine;

namespace DialogueSystem.Data.Error
{
    public class DSErrorData
    {
        public Color Color { get; private set; }

        public DSErrorData()
        {
            GenerateRandomColor();
        }

        private void GenerateRandomColor()
        {
            Color = new Color32(
                (byte)Random.Range(65, 256),
                (byte)Random.Range(50, 176),
                (byte)Random.Range(50, 176),
                255
                );
        }
    }
}
