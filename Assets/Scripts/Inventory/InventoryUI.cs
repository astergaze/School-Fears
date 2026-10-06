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
    [SerializeField] private TextMeshProUGUI detailName; // mapear a DetailTitleDescription en el inspector
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private Button useButton;

    private List<GameObject> spawnedSlots = new List<GameObject>();
    private ItemData selectedItem;

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
            Player.UnlockCursor();
            RefreshGrid();
        }
        else
        {
            Time.timeScale = 1f;
            Player.LockCursor();
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
            GameObject slotObj = Instantiate(itemSlotPrefab, itemGrid);
            spawnedSlots.Add(slotObj);

            ItemSlotUI slot = slotObj.GetComponent<ItemSlotUI>();
            if (slot == null)
            {
                Debug.LogError("InventoryUI: el prefab de slot no tiene el componente ItemSlotUI.");
                continue;
            }

            slot.Setup(item, SelectItem);
        }

        ClearDetail();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            RefreshGrid();
        }
    }
    private void SelectItem(ItemData item)
    {
        selectedItem = item;
        if (detailIcon != null)
        {
            detailIcon.sprite = item.icon;
            detailIcon.gameObject.SetActive(item.icon != null);
        }

        // Usamos los textos localizados
        if (detailName != null)
            detailName.text = item.GetLocalizedName();

        if (detailDescription != null)
            detailDescription.text = item.GetLocalizedDescription();

        if (useButton != null)
        {
            useButton.gameObject.SetActive(item.isUsable);
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() => UseItem(item));
        }
    }

    private void UseItem(ItemData item)
    {
        Debug.Log($"Usando: {item.itemName}");
        // Acá se conectará con el sistema de interacción
    }

    private void ClearDetail()
    {
        if (detailName != null) detailName.text = "";
        if (detailDescription != null) detailDescription.text = "";
        if (detailIcon != null) detailIcon.gameObject.SetActive(false);

        useButton?.gameObject.SetActive(false);
        selectedItem = null;
    }
}