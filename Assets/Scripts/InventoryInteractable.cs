// Inventario
using UnityEngine;

public class InventoryInteractable : Interactable
{
    [SerializeField] private string itemId;
    [SerializeField] private Sprite itemIcon;

    public override void Interact(Player player)
    {
        Debug.Log($"Added to inventory: {itemId}");
        // player.inventory.Add(itemId, itemIcon);
        gameObject.SetActive(false);
    }
}