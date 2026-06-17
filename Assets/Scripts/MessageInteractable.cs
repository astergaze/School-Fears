using UnityEngine;

public class MessageInteractable : Interactable
{
    [SerializeField] private DialogueLine[] lines;

    public override void Interact(Player player)
    {
        DialogueManager.Instance.StartDialogue(lines);
    }
}