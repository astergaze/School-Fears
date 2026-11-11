using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // agregar esta línea

public class PauseMenu : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject pauseMenuUI;

    [Header("Estado")]
    public bool isPaused = false;

    [Header("Salida")]
    [Tooltip("Nombre exacto de la escena del menú principal (tiene que estar en Build Settings)")]
    [SerializeField] private string menuSceneName = "menu";

    private bool leaving = false;

    void Start()
    {
        pauseMenuUI.SetActive(false); // arranca siempre oculto
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        SaveManager.Instance?.SaveNow();
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        isPaused = true;
        Player.UnlockCursor();
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        isPaused = false;
        Player.LockCursor();
    }

    /// <summary>
    /// Botón "Salir": guarda, hace fade a negro, limpia lo que persiste entre
    /// escenas (igual que GameOverController) y vuelve al menú principal.
    /// </summary>
    public void GoToMainMenu()
    {
        if (leaving) return; // evita doble clic

        // Si la escena no está en Build Settings, abortamos ANTES de destruir nada:
        // así el juego no queda roto (sin cámara ni jugador) si falta agregarla.
        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError($"PauseMenu: la escena '{menuSceneName}' no está en Build Settings. " +
                           "Abrila y usá File > Build Profiles > Add Open Scenes.");
            return;
        }

        leaving = true;
        StartCoroutine(GoToMainMenuRoutine());
    }

    private IEnumerator GoToMainMenuRoutine()
    {
        // Guardado de seguridad antes de destruir los datos de la partida
        SaveManager.Instance?.SaveNow();

        // Ocultamos el menú de pausa de inmediato: así no queda a la vista mientras
        // se funde a negro y se carga el menú principal.
        pauseMenuUI.SetActive(false);

        // Fade a negro: tapa el momento en que no hay cámara y la carga de la escena.
        // Usa tiempo sin escala, así funciona aunque el juego esté pausado.
        if (SceneTransitioner.Instance != null)
            yield return SceneTransitioner.Instance.FadeRoutine(1f);

        // El Player y el GameManager viven entre escenas (DontDestroyOnLoad):
        // hay que destruirlos o seguirían activos dentro del menú.
        if (Player.Instance != null)
            Destroy(Player.Instance.gameObject);

        if (GameManager.Instance != null)
            Destroy(GameManager.Instance.gameObject);

        // Deshacer lo que hizo Pause()
        Time.timeScale = 1f;
        AudioListener.pause = false;
        isPaused = false;
        Player.UnlockCursor();

        SceneManager.LoadScene(menuSceneName);

        // El SceneTransitioner persiste entre escenas: él se encarga de volver
        // a transparente cuando el menú ya está cargado.
        SceneTransitioner.Instance?.FadeScreen(0f);
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}