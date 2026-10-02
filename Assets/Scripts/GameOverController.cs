using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Placeholder de Game Over hasta que tengas tu propia pantalla/flujo de derrota:
/// desactiva los scripts que le pases (ej: el movimiento del Player), muestra un
/// texto simple en pantalla (creado 100% en runtime, no necesita ningún prefab de UI)
/// y te manda de vuelta al menú después de unos segundos.
///
/// Enganchar TentaclePathManager.onGameOver -> GameOverController.Trigger() en el Inspector.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [Tooltip("Scripts a desactivar al morir (ej: el script de movimiento/input del Player)")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnGameOver;
    [SerializeField] private float reloadDelay = 3f;
    [SerializeField] private string gameOverText = "GAME OVER";
    [Tooltip("Nombre EXACTO de la escena de menú (tal cual aparece en Build Settings)")]
    [SerializeField] private string menuSceneName = "menu";

    [Header("Reseteo al volver al menú")]
    [Tooltip("El Player (buscado por tag 'Player') siempre se destruye antes de volver al menú, para que sea como arrancar el juego de nuevo. Acá podés arrastrar otros objetos persistentes (ej: GameManager) que también necesiten resetearse.")]
    [SerializeField] private GameObject[] extraPersistentObjectsToDestroy;

    private bool triggered = false;

    public void Trigger()
    {
        if (triggered) return;
        triggered = true;

        foreach (var s in scriptsToDisableOnGameOver)
        {
            if (s != null) s.enabled = false;
        }

        ShowGameOverUI();
        Invoke(nameof(GoToMenu), reloadDelay);
    }

    private void ShowGameOverUI()
    {
        GameObject canvasGO = new GameObject("GameOverCanvas_Runtime");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgImage = bgGO.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.9f);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject textGO = new GameObject("GameOverText");
        textGO.transform.SetParent(canvasGO.transform, false);
        var text = textGO.AddComponent<UnityEngine.UI.Text>();
        text.text = gameOverText;
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
        // 1. Destruir al Player persistente
        if (Player.Instance != null)
        {
            Destroy(Player.Instance.gameObject);
        }
        else
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) Destroy(playerGO);
        }

        // 2. Destruir los singletons persistentes que deban resetearse de raíz
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }

        // Si también necesitás resetear otros singletons (Dialogue, AudioManager, etc.):
        if (DialogueManager.Instance != null)
        {
            Destroy(DialogueManager.Instance.gameObject);
        }

        // 3. Limpieza de seguridad de la lista manual (con null-check seguro)
        if (extraPersistentObjectsToDestroy != null)
        {
            for (int i = 0; i < extraPersistentObjectsToDestroy.Length; i++)
            {
                var target = extraPersistentObjectsToDestroy[i];
                if (target != null)
                {
                    Destroy(target);
                }
            }
        }

        // 4. Asegurar que la escala de tiempo esté en 1 antes de cargar el menú
        Time.timeScale = 1f;
        Player.UnlockCursor();

        SceneManager.LoadScene(menuSceneName);
    }
}