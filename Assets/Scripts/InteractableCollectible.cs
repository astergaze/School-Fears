// Coleccionable (peluche → skin)
using UnityEngine;

public class InteractableCollectible : Interactable
{
    [SerializeField] private string collectibleId;
    [SerializeField] private string skinUnlocked;

    public override void Interact(Player player)
    {
        Debug.Log($"Collected: {collectibleId} — Unlocked skin: {skinUnlocked}");
        // Lógica para agregar al save / desbloquear skin
        gameObject.SetActive(false);
    }

    public override void OnFocus()
    {
        // Mostrar outline o prompt "Pick up"
    }
}