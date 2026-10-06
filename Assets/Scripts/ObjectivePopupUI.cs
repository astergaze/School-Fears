using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Singleton persistente que maneja la UI de avisos tipo 'objetivo/misión'.
/// Se autoconstruye si no existe en la escena.
/// </summary>
public class ObjectivePopupUI : MonoBehaviour
{
    private static ObjectivePopupUI instance;
    public static ObjectivePopupUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ObjectivePopupUI>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ObjectivePopupUI_Persistent");
                    instance = go.AddComponent<ObjectivePopupUI>();
                }
            }
            return instance;
        }
    }

    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private CanvasGroup popupGroup;

    [Header("Timings por Defecto")]
    [SerializeField] private float defaultFadeInTime = 0.4f;
    [SerializeField] private float defaultHoldTime = 3.5f;
    [SerializeField] private float defaultFadeOutTime = 0.6f;

    private Coroutine activeRoutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        if (popupRoot == null || popupText == null || popupGroup == null)
        {
            BuildRuntimeUI();
        }

        popupGroup.alpha = 0f;
        popupRoot.SetActive(false);
    }

    public void ShowMessage(string text, float? holdTime = null, float? fadeIn = null, float? fadeOut = null)
    {
        if (popupText != null)
            popupText.text = text;

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(ShowRoutine(
            fadeIn ?? defaultFadeInTime,
            holdTime ?? defaultHoldTime,
            fadeOut ?? defaultFadeOutTime));
    }

    public void ShowMessageById(string textId, float? holdTime = null, float? fadeIn = null, float? fadeOut = null)
    {
        string text = (LocalizationManager.Instance != null)
            ? LocalizationManager.Instance.GetText(textId)
            : textId;

        ShowMessage(text, holdTime, fadeIn, fadeOut);
    }

    private IEnumerator ShowRoutine(float fadeIn, float hold, float fadeOut)
    {
        popupRoot.SetActive(true);

        yield return Fade(0f, 1f, fadeIn);
        yield return new WaitForSeconds(hold);
        yield return Fade(1f, 0f, fadeOut);

        popupRoot.SetActive(false);
        activeRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            popupGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            popupGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        popupGroup.alpha = to;
    }

    private void BuildRuntimeUI()
    {
        Canvas runtimeCanvas = gameObject.AddComponent<Canvas>();
        runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        runtimeCanvas.sortingOrder = 960; // Encima de los prompts (950), debajo del fade negro (1000+)

        var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        popupRoot = new GameObject("PopupRoot");
        popupRoot.transform.SetParent(transform, false);

        RectTransform rect = popupRoot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.88f);
        rect.anchorMax = new Vector2(0.5f, 0.88f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1000f, 90f);

        popupGroup = popupRoot.AddComponent<CanvasGroup>();
        popupGroup.interactable = false;
        popupGroup.blocksRaycasts = false;

        GameObject textGO = new GameObject("PopupText");
        textGO.transform.SetParent(popupRoot.transform, false);

        popupText = textGO.AddComponent<TextMeshProUGUI>();
        popupText.fontSize = 32;
        popupText.fontStyle = FontStyles.Bold;
        popupText.alignment = TextAlignmentOptions.Center;
        popupText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        popupText.raycastTarget = false;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }
}