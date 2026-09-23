using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Maneja toda transición entre escenas: sonido de apertura, fade a negro
/// (disimula el hitch de carga y separa los tiempos de los dos sonidos),
/// carga de la escena, sonido de cierre ya del otro lado, y fade de vuelta.
/// Las puertas no manejan timing por su cuenta, solo le piden a este manager
/// que las lleve a tal escena.
/// </summary>
public class SceneTransitioner : MonoBehaviour
{
    public static SceneTransitioner Instance { get; private set; }

    [Header("Fade")]
    [Tooltip("CanvasGroup de una imagen negra full screen (Screen Space - Overlay), alpha 0 al inicio")]
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float fadeDuration = 0.4f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeCanvas != null)
        {
            fadeCanvas.alpha = 0f;
            fadeCanvas.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Lleva al player a otra escena, con sonido de apertura ya, fade a negro,
    /// carga, sonido de cierre del otro lado, y fade de vuelta.
    /// pendingSpawnPointId es opcional: pasarlo solo si la escena destino usa
    /// MazePlayerSpawner (varios puntos de entrada); dejar null para SceneEntrySpawner.
    /// </summary>
    public void GoToScene(string sceneName, AudioClip openSound, AudioClip closeSound, string pendingSpawnPointId = null)
    {
        StartCoroutine(TransitionRoutine(sceneName, openSound, closeSound, pendingSpawnPointId));
    }

    private IEnumerator TransitionRoutine(string sceneName, AudioClip openSound, AudioClip closeSound, string pendingSpawnPointId)
    {
        AudioManager.Instance?.PlaySFX(openSound);

        yield return Fade(1f); // a negro, con el sonido de apertura ya sonando/terminando

        if (!string.IsNullOrEmpty(pendingSpawnPointId) && GameManager.Instance != null)
            GameManager.Instance.pendingSpawnPointId = pendingSpawnPointId;

        SceneManager.LoadScene(sceneName);

        AudioManager.Instance?.PlaySFX(closeSound);

        yield return Fade(0f); // volvemos a ver la escena nueva
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvas == null) yield break;

        fadeCanvas.blocksRaycasts = true;
        float startAlpha = fadeCanvas.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // unscaled: funciona igual aunque Time.timeScale esté en 0
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = targetAlpha;
        fadeCanvas.blocksRaycasts = targetAlpha > 0f;
    }
}