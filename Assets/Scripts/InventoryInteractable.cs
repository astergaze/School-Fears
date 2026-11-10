using UnityEngine;
using UnityEngine.SceneManagement;

public class InventoryInteractable : Interactable
{
    [SerializeField] private ItemData item;

    // ID automático: escena + ítem + posición (sirve mientras el objeto no se mueva de lugar)
    private string PickupFlag
    {
        get
        {
            Vector3 p = transform.position;
            return $"pickup_{SceneManager.GetActiveScene().name}_{(item != null ? item.name : "none")}_{Mathf.RoundToInt(p.x)}_{Mathf.RoundToInt(p.y)}_{Mathf.RoundToInt(p.z)}";
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasFlag(PickupFlag))
            gameObject.SetActive(false);
    }

    public override void Interact(Player player)
    {
        if (item == null)
        {
            Debug.LogWarning("InventoryInteractable: no ItemData asignado.");
            return;
        }

        player.Inventory.AddItem(item);
        GameManager.Instance?.SetFlag(PickupFlag);
        gameObject.SetActive(false);
    }
}