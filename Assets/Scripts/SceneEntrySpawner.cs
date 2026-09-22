using UnityEngine;

/// <summary>
/// Este script va en un GameObject vacío de escenas con una sola entrada
/// (las aulas, o cualquier escena a la que siempre se llega por el mismo lugar).
/// Al cargar la escena, reposiciona al player persistido ahi.
///
/// Si la escena tuviera varias puertas de entrada distintas, usar MazePlayerSpawner
/// en su lugar (ese sí soporta múltiples SpawnPoint con id).
/// </summary>
public class SceneEntrySpawner : MonoBehaviour
{
    [Tooltip("Punto exacto donde debe aparecer el player al cargar esta escena")]
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("SceneEntrySpawner: no hay Spawn Point asignado en el inspector.");
            return;
        }

        Player.Instance.TeleportTo(spawnPoint.position, spawnPoint.rotation);
    }
}