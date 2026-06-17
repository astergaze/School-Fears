using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public DialogueSpeaker speaker;
    [TextArea(2, 4)]
    public string text;
}