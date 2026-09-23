using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Reemplaza con el nombre exacto de tu escena de juego
    public void LoadGameScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    public void QuitGame()
    {
        // Cierra la aplicación en una build ejecutable (PC, Mac, etc.)
        Application.Quit();

        // Detiene la reproducción si estás probando dentro del editor de Unity
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}