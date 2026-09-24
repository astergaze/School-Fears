using UnityEngine;

public class MessageInteractable : Interactable
{
    [SerializeField] private DialogueLine[] lines;
    [SerializeField] private bool fadeOutOnEnd = true;

    public override void Interact(Player player)
    {
        if (fadeOutOnEnd && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
        }

        DialogueManager.Instance.StartDialogue(lines);
    }

    private void HandleDialogueEnded()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;

        SceneTransitioner.Instance?.FadeScreen(1f);
    }

    private void OnDisable()
    {
        // Limpieza de seguridad por si el objeto se desactiva antes de terminar
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
    }
}