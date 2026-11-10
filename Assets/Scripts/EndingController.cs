using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla la transición al finalizar la secuencia de la Linterna en Scenes/Auladestroyed.
/// Desactiva scripts de input/control del jugador, ejecuta Fade Out y redirige
/// limpiamente a la escena de detención del Capítulo 2 (o la configurada en Inspector).
/// </summary>
public class EndingController : MonoBehaviour
{
    [Header("Configuración de Destino")]
    [Tooltip("Nombre EXACTO o ruta de la escena de destino en Build Settings")]
    [SerializeField] private string targetSceneName = "Scenes/Capitulo2/detencion";
    [SerializeField] private float delayBeforeTransition = 2.5f;

    [Header("Interfaz de Pantalla (Opcional)")]
    [Tooltip("Activar si se desea mostrar un overlay de texto antes de cambiar de escena")]
    [SerializeField] private bool showEndingTextUI = false;
    [SerializeField] private string endingText = "CAPÍTULO 2";

    [Header("Desactivación de Control")]
    [Tooltip("Scripts a desactivar al terminar (ej: el script de movimiento/input del Player)")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnEnding;

    [Header("Persistencia")]
    [Tooltip("Resetear singletons persistentes para garantizar un inicio limpio en el Capítulo 2")]
    [SerializeField] private bool resetSingletons = true;

    private bool triggered = false;

    public void Trigger()
    {
        if (triggered) return;
        triggered = true;

        foreach (var s in scriptsToDisableOnEnding)
        {
            if (s != null) s.enabled = false;
        }

        if (showEndingTextUI)
        {
            ShowEndingUI();
        }

        // Iniciar pantalla a negro usando la firma exacta (1 parámetro: targetAlpha)
        if (SceneTransitioner.Instance != null)
        {
            SceneTransitioner.Instance.FadeScreen(1f);
        }

        Invoke(nameof(GoToNextScene), delayBeforeTransition);
    }

    // Alias en minúscula por compatibilidad con invocaciones desde UnityEvents
    public void trigger() => Trigger();

    private void ShowEndingUI()
    {
        GameObject canvasGO = new GameObject("EndingCanvas_Runtime");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImage = bgGO.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.95f);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject textGO = new GameObject("EndingText");
        textGO.transform.SetParent(canvasGO.transform, false);
        var text = textGO.AddComponent<UnityEngine.UI.Text>();
        text.text = endingText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 64;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void GoToNextScene()
    {
        if (resetSingletons)
        {
            if (Player.Instance != null)
            {
                Destroy(Player.Instance.gameObject);
            }
            else
            {
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null) Destroy(playerGO);
            }

            if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
            if (DialogueManager.Instance != null) Destroy(DialogueManager.Instance.gameObject);
        }

        Time.timeScale = 1f;
        Player.UnlockCursor();

        InteractionPromptUI.Instance?.HidePrompt();
        SceneManager.LoadScene(targetSceneName);
        SceneTransitioner.Instance?.FadeScreen(0f);
    }
}