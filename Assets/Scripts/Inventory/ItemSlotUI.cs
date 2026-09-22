using UnityEngine;
using UnityEngine.EventSystems; // Necesario para detectar teclado y mouse
using TMPro;

public class ItemSlotUI : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    [Header("Referencias UI")]
    public TMP_Text nombreTexto;

    private ItemData itemData;
    private InventoryUIManager uiManager;

    public void ConfigurarSlot(ItemData item, InventoryUIManager manager)
    {
        itemData = item;
        uiManager = manager;

        if (nombreTexto != null && itemData != null)
        {
            nombreTexto.text = "- " + itemData.itemName.ToUpper();
        }
    }

    // Se activa automáticamente al pasar el mouse por encima
    public void OnPointerEnter(PointerEventData eventData)
    {
        SeleccionarEsteItem();
    }

    // Se activa automáticamente al navegar hacia este objeto con las flechas del teclado
    public void OnSelect(BaseEventData eventData)
    {
        SeleccionarEsteItem();
    }

    // Le avisa al panel derecho que muestre la información
    private void SeleccionarEsteItem()
    {
        if (uiManager != null && itemData != null)
        {
            uiManager.MostrarDetallesItem(itemData);
        }
    }
}