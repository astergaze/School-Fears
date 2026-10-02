using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pantalla de "Gracias por jugar" al terminar el juego: desactiva los scripts que
/// le pases (ej: movimiento del Player), muestra un texto simple en pantalla (creado
/// 100% en runtime) y vuelve al menú después de unos segundos, reseteando los
/// singletons persistentes -- mismo patrón que GameOverController, para que volver
/// al menú desde el final sea equivalente a reiniciar el juego.
///
/// Enganchar MessageInteractable.onMessageEnded (en el mensaje final, ej. la Linterna)
/// -> EndingController.Trigger() en el Inspector.
/// </summary>
public class EndingController : MonoBehaviour
{
    [Tooltip("Scripts a desactivar al terminar (ej: el script de movimiento/input del Player)")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnEnding;
    [SerializeField] private float delayBeforeMenu = 4f;
    [SerializeField] private string endingText = "GRACIAS POR JUGAR";
    [Tooltip("Nombre EXACTO de la escena de menú (tal cual aparece en Build Settings)")]
    [SerializeField] private string menuSceneName = "menu";

    private bool triggered = false;

    public void Trigger()
    {
        if (triggered) return;
        triggered = true;

        foreach (var s in scriptsToDisableOnEnding)
        {
            if (s != null) s.enabled = false;
        }

        ShowEndingUI();
        Invoke(nameof(GoToMenu), delayBeforeMenu);
    }

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

    private void GoToMenu()
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

        Time.timeScale = 1f;
        Player.UnlockCursor();

        // InteractionPromptUI y SceneTransitioner son singletons persistentes: si no se
        // limpian acá, lo que haya quedado en pantalla (el "Presiona E" o el fade a negro
        // de HandleDialogueEnded) se arrastra tal cual al menú.
        InteractionPromptUI.Instance?.HidePrompt();
        SceneManager.LoadScene(menuSceneName);
        SceneTransitioner.Instance?.FadeScreen(0f);
    }
}