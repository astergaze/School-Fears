using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject inventoryPanel;

    [Header("Grid")]
    [SerializeField] private Transform itemGrid;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Detail")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private Button useButton;
    [SerializeField] private Button combineButton;

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private ItemData selectedItem;
    private ItemData pendingCombineItem;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        inventoryPanel.SetActive(false);
        ClearDetail();
    }

    public void Toggle()
    {
        bool open = !inventoryPanel.activeSelf;
        inventoryPanel.SetActive(open);

        if (open)
        {
            Time.timeScale = 0f;
            RefreshGrid();
        }
        else
        {
            Time.timeScale = 1f;
            pendingCombineItem = null;
        }
    }

    private void RefreshGrid()
    {
        foreach (var slot in spawnedSlots)
            Destroy(slot);
        spawnedSlots.Clear();

        List<ItemData> items = Player.Instance.Inventory.GetAllItems();

        foreach (ItemData item in items)
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemGrid);
            spawnedSlots.Add(slot);

            Image icon = slot.transform.Find("ItemIcon").GetComponent<Image>();
            TextMeshProUGUI nameText = slot.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
            Button btn = slot.GetComponent<Button>();

            if (item.icon != null)
                icon.sprite = item.icon;

            nameText.text = item.itemName;

            ItemData captured = item;
            btn.onClick.AddListener(() => SelectItem(captured));
        }

        ClearDetail();
    }

    private void SelectItem(ItemData item)
    {
        if (pendingCombineItem != null)
        {
            TryCombine(pendingCombineItem, item);
            return;
        }

        selectedItem = item;
        detailIcon.sprite = item.icon != null ? item.icon : null;
        detailName.text = item.itemName;
        detailDescription.text = item.description;

        useButton.gameObject.SetActive(item.isUsable);
        combineButton.gameObject.SetActive(item.isCombineable);

        useButton.onClick.RemoveAllListeners();
        useButton.onClick.AddListener(() => UseItem(item));

        combineButton.onClick.RemoveAllListeners();
        combineButton.onClick.AddListener(() => StartCombine(item));
    }

    private void UseItem(ItemData item)
    {
        Debug.Log($"Usando: {item.itemName}");
        // Acá se conectará con el sistema de interacción
    }

    private void StartCombine(ItemData item)
    {
        pendingCombineItem = item;
        detailDescription.text = $"Seleccioná un item para combinar con {item.itemName}";
        useButton.gameObject.SetActive(false);
        combineButton.gameObject.SetActive(false);
    }

    private void TryCombine(ItemData a, ItemData b)
    {
        if (a.combinesWith == b && a.combineResult != null)
        {
            Player.Instance.Inventory.RemoveItem(a);
            Player.Instance.Inventory.RemoveItem(b);
            Player.Instance.Inventory.AddItem(a.combineResult);
            Debug.Log($"Combinación exitosa: {a.combineResult.itemName}");
            RefreshGrid();
        }
        else
        {
            detailDescription.text = "Estos items no se pueden combinar.";
        }

        pendingCombineItem = null;
    }

    private void ClearDetail()
    {
        detailName.text = "";
        detailDescription.text = "";
        detailIcon.sprite = null;
        useButton.gameObject.SetActive(false);
        combineButton.gameObject.SetActive(false);
        selectedItem = null;
    }
}