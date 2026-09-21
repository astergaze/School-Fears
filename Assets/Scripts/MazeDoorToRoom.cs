using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Puerta DENTRO del laberinto que lleva a un aula específica.
/// Antes de cargar la escena, guarda en el GameManager el id del SpawnPoint
/// ubicado en esta misma puerta, para poder volver acá (y no al inicio del laberinto)
/// cuando el jugador salga del aula.
/// </summary>
public class MazeDoorToRoom : Interactable
{
    [Tooltip("Nombre exacto de la escena del aula (tiene que estar en Build Settings)")]
    [SerializeField] private string roomSceneName;

    [Tooltip("Id único de este punto de entrada. Tiene que coincidir con un SpawnPoint puesto acá mismo, en el laberinto.")]
    [SerializeField] private string spawnPointId;

    public override void Interact(Player player)
    {
        GameManager.Instance.pendingSpawnPointId = spawnPointId;
        SceneManager.LoadScene(roomSceneName);
    }
}