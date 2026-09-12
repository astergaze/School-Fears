using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 1f;
    public string interactionPrompt = "Interact";

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
}