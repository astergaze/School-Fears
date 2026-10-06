using UnityEngine;

/// <summary>
/// Se coloca en un GameObject con Collider (se fuerza a Trigger en Awake).
/// Al entrar el Player, dispara un popup de objetivo/misión en pantalla sin usar diálogos.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TriggerObjectivePopup : MonoBehaviour
{
    [Header("Contenido")]
    [Tooltip("ID de localización en tu CSV (ej: 'obj_find_key'). Si se define, tiene prioridad sobre Direct Text.")]
    [SerializeField] private string textId;

    [Tooltip("Texto directo en caso de no querer usar el LocalizationManager.")]
    [TextArea(2, 4)]
    [SerializeField] private string directText;

    [Header("Timings (Opcional - Dejar en 0 o negativo para usar los default del UI)")]
    [Tooltip("Tiempo que se mantiene visible")]
    [SerializeField] private float holdTime = 3.5f;

    [Tooltip("Duración de la transición de entrada")]
    [SerializeField] private float fadeInTime = 0.4f;

    [Tooltip("Duración de la transición de salida")]
    [SerializeField] private float fadeOutTime = 0.6f;

    [Header("Trigger")]
    [Tooltip("Si está activo, solo se dispara una vez en la partida/sesión.")]
    [SerializeField] private bool oneShot = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        // Aseguramos que sea trigger automáticamente
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && oneShot) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        float? hold = holdTime > 0f ? holdTime : (float?)null;
        float? fadeIn = fadeInTime > 0f ? fadeInTime : (float?)null;
        float? fadeOut = fadeOutTime > 0f ? fadeOutTime : (float?)null;

        // Prioridad: LocalizationManager vía textId -> Direct Text
        if (!string.IsNullOrEmpty(textId))
        {
            ObjectivePopupUI.Instance.ShowMessageById(textId, hold, fadeIn, fadeOut);
        }
        else if (!string.IsNullOrEmpty(directText))
        {
            ObjectivePopupUI.Instance.ShowMessage(directText, hold, fadeIn, fadeOut);
        }
    }

    /// <summary>
    /// Útil si necesitás reactivar este trigger desde otro script o evento.
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}