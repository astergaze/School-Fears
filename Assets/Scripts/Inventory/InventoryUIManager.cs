using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUIManager : MonoBehaviour
{
    [Header("Base de Datos")]
    public Inventory inventarioDelJugador; // Arrastra aquí tu Player (Inventory)

    [Header("Grid")]
    public Transform ItemGrid;          // Arrastra tu ItemGrid
    public GameObject ItemSlot;         // Arrastra el prefab ItemSlot desde Project

    [Header("Detail")]
    public Image DetailIcon;            // Imagen grande de arriba a la derecha
    public TMP_Text DetailName;         // Título del ítem seleccionado
    public TMP_Text DetailDescription;  // Descripción detallada de abajo
    public Button UseButton;            // Botón Seleccionar

    private ItemData itemSeleccionado;

    void Start()
    {
        if (inventarioDelJugador != null)
        {
            // Suscribirse al evento de actualización si tu inventario lo usa, 
            // o puedes llamar a ActualizarInventario() manualmente cuando agarres un ítem.
        }
    }

    // Llama a esta función cada vez que se agregue un ítem o se abra el inventario
    public void ActualizarInventario(List<ItemData> listaDeItems)
    {
        // 1. Limpiar slots anteriores para evitar duplicados
        foreach (Transform hijo in ItemGrid)
        {
            Destroy(hijo.gameObject);
        }

        // 2. Crear un slot por cada ítem en el inventario
        foreach (var item in listaDeItems)
        {
            GameObject nuevoSlot = Instantiate(ItemSlot, ItemGrid);

            // Configurar el texto y evento del slot
            ItemSlotUI slotUI = nuevoSlot.GetComponent<ItemSlotUI>();
            if (slotUI != null)
            {
                slotUI.ConfigurarSlot(item, this);
            }
        }
    }

    // Esta función se ejecuta al hacer clic en un ítem de la lista izquierda
    public void MostrarDetallesItem(ItemData item)
    {
        itemSeleccionado = item;

        if (item != null)
        {
            // Título del ítem en el panel de la derecha
            if (DetailName != null)
                DetailName.text = item.itemName;

            // Descripción detallada abajo del título
            if (DetailDescription != null)
                DetailDescription.text = item.description;

            // Imagen arriba a la derecha
            if (DetailIcon != null)
            {
                if (item.icon != null)
                {
                    DetailIcon.gameObject.SetActive(true);
                    DetailIcon.sprite = item.icon;
                }
                else
                {
                    DetailIcon.gameObject.SetActive(false); // Oculta si no tiene sprite
                }
            }
        }
    }

    // Acción del Botón "Seleccionar"
    public void BotonSeleccionarPresionado()
    {
        if (itemSeleccionado != null)
        {
            Debug.Log("Item seleccionado para usar: " + itemSeleccionado.itemName);
            // Aquí puedes poner la lógica si el ítem es consumable o utilizable
        }
    }
}