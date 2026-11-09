using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Da una llave (ItemData) al jugador. Sirve para:
///  - una llave tirada en el mapa (Deactivate After Taking = ON): desaparece y no reaparece si volvés a la escena.
///  - un objeto fijo, como la mesa central del aula (Deactivate After Taking = OFF): se queda en su lugar.
/// Si hay "Found Lines", el diálogo va primero y la llave se entrega cuando termina.
/// A diferencia de NoteItemInteractable, NO suma al conteo de notas de GameManager
/// (usa flags aparte), así que no interfiere con ClassroomExitDoor.
/// Requiere SetFlag/HasFlag en GameManager.
/// </summary>
public class KeyGiverInteractable : Interactable
{
    [Header("Llave")]
    [SerializeField] private ItemData keyItem;
    [Tooltip("ID único del flag en GameManager. Si lo dejás vacío usa 'key_' + nombre del ItemData.")]
    [SerializeField] private string flagId;

    [Header("Comportamiento")]
    [Tooltip("ON: llave tirada en el mapa (desaparece al tomarla). OFF: objeto que se queda (mesa, escritorio).")]
    [SerializeField] private bool deactivateAfterTaking = true;

    [Header("Diálogo")]
    [Tooltip("Se reproduce antes de entregar la llave (ej: '¿Para qué será…?').")]
    [SerializeField] private DialogueLine[] foundLines;
    [Tooltip("Opcional: se reproduce si el jugador ya la tomó (solo útil si el objeto no se desactiva).")]
    [SerializeField] private DialogueLine[] alreadyTakenLines;

    [Header("Popup de objetivo (opcional)")]
    [Tooltip("ID de localización. Tiene que existir en el CSV; si no, se usa el texto directo de abajo.")]
    [SerializeField] private string popupTextId;
    [SerializeField] private string popupText;

    [Header("Sonido (opcional)")]
    [SerializeField] private AudioClip pickupSound;

    public UnityEvent onKeyTaken;

    private Player interactingPlayer;

    private string FlagKey =>
        !string.IsNullOrEmpty(flagId) ? flagId :
        (keyItem != null ? "key_" + keyItem.name : null);

    private void Start()
    {
        // Si ya la tomó y volvió a entrar a la escena, la llave no reaparece
        if (deactivateAfterTaking && GameManager.Instance != null && GameManager.Instance.HasFlag(FlagKey))
            gameObject.SetActive(false);
    }

    public override void Interact(Player player)
    {
        interactingPlayer = player;

        if (IsAlreadyTaken(player))
        {
            if (alreadyTakenLines != null && alreadyTakenLines.Length > 0 && DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(alreadyTakenLines);
            return;
        }

        if (DialogueManager.Instance != null && foundLines != null && foundLines.Length > 0)
        {
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
            DialogueManager.Instance.OnDialogueEnded += HandleDialogueEnded;
            DialogueManager.Instance.StartDialogue(foundLines);
        }
        else
        {
            TakeKey();
        }
    }

    private bool IsAlreadyTaken(Player player)
    {
        if (GameManager.Instance != null && GameManager.Instance.HasFlag(FlagKey)) return true;
        return keyItem != null && player.Inventory != null && player.Inventory.HasItem(keyItem);
    }

    private void HandleDialogueEnded()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;

        TakeKey();
    }

    private void TakeKey()
    {
        if (keyItem == null)
        {
            Debug.LogWarning("KeyGiverInteractable: no hay ItemData asignado.", this);
            return;
        }

        if (interactingPlayer != null && interactingPlayer.Inventory != null)
            interactingPlayer.Inventory.AddItem(keyItem);

        GameManager.Instance?.SetFlag(FlagKey);

        if (pickupSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(pickupSound);

        ShowPopup();
        onKeyTaken?.Invoke();

        if (deactivateAfterTaking)
        {
            OnLoseFocus();
            InteractionPromptUI.Instance?.HidePrompt();
            gameObject.SetActive(false);
        }
    }

    private void ShowPopup()
    {
        if (ObjectivePopupUI.Instance == null) return;

        if (!string.IsNullOrEmpty(popupTextId) && LocalizationManager.Instance != null)
        {
            string loc = LocalizationManager.Instance.GetText(popupTextId);
            if (!loc.StartsWith("["))
            {
                ObjectivePopupUI.Instance.ShowMessage(loc);
                return;
            }
        }

        if (!string.IsNullOrEmpty(popupText))
            ObjectivePopupUI.Instance.ShowMessage(popupText);
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
    }
}