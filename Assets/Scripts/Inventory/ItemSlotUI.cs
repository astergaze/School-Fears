using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlotUI : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    [Header("Referencias UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    private ItemData itemData;
    private System.Action<ItemData> onSelected;

    public void Setup(ItemData item, System.Action<ItemData> callback)
    {
        itemData = item;
        onSelected = callback;

        if (descriptionText != null && item != null)
            descriptionText.text = item.GetLocalizedDescription();

        if (nameText != null && item != null)
            nameText.text = item.GetLocalizedName();
    }

    public void OnPointerEnter(PointerEventData eventData) => Select();
    public void OnSelect(BaseEventData eventData) => Select();
    private void Select() => onSelected?.Invoke(itemData);
}