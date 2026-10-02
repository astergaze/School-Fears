using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 1f;
    [Tooltip("ID de localización para el prompt en pantalla")]
    public string promptKey = "promptInteractDefault";
    private OutlineEffect outline;

    protected virtual void Awake()
    {
        outline = GetComponent<OutlineEffect>();
    }

    public abstract void Interact(Player player);

    public virtual void OnFocus()
    {
        outline?.SetOutlineVisible(true);
    }

    public virtual void OnLoseFocus()
    {
        outline?.SetOutlineVisible(false);
    }

    public virtual string GetPromptText()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(promptKey))
        {
            return LocalizationManager.Instance.GetText(promptKey);
        }
        return "Presiona E para interactuar";
    }
}