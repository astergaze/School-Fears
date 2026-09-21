using UnityEngine;

[System.Serializable]
public class DialogueChoice
{
    [Tooltip("ID de localizacion: debe existir en el CSV de dialogos (columna 'id')")]
    public string textId;
    public ChoiceState state;
    public DialogueLine[] responseLines; // diálogo que se dispara después de elegir
    [Tooltip("ID interno (ej. 'leave_scene'). No es texto ni localización.")] // es un tipo de id que se va a encargar de acciones
    public string actionId;
}