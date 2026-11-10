using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // agregar esta línea

public class PauseMenu : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject pauseMenuUI;

    [Header("Estado")]
    public bool isPaused = false;

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

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuPrincipal");
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}