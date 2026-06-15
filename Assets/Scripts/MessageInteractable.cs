using UnityEngine;
using TMPro;

public class MessageInteractable : Interactable
{
    [Header("Message Settings")]
    [SerializeField] private string message = "...";
    [SerializeField] private float displayDuration = 3f;

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject messagePanel;

    private Coroutine hideCoroutine;

    public override void Interact(Player player)
    {
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        messagePanel.SetActive(true);
        messageText.text = message;

        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private System.Collections.IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        messagePanel.SetActive(false);
    }

    public override void OnFocus()
    {
        // Opcional: highlight del objeto cuando el jugador lo mira
    }

    public override void OnLoseFocus()
    {
        // Opcional: quitar highlight
    }
}