using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Da una llave (ItemData) al jugador.
/// Verifica la presencia real en el inventario antes de denegar la entrega,
/// permitiendo reobtenerla si fue eliminada (ej. purga de detención).
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
    [Tooltip("Opcional: se reproduce si el jugador ya la tomó y aún la conserva en el inventario.")]
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
        // Solo se desactiva al iniciar si el objeto debe desaparecer Y el jugador realmente posee el ítem en el inventario
        if (deactivateAfterTaking && Player.Instance != null && HasItemInInventory(Player.Instance))
        {
            gameObject.SetActive(false);
        }
    }

    public override void Interact(Player player)
    {
        interactingPlayer = player;

        // Si el jugador ya posee físicamente el ítem en el inventario, reproduce el diálogo de 'ya obtenido'
        if (HasItemInInventory(player))
        {
            if (alreadyTakenLines != null && alreadyTakenLines.Length > 0 && DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(alreadyTakenLines);
            return;
        }

        // Si NO lo tiene (sea la primera vez o porque fue removido en la detención), inicia el flujo de entrega
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

    /// <summary>
    /// Comprueba si el jugador conserva actualmente el ItemData en su inventario.
    /// </summary>
    public bool HasItemInInventory(Player player)
    {
        if (keyItem == null || player == null || player.Inventory == null) return false;
        return player.Inventory.HasItem(keyItem);
    }

    private void HandleDialogueEnded()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;

        TakeKey();
    }

    /// <summary>
    /// Otorga el ítem al inventario del jugador y ejecuta las respuestas visuales/sonoras.
    /// </summary>
    private void TakeKey()
    {
        if (keyItem == null)
        {
            Debug.LogWarning("KeyGiverInteractable: no hay ItemData asignado.", this);
            return;
        }

        // Otorga el ítem si el inventario no lo registra
        if (interactingPlayer != null && interactingPlayer.Inventory != null)
        {
            if (!interactingPlayer.Inventory.HasItem(keyItem))
            {
                interactingPlayer.Inventory.AddItem(keyItem);
                Debug.Log($"[KeyGiverInteractable] Ítem '{keyItem.name}' entregado al jugador.");
            }
        }

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