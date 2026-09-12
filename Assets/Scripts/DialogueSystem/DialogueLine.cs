using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public DialogueSpeaker speaker;
    [Tooltip("ID de localizacion: debe existir en el CSV de dialogos (columna 'id')")]
    public string textId;
    public DialogueChoice[] choices; // vacío = no hay choices en esta línea
}