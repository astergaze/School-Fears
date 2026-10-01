using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Paneles del Menú")]
    public GameObject mainMenuPanel;
    public GameObject continuePanel; // < Actualizado para tu panel de Continuar

    void Start()
    {
        // Al iniciar, el menú principal está activo y el de continuar oculto
        mainMenuPanel.SetActive(true);
        continuePanel.SetActive(false);
    }

    //FUNCIONES PARA ALTERNAR PANELES 

    public void AbrirContinuar()
    {
        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(true);
    }

    public void VolverAlMenu()
    {
        continuePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void LoadGameScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {

        Application.Quit();


#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}