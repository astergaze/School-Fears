using UnityEngine;

/// <summary>
/// Puerta DENTRO del laberinto que lleva a un aula específica.
/// Le pide al SceneTransitioner que maneje sonido + fade + carga, pasándole
/// el id del SpawnPoint ubicado en esta misma puerta, para poder volver acá
/// (y no al inicio del laberinto) cuando el jugador salga del aula.
/// </summary>
public class MazeDoorToRoom : Interactable
{
    [Tooltip("Nombre exacto de la escena del aula (tiene que estar en Build Settings)")]
    [SerializeField] private string roomSceneName;

    [Tooltip("Id único de este punto de entrada. Tiene que coincidir con un SpawnPoint puesto acá mismo, en el laberinto.")]
    [SerializeField] private string spawnPointId;

    [Header("Sonido")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    public override void Interact(Player player)
    {
        SceneTransitioner.Instance.GoToScene(roomSceneName, openSound, closeSound, spawnPointId);
    }
}