using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Trigger para el final malo (Bad Ending) en el Buffet.
/// 1. Mantiene/ejecuta un Fade to Black.
/// 2. Despliega la secuencia de diálogos.
/// 3. Al concluir el diálogo, registra el flag, sincroniza la partida con el servidor (SaveManager)
///    y muestra la pantalla de Bad Ending.
/// 4. Después muestra los créditos con scroll lento de abajo hacia arriba (se puede saltear con E).
/// 5. Regresa al menú principal.
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
    [Tooltip("Segundos que se ve la pantalla de BAD ENDING antes de pasar a los créditos")]
    [SerializeField] private float delayBeforeMenu = 5f;
    [SerializeField] private string menuSceneName = "menu";

    [Header("Créditos")]
    [Tooltip("Velocidad del scroll en unidades de UI por segundo (canvas de referencia 1920x1080). Más bajo = más lento")]
    [SerializeField] private float scrollSpeed = 80f;
    [Tooltip("Segundos de espera con la pantalla vacía cuando termina el scroll, antes de ir al menú")]
    [SerializeField] private float endHold = 1.5f;
    [Tooltip("Duración del fade in de los créditos")]
    [SerializeField] private float creditsFadeIn = 1.5f;
    [Tooltip("Tecla para saltear los créditos")]
    [SerializeField] private Key skipKey = Key.E;
    [SerializeField] private string skipHintText = "Saltar [E]";

    [Header("Sonido")]
    [SerializeField] private AudioClip endingSound;

    private bool triggered = false;
    private GameObject badEndingCanvas;

    // Rol, nombres
    private static readonly string[,] Credits = new string[,]
    {
        { "Programación",          "Thiago Martinez\nBrian Villca\nUlises Alvelo" },
        { "Modelado 3D",           "Benicio Vargas" },
        { "Efectos de sonido",     "Thiago Martinez" },
        { "Música",                "Thiago Martinez" },
        { "Diseño de escenarios",  "Barbara Gamarra\nBrian Villca\nUlises Alvelo" },
        { "Texturizado",           "Ulises Alvelo" },
        { "Diseño de página web",  "Barbara Gamarra" },
        { "Historia",              "Ulises Alvelo\nBarbara Gamarra\nThiago Martinez\nBenicio Vargas\nBrian Villca" },
        { "Documentación",         "Barbara Gamarra\nBrian Villca\nBenicio Vargas\nUlises Alvelo\nThiago Martinez" },
        { "Agradecimientos",       "Hideo Kojima\nPablo Fiscella\nGolden Pablo Fiscella\nScott Cawthon\nHayao Miyazaki\nAkira Toriyama\nHirohiko Araki\nEiichiro Oda\nYoko Taro\nJonathan Joestar\nJotaro Kujo\nJosuke Higashikata\nDoraemon\nGiorno Giovanna\nJolyne Kujo\nJohnny Joestar\nKeanu Reeves\nKoichi Hirose\nJohnny Silverhand\nDon dimadon dueño del domodin de dimmsdale\nUranai Baba\nLucy Kushinada\nDavid Martinez\nKiwi\nMaine\nRebecca\nAdam Smasher\nWeak Kingsley\nD (David)\nTalia Yang\nRoman Carax\nCapricorn\nOkabe Rintaro\nMayuri Shiina\nPower Ranger Rojo\nPower Ranger Azul\nShaco\nDarius\nAkshan\nNeeko\nEkko\nYasuo\nYone\nClove Valorant\nNeon Valorant\nChamber Valorant\nChrom\nDelsin Rowe\nGarrus\nWaluigi\nKirby\nKrystal\nLucario\nLink\nZelda\nRed Hot Chili Peppers\nEminem\nRainbow Dash\nPinkie Pie\nRem Deadlock\nDoorman Deadlock\nMina Ha\nIvy Deadlock\nHeather Manson\nJacket\nQueen\nBohemian Rapsody\nKai'ana\nNala\nHornet\nPinkie\nHelga"}
    };

    private struct CreditsView
    {
        public CanvasGroup group;
        public RectTransform viewport;
        public RectTransform content;
    }

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

        // 4. Bad Ending -> Créditos -> Menú
        StartCoroutine(EndingRoutine());
    }

    private bool SkipPressed()
    {
        return Keyboard.current != null && Keyboard.current[skipKey].wasPressedThisFrame;
    }

    private IEnumerator EndingRoutine()
    {
        badEndingCanvas = ShowBadEndingUI();
        yield return new WaitForSecondsRealtime(delayBeforeMenu);

        if (badEndingCanvas != null) Destroy(badEndingCanvas);

        CreditsView view = BuildCreditsUI();

        // Esperamos un frame para que el ContentSizeFitter calcule la altura real del texto
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(view.content);

        float viewportH = view.viewport.rect.height;
        float contentH = view.content.rect.height;

        // y = 0 -> el borde superior del texto está en el borde superior de la pantalla.
        // Arranca justo debajo de la pantalla y termina cuando el final del texto salió por arriba.
        float y = -viewportH;
        float endY = contentH;
        float fadeT = 0f;
        bool skipped = false;

        view.content.anchoredPosition = new Vector2(0f, y);

        while (y < endY)
        {
            if (SkipPressed()) { skipped = true; break; }

            float dt = Time.unscaledDeltaTime;
            fadeT += dt;
            view.group.alpha = creditsFadeIn > 0f ? Mathf.Clamp01(fadeT / creditsFadeIn) : 1f;

            y += scrollSpeed * dt;
            view.content.anchoredPosition = new Vector2(0f, y);
            yield return null;
        }

        if (!skipped)
        {
            float t = 0f;
            while (t < endHold)
            {
                if (SkipPressed()) break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        ReturnToMenu();
    }

    private GameObject ShowBadEndingUI()
    {
        GameObject canvasGO = new GameObject("BadEndingCanvas_Runtime");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Fondo negro total
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = Color.black;
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Texto de Bad Ending
        GameObject textGO = new GameObject("BadEndingText");
        textGO.transform.SetParent(canvasGO.transform, false);
        var text = textGO.AddComponent<Text>();
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

        return canvasGO;
    }

    /// <summary>
    /// Crea la pantalla de créditos: fondo negro, un viewport con máscara y un único Text
    /// (título + todas las secciones) que se desplaza hacia arriba. Abajo a la derecha
    /// queda fijo el aviso para saltear.
    /// </summary>
    private CreditsView BuildCreditsUI()
    {
        GameObject canvasGO = new GameObject("CreditsCanvas_Runtime");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1001;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        var group = canvasGO.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Fondo negro
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = Color.black;
        Stretch(bgGO.GetComponent<RectTransform>());

        // Viewport con máscara (el texto se recorta en los bordes de la pantalla)
        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform));
        viewportGO.transform.SetParent(canvasGO.transform, false);
        viewportGO.AddComponent<RectMask2D>();
        var viewportRect = viewportGO.GetComponent<RectTransform>();
        Stretch(viewportRect);

        // Texto del scroll: título + secciones en un solo Text con rich text
        var sb = new System.Text.StringBuilder();
        sb.Append("<size=64><b><color=#2c00da>CRÉDITOS</color></b></size>\n\n\n");
        int count = Credits.GetLength(0);
        for (int i = 0; i < count; i++)
        {
            sb.Append("<color=#2c00da><b>").Append(Credits[i, 0]).Append("</b></color>\n");
            sb.Append(Credits[i, 1]);
            if (i < count - 1) sb.Append("\n\n\n");
        }

        GameObject contentGO = new GameObject("CreditsContent");
        contentGO.transform.SetParent(viewportGO.transform, false);
        var body = contentGO.AddComponent<Text>();
        body.text = sb.ToString();
        body.font = font;
        body.fontSize = 32;
        body.supportRichText = true;
        body.alignment = TextAnchor.UpperCenter;
        body.color = Color.white;
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.lineSpacing = 1.1f;
        body.raycastTarget = false;

        var fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.1f, 1f);
        contentRect.anchorMax = new Vector2(0.9f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(0f, 0f);
        contentRect.offsetMax = new Vector2(0f, 0f);
        contentRect.anchoredPosition = new Vector2(0f, -2000f); // fuera de pantalla hasta el primer frame

        // Aviso para saltear (fijo, abajo a la derecha, fuera del viewport)
        GameObject hintGO = new GameObject("SkipHint");
        hintGO.transform.SetParent(canvasGO.transform, false);
        var hint = hintGO.AddComponent<Text>();
        hint.text = skipHintText;
        hint.font = font;
        hint.fontSize = 28;
        hint.alignment = TextAnchor.LowerRight;
        hint.color = new Color(1f, 1f, 1f, 0.6f);
        hint.raycastTarget = false;
        var hintRect = hintGO.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-40f, 30f);
        hintRect.sizeDelta = new Vector2(400f, 50f);

        return new CreditsView { group = group, viewport = viewportRect, content = contentRect };
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
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