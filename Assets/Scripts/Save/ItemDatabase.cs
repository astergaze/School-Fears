using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lista de TODOS los ItemData del juego, para poder reconstruir el inventario desde un guardado
/// (los guardados solo llevan el nombre del asset).
/// Crear con: Create > Inventory > Item Database, y guardarlo EXACTAMENTE en
/// Assets/Resources/ItemDatabase.asset. Arrastrar ahí todos los ItemData (incluidos los
/// "combineResult").
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> items = new List<ItemData>();

    private static ItemDatabase instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (instance == null) instance = Resources.Load<ItemDatabase>("ItemDatabase");
            return instance;
        }
    }

    private Dictionary<string, ItemData> map;

    public ItemData Find(string assetName)
    {
        if (map == null)
        {
            map = new Dictionary<string, ItemData>();
            foreach (var it in items)
                if (it != null) map[it.name] = it;
        }
        return map.TryGetValue(assetName, out var found) ? found : null;
    }

    private void OnValidate() { map = null; }
}
