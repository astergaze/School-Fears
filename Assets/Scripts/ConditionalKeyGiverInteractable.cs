using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Permite obtener un objeto (rewardItem) solo si el jugador posee otro objeto previo en su inventario (requiredItem).
/// Revisa la presencia real del ítem en el inventario para permitir reobtenerlo si fue removido (ej: detención),
/// manteniendo el cerrojo abierto si el flag de GameManager ya había sido registrado.
/// </summary>
public class ConditionalKeyGiverInteractable : Interactable
{
    [Header("Prerrequisito (Llave / Ítem requerido)")]
    [Tooltip("El Ítem (.asset) necesario en el inventario para poder abrir/desbloquear esto (ej: drawerkey).")]
    [SerializeField] private ItemData requiredItem;
    [Tooltip("Si está activo, consume el ítem requerido al usarse con éxito.")]
    [SerializeField] private bool consumeRequiredItem = false;
    [Tooltip("Diálogo cuando el jugador interactúa SIN el ítem requerido en el inventario.")]
    [SerializeField] private DialogueLine[] missingItemLines;
    [SerializeField] private AudioClip lockedSound;

    [Header("Recompensa (Ítem a entregar)")]
    [Tooltip("El Ítem (.asset) que se entregará al jugador (ej: aula1key).")]
    [SerializeField] private ItemData rewardItem;
    [Tooltip("ID único para recordar en GameManager si ya se obtuvo este ítem.")]
    [SerializeField] private string flagId;
    [Tooltip("ON: El objeto desaparece al tomar el ítem. OFF: Permanece en escena (ej: cajón/escritorio).")]
    [SerializeField] private bool deactivateAfterTaking = false;

    [Header("Diálogos de Éxito")]
    [Tooltip("Diálogo que se reproduce al abrir/obtener el ítem (ej: '¡La llave del Aula 1 estaba adentro!').")]
    [SerializeField] private DialogueLine[] successLines;
    [Tooltip("Diálogo opcional si el objeto permanece y el jugador interactúa nuevamente habiendo tomado el ítem.")]
    [SerializeField] private DialogueLine[] alreadyTakenLines;

    [Header("Feedback Visual & Sonoro")]
    [SerializeField] private string popupTextId;
    [SerializeField] private string popupText = "Obtuviste la Llave del Aula 1";
    [SerializeField] private AudioClip unlockSound;
    [SerializeField] private AudioClip pickupSound;

    public UnityEvent onItemUnlocked;

    private Player interactingPlayer;

    private string FlagKey =>
        !string.IsNullOrEmpty(flagId) ? flagId :
        (rewardItem != null ? "unlocked_" + rewardItem.name : null);

    private void Start()
    {
        // Solo se oculta al cargar si debía desactivarse Y el jugador conserva físicamente la recompensa
        if (deactivateAfterTaking && Player.Instance != null && HasRewardInInventory(Player.Instance))
        {
            gameObject.SetActive(false);
        }
    }

    public override void Interact(Player player)
    {
        interactingPlayer = player;

        // 1. Verificar si el jugador YA POSEE la recompensa en su inventario
        if (HasRewardInInventory(player))
        {
            if (alreadyTakenLines != null && alreadyTakenLines.Length > 0 && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(alreadyTakenLines);
            }
            return;
        }

        // 2. Validar acceso: si ya fue desbloqueado previamente (flag en GameManager), no exige la llave requerida de nuevo
        bool isAlreadyUnlockedByFlag = GameManager.Instance != null && GameManager.Instance.HasFlag(FlagKey);
        bool hasRequired = isAlreadyUnlockedByFlag || requiredItem == null || (player.Inventory != null && player.Inventory.HasItem(requiredItem));

        if (!hasRequired)
        {
            // Bloqueado: No tiene el ítem requerido ni el lugar había sido desbloqueado antes
            if (lockedSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(lockedSound);
            }

            if (missingItemLines != null && missingItemLines.Length > 0 && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(missingItemLines);
            }
            return;
        }

        // 3. Posee el ítem requerido (o el lugar ya estaba abierto): reproducir éxito y reentregar recompensa
        if (unlockSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(unlockSound);
        }

        if (DialogueManager.Instance != null && successLines != null && successLines.Length > 0)
        {
            DialogueManager.Instance.OnDialogueEnded -= HandleSuccessDialogueEnded;
            DialogueManager.Instance.OnDialogueEnded += HandleSuccessDialogueEnded;
            DialogueManager.Instance.StartDialogue(successLines);
        }
        else
        {
            GrantReward();
        }
    }

    /// <summary>
    /// Comprueba si el jugador posee actualmente la recompensa (rewardItem) en el inventario.
    /// </summary>
    public bool HasRewardInInventory(Player player)
    {
        if (rewardItem == null || player == null || player.Inventory == null) return false;
        return player.Inventory.HasItem(rewardItem);
    }

    private void HandleSuccessDialogueEnded()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= HandleSuccessDialogueEnded;
        }

        GrantReward();
    }

    private void GrantReward()
    {
        if (interactingPlayer == null || interactingPlayer.Inventory == null) return;

        // Consumir el ítem requerido únicamente si sigue en el inventario y no se había consumido antes
        if (consumeRequiredItem && requiredItem != null && interactingPlayer.Inventory.HasItem(requiredItem))
        {
            interactingPlayer.Inventory.RemoveItem(requiredItem);
        }

        // Entregar la recompensa si no la posee
        if (rewardItem != null && !interactingPlayer.Inventory.HasItem(rewardItem))
        {
            interactingPlayer.Inventory.AddItem(rewardItem);
            Debug.Log($"[ConditionalKeyGiver] Recompensa '{rewardItem.name}' entregada al jugador.");
        }

        // Registrar/mantener en GameManager la bandera de desbloqueo
        GameManager.Instance?.SetFlag(FlagKey);

        // Sonido de obtención
        if (pickupSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(pickupSound);
        }

        ShowPopup();
        onItemUnlocked?.Invoke();

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
        {
            ObjectivePopupUI.Instance.ShowMessage(popupText);
        }
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= HandleSuccessDialogueEnded;
        }
    }
}