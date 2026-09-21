using UnityEngine;

/// <summary>
/// Marca un punto donde el player puede aparecer al cargar el laberinto.
/// Poné uno de estos en cada puerta que lleva a un aula, con un id único
/// que coincida con el "Spawn Point Id" configurado en esa puerta.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Id único. Tiene que coincidir exactamente con el Spawn Point Id de la puerta correspondiente.")]
    public string id;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}