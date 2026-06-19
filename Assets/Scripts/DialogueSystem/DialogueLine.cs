using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public DialogueSpeaker speaker;
    [TextArea(2, 4)]
    public string text;
    public DialogueChoice[] choices; // vacío = no hay choices en esta línea
}