using System.Collections.Generic;
using UnityEngine;

public class DetentionSceneController : MonoBehaviour
{
    [Header("Configuración de Inventario")]
    [Tooltip("Lista de ScriptableObjects (ItemData) que el jugador CONSERVARÁ en la detención.")]
    [SerializeField] private List<ItemData> itemsToKeep = new List<ItemData>();

    [Header("Limpieza de SpawnPoint")]
    [Tooltip("Borra el ID del punto de reaparición anterior para no aparecer en la puerta del aula.")]
    [SerializeField] private bool clearPreviousSpawnPoint = true;

    private void Start()
    {
        if (clearPreviousSpawnPoint)
        {
            ResetPreviousSpawnPoint();
        }

        FilterInventory();
    }

    private void ResetPreviousSpawnPoint()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.pendingSpawnPointId = null;
            Debug.Log("[DetentionScene] pendingSpawnPointId reseteado a null.");
        }
    }

    private void FilterInventory()
    {
        // 1. Obtener la referencia real del inventario del Player
        Inventory inventory = null;
        if (Player.Instance != null && Player.Instance.Inventory != null)
        {
            inventory = Player.Instance.Inventory;
        }
        else
        {
            inventory = FindFirstObjectByType<Inventory>();
        }

        if (inventory == null)
        {
            Debug.LogWarning("[DetentionScene] No se encontró el componente Inventory en Player.Instance ni en la escena.");
            return;
        }

        // 2. Obtener la lista actual de ítems (GetAllItems crea una copia segura para iterar)
        List<ItemData> currentItems = inventory.GetAllItems();
        int removedCount = 0;

        foreach (ItemData item in currentItems)
        {
            if (item == null) continue;

            // 3. Verificación multicriterio (Nombre del Asset de Unity, Referencia, nameKey o itemName)
            bool shouldKeep = itemsToKeep.Exists(keep =>
                keep != null && (
                    keep == item ||
                    keep.name == item.name ||
                    (!string.IsNullOrEmpty(keep.nameKey) && keep.nameKey == item.nameKey) ||
                    (!string.IsNullOrEmpty(keep.itemName) && keep.itemName == item.itemName)
                )
            );

            if (!shouldKeep)
            {
                inventory.RemoveItem(item);
                removedCount++;
                Debug.Log($"[DetentionScene] Ítem removido: {item.name} (Key: {item.nameKey})");
            }
        }

        Debug.Log($"[DetentionScene] Purga de inventario completada. Total eliminados: {removedCount}");
    }
}