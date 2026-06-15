// Inventario
using UnityEngine;

public class InventoryInteractable : Interactable
{
    [SerializeField] private ItemData item;

    public override void Interact(Player player)
    {
        if (item == null)
        {
            Debug.LogWarning("InventoryInteractable: no ItemData asignado.");
            return;
        }

        player.Inventory.AddItem(item);
        Debug.Log($"Added to inventory: {item.itemName}");
        gameObject.SetActive(false);
    }
}