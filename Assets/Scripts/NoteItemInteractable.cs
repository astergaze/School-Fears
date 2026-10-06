using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Interactuable híbrido: reproduce el diálogo de la nota y, al terminar,
/// entrega el ItemData al inventario, muestra el popup y desactiva la nota.
/// </summary>
public class NoteItemInteractable : Interactable
{
    [Header("Identificación")]
    [Tooltip("ID único para que GameManager recuerde si ya la levantaste (ej: 'note_1', 'note_2')")]
    [SerializeField] private string noteUniqueId = "note_1";

    [Header("Inventario")]
    [SerializeField] private ItemData noteItem;

    [Header("Diálogo")]
    [SerializeField] private DialogueLine[] lines;
    [SerializeField] private bool fadeOutOnEnd = false;

    [Header("Feedback de Objetivo")]
    [Tooltip("ID de localización para el popup (opcional). Si está vacío usa objectivePopupText")]
    [SerializeField] private string objectivePopupId;
    [Tooltip("Texto directo en caso de no usar CSV")]
    [SerializeField] private string objectivePopupText = "Nota recogida ({0}/4)";

    [Header("Sonido (Opcional)")]
    [SerializeField] private AudioClip pickupSound;

    public UnityEvent onNotePickedUp;

    private Player interactingPlayer;

    private void Start()
    {
        // Si el jugador ya recogió esta nota en esta partida y volvió a entrar al aula, la borramos
        if (GameManager.Instance != null && GameManager.Instance.IsNoteCollected(noteUniqueId))
        {
            gameObject.SetActive(false);
        }
    }

    public override void Interact(Player player)
    {
        interactingPlayer = player;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
            DialogueManager.Instance.StartDialogue(lines);
        }
        else
        {
            // Fallback directo si no hay DialogueManager
            CollectNote();
        }
    }

    private void HandleDialogueEnded()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;

        if (fadeOutOnEnd)
            SceneTransitioner.Instance?.FadeScreen(1f);

        CollectNote();
    }

    private void CollectNote()
    {
        // 1. Guardar en inventario
        if (noteItem != null && interactingPlayer != null && interactingPlayer.Inventory != null)
        {
            interactingPlayer.Inventory.AddItem(noteItem);
        }

        // 2. Registrar en GameManager persistente
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterNoteCollected(noteUniqueId);
        }

        // 3. SFX de papel/pickup
        if (pickupSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(pickupSound);
        }

        // 4. Disparar aviso visual de objetivo
        int totalCollected = GameManager.Instance != null ? GameManager.Instance.CollectedNotesCount : 1;
        ShowPickupPopup(totalCollected);

        onNotePickedUp?.Invoke();

        // 5. Apagar interactuable
        OnLoseFocus();
        InteractionPromptUI.Instance?.HidePrompt();
        gameObject.SetActive(false);
    }

    private void ShowPickupPopup(int count)
    {
        if (ObjectivePopupUI.Instance == null) return;

        if (!string.IsNullOrEmpty(objectivePopupId) && LocalizationManager.Instance != null)
        {
            string baseFormat = LocalizationManager.Instance.GetText(objectivePopupId);
            string finalMsg = string.Format(baseFormat, count);
            ObjectivePopupUI.Instance.ShowMessage(finalMsg);
        }
        else
        {
            string finalMsg = string.Format(objectivePopupText, count);
            ObjectivePopupUI.Instance.ShowMessage(finalMsg);
        }
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
    }
}