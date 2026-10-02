using UnityEngine;
using UnityEngine.Events;

public class MessageInteractable : Interactable
{
    [SerializeField] private DialogueLine[] lines;
    [SerializeField] private bool fadeOutOnEnd = true;

    [Tooltip("Se invoca cuando termina ESTE diálogo en particular (después del fade, si " +
             "está activado). Útil para disparar algo puntual al terminar, como la pantalla " +
             "de 'Gracias por jugar' en el mensaje final del juego.")]
    public UnityEvent onMessageEnded;

    public override void Interact(Player player)
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
        }

        DialogueManager.Instance.StartDialogue(lines);
    }

    private void HandleDialogueEnded()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;

        if (fadeOutOnEnd)
            SceneTransitioner.Instance?.FadeScreen(1f);

        onMessageEnded?.Invoke();
    }

    private void OnDisable()
    {
        // Limpieza de seguridad por si el objeto se desactiva antes de terminar
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
    }
}