using UnityEngine;

public enum ItemType { Key, Collectible, Consumable, QuestItem }

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Localization Keys (CSV)")]
    [Tooltip("ID en el CSV (ej: 'item_flashlight_name'). Si está vacío, usa itemName como fallback.")]
    public string nameKey;
    [Tooltip("ID en el CSV (ej: 'item_flashlight_desc'). Si está vacío, usa description como fallback.")]
    public string descriptionKey;

    [Header("Fallback / Raw Text")]
    public string itemName;
    [TextArea(2, 4)]
    public string description;

    [Header("Settings")]
    public Sprite icon;
    public ItemType type;
    public bool isUsable;
    public bool isCombineable;
    public ItemData combinesWith;
    public ItemData combineResult;

    /// <summary>
    /// Devuelve el nombre traducido según el idioma activo en LocalizationManager,
    /// o cae a itemName si no hay key asignada o no existe el manager.
    /// </summary>
    public string GetLocalizedName()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(nameKey))
        {
            string loc = LocalizationManager.Instance.GetText(nameKey);
            if (!string.IsNullOrEmpty(loc)) return loc;
        }
        return itemName;
    }

    /// <summary>
    /// Devuelve la descripción traducida según el idioma activo en LocalizationManager,
    /// o cae a description si no hay key asignada o no existe el manager.
    /// </summary>
    public string GetLocalizedDescription()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(descriptionKey))
        {
            string loc = LocalizationManager.Instance.GetText(descriptionKey);
            if (!string.IsNullOrEmpty(loc)) return loc;
        }
        return description;
    }
}