using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger para el final malo (Bad Ending) en el Buffet.
/// 1. Mantiene/ejecuta un Fade to Black.
/// 2. Despliega la secuencia de diálogos.
/// 3. Al concluir el diálogo, registra el flag, sincroniza la partida con el servidor (SaveManager),
///    muestra la pantalla de Bad Ending y regresa al menú principal.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BadEndingTrigger : MonoBehaviour
{
    [Header("Diálogos")]
    [SerializeField] private DialogueLine[] dialogueLines;

    [Header("Configuración del Bad Ending")]
    [Tooltip("Flag que se registrará en el GameManager y se enviará al servidor")]
    [SerializeField] private string badEndingFlag = "bad_ending_obtained";
    [SerializeField] private string badEndingText = "BAD ENDING";
    [SerializeField] private float delayBeforeMenu = 5f;
    [SerializeField] private string menuSceneName = "menu";

    [Header("Sonido")]
    [SerializeField] private AudioClip endingSound;

    private bool triggered = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        StartCoroutine(ExecuteSequence());
    }

    private IEnumerator ExecuteSequence()
    {
        // 1. Bloquear entradas e iniciar Fade to Black
        SceneTransitioner.IsTransitioning = true;
        InteractionPromptUI.Instance?.HidePrompt();

        if (SceneTransitioner.Instance != null)
        {
            yield return SceneTransitioner.Instance.FadeRoutine(1f);
        }

        if (endingSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(endingSound);
        }

        // 2. Iniciar Diálogos (mantenimiento de pantalla en negro)
        if (dialogueLines != null && dialogueLines.Length > 0 && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueFinished;
            DialogueManager.Instance.OnDialogueEnded += OnDialogueFinished;
            DialogueManager.Instance.StartDialogue(dialogueLines);
        }
        else
        {
            OnDialogueFinished();
        }
    }

    private void OnDialogueFinished()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueFinished;
        }

        // 3. Registrar el estado en GameManager y persistir al Servidor
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag(badEndingFlag);
        }

        if (SaveManager.Instance != null)
        {
            // Guarda localmente y sube automáticamente vía HTTP (CloudApi)
            SaveManager.Instance.SaveNow(upload: true);
        }

        // 4. Mostrar interfaz de Bad Ending y programar retorno al menú
        ShowBadEndingUI();
        Invoke(nameof(ReturnToMenu), delayBeforeMenu);
    }

    private void ShowBadEndingUI()
    {
        GameObject canvasGO = new GameObject("BadEndingCanvas_Runtime");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Fondo negro total
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImage = bgGO.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = Color.black;
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Texto de Bad Ending
        GameObject textGO = new GameObject("BadEndingText");
        textGO.transform.SetParent(canvasGO.transform, false);
        var text = textGO.AddComponent<UnityEngine.UI.Text>();
        text.text = badEndingText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 54;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.8f, 0.1f, 0.1f, 1f); // Rojo carmesí

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void ReturnToMenu()
    {
        // Limpieza de singletons persistentes
        if (Player.Instance != null) Destroy(Player.Instance.gameObject);
        if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        if (DialogueManager.Instance != null) Destroy(DialogueManager.Instance.gameObject);

        Time.timeScale = 1f;
        Player.UnlockCursor();
        SceneTransitioner.IsTransitioning = false;

        InteractionPromptUI.Instance?.HidePrompt();
        SceneManager.LoadScene(menuSceneName);
        SceneTransitioner.Instance?.FadeScreen(0f);
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueFinished;
        }
    }
}