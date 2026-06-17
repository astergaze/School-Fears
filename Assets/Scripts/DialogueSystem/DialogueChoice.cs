using UnityEngine;

[System.Serializable]
public class DialogueChoice
{
    public string text;
    public ChoiceState state;
    public DialogueLine[] responseLines; // diálogo que se dispara después de elegir
}