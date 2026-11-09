using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Permite obtener un objeto (rewardItem) solo si el jugador posee otro objeto previo en su inventario (requiredItem).
/// Ideal para cajones, escritorios o candados (ej: usar 'drawerkey' para obtener 'aula1key').
/// Integra persistencia de estado mediante banderas en GameManager, soporte de diálogos (bloqueado/éxito) y SFX.
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
        // Si ya fue reclamado previamente y el objeto debía desactivarse, se apaga al cargar la escena
        if (deactivateAfterTaking && GameManager.Instance != null && GameManager.Instance.HasFlag(FlagKey))
        {
            gameObject.SetActive(false);
        }
    }

    public override void Interact(Player player)
    {
        interactingPlayer = player;

        // 1. Verificar si ya se obtuvo la recompensa anteriormente
        if (IsAlreadyUnlocked())
        {
            if (alreadyTakenLines != null && alreadyTakenLines.Length > 0 && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(alreadyTakenLines);
            }
            return;
        }

        // 2. Validar si el jugador posee el ítem requerido en el inventario
        bool hasRequired = requiredItem == null || (player.Inventory != null && player.Inventory.HasItem(requiredItem));

        if (!hasRequired)
        {
            // Fallback: No tiene la llave/herramienta requerida
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

        // 3. Posee el ítem: Disparar diálogo de éxito o entregar recompensa directamente
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

    private bool IsAlreadyUnlocked()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasFlag(FlagKey)) return true;
        return rewardItem != null && interactingPlayer != null && interactingPlayer.Inventory != null && interactingPlayer.Inventory.HasItem(rewardItem);
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

        // Consumir el ítem requerido si así fue configurado
        if (consumeRequiredItem && requiredItem != null)
        {
            interactingPlayer.Inventory.RemoveItem(requiredItem);
        }

        // Entregar recompensa
        if (rewardItem != null)
        {
            interactingPlayer.Inventory.AddItem(rewardItem);
        }

        // Registrar en el GameManager la bandera de progreso
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