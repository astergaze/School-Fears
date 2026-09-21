using UnityEngine;

/// <summary>
/// Poné este script en un GameObject vacío de la escena del laberinto.
/// Al cargar la escena, revisa si el GameManager tiene guardado un punto de
/// vuelta (porque el jugador viene de salir de un aula) y reposiciona al
/// player ahí. Si no hay ninguno guardado (primera vez que entra al laberinto),
/// usa el spawn point default.
/// </summary>
public class MazePlayerSpawner : MonoBehaviour
{
    [Tooltip("Punto de spawn usado la primera vez que se entra al laberinto")]
    [SerializeField] private Transform defaultSpawnPoint;

    private void Start()
    {
        Transform target = defaultSpawnPoint;
        string pendingId = GameManager.Instance.pendingSpawnPointId;

        if (!string.IsNullOrEmpty(pendingId))
        {
            SpawnPoint[] points = FindObjectsOfType<SpawnPoint>();
            foreach (var point in points)
            {
                if (point.id == pendingId)
                {
                    target = point.transform;
                    break;
                }
            }

            GameManager.Instance.pendingSpawnPointId = null; // se consume: si vuelve a entrar sin usar puerta, no lo reusa
        }

        if (target == null)
        {
            Debug.LogError("MazePlayerSpawner: no hay Default Spawn Point asignado y no se encontró el SpawnPoint pendiente.");
            return;
        }

        Player.Instance.transform.position = target.position;
        Player.Instance.transform.rotation = target.rotation;
    }
}