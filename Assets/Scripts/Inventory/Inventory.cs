using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private List<ItemData> items = new List<ItemData>();

    public void AddItem(ItemData item) => items.Add(item);
    public void RemoveItem(ItemData item) => items.Remove(item);

    public bool HasItem(ItemData item) => items.Contains(item);
    public List<ItemData> GetAllItems() => new List<ItemData>(items);

    // Filtrás por tipo cuando lo necesitás, sin listas separadas
    public List<ItemData> GetByType(ItemType type) => 
        items.Where(i => i.type == type).ToList();
}