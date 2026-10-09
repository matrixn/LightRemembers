using System;
using UnityEngine;

namespace LightRemembers.Narrative
{
    [Serializable]
    public struct DialogueLine
    {
        public string speaker;
        [TextArea(2, 4)] public string text;
        [Min(0.1f)] public float duration;

        public DialogueLine(string lineSpeaker, string lineText, float lineDuration)
        {
            speaker = lineSpeaker;
            text = lineText;
            duration = lineDuration;
        }
    }

    [CreateAssetMenu(menuName = "Light Remembers/Dialogue Sequence")]
    public sealed class DialogueSequence : ScriptableObject
    {
        [SerializeField] private DialogueLine[] lines;
        public DialogueLine[] Lines => lines;

        public void Configure(DialogueLine[] sequenceLines) => lines = sequenceLines;
    }
}
