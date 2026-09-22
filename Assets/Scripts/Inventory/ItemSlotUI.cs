using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Un slot individual de la grilla de inventario. Solo texto por ahora — el
/// diseño actual no muestra ícono en cada fila de la lista (el ícono grande
/// va aparte, en el panel de detalle a la derecha). Se puede seleccionar con
/// click, con el mouse pasando por encima, o navegando con teclado/gamepad.
/// </summary>
public class ItemSlotUI : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    [Header("Referencias UI")]
    [SerializeField] private TMP_Text nameText;

    private ItemData itemData;
    private System.Action<ItemData> onSelected;

    public void Setup(ItemData item, System.Action<ItemData> callback)
    {
        itemData = item;
        onSelected = callback;

        if (nameText != null)
            nameText.text = item.itemName;
    }

    // Pasar el mouse por encima selecciona (preview rápido, sin necesidad de click)
    public void OnPointerEnter(PointerEventData eventData) => Select();

    // Navegar con teclado/gamepad hasta este slot también selecciona
    public void OnSelect(BaseEventData eventData) => Select();

    private void Select() => onSelected?.Invoke(itemData);
}