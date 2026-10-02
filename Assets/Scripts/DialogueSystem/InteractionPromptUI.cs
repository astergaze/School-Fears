using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    private static InteractionPromptUI instance;
    public static InteractionPromptUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<InteractionPromptUI>();
                if (instance == null)
                {
                    GameObject go = new GameObject("InteractionPromptUI_Persistent");
                    instance = go.AddComponent<InteractionPromptUI>();
                }
            }
            return instance;
        }
    }

    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TextMeshProUGUI promptText;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        if (promptRoot == null || promptText == null)
        {
            BuildRuntimeUI();
        }

        HidePrompt();
    }

    public void ShowPrompt(string text)
    {
        if (promptText != null)
            promptText.text = text;

        if (promptRoot != null)
            promptRoot.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptRoot != null)
            promptRoot.SetActive(false);
    }

    private void BuildRuntimeUI()
    {
        // Canvas propio persistente para que no dependa de si la escena tiene Canvas o no
        Canvas runtimeCanvas = gameObject.AddComponent<Canvas>();
        runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        runtimeCanvas.sortingOrder = 950; // por debajo del fade de SceneTransitioner (que suele ser 1000+)

        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        promptRoot = new GameObject("PromptRoot");
        promptRoot.transform.SetParent(transform, false);

        RectTransform rect = promptRoot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.08f);
        rect.anchorMax = new Vector2(0.5f, 0.08f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800f, 60f);

        GameObject textGO = new GameObject("PromptText");
        textGO.transform.SetParent(promptRoot.transform, false);

        promptText = textGO.AddComponent<TextMeshProUGUI>();
        promptText.fontSize = 28;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.raycastTarget = false;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
}