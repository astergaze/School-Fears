using UnityEngine;


public enum ItemType { Key, Collectible, Consumable, QuestItem } // Cambiar despues

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public ItemType type;
    public bool isUsable;
    public bool isCombineable;
    public ItemData combinesWith;
    public ItemData combineResult;
}
