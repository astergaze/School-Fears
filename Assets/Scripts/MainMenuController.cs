using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Paneles del Menú")]
    public GameObject mainMenuPanel;
    public GameObject continuePanel;

    [Header("Panel de Salida")]
    public GameObject confirmQuitPanel; // <-- Aquí enlazaremos tu nuevo cartel

    public void AbrirContinuar()
    {
        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(true);
    }

    public void VolverAlMenu()
    {
        continuePanel.SetActive(false);
        if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void LoadGameScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // --- NUEVAS FUNCIONES PARA EL CARTEL DE SALIDA ---

    // 1. Apaga el menú principal y prende el cartel de pregunta
    public void IntentarSalir()
    {
        mainMenuPanel.SetActive(false);
        confirmQuitPanel.SetActive(true);
    }

    // 2. Apaga el cartel y vuelve al menú principal (Botón "NO")
    public void CancelarSalida()
    {
        confirmQuitPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // 3. Cierra el juego definitivamente (Botón "SÍ")
    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}