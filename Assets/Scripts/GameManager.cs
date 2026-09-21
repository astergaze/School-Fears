using UnityEngine;

/// <summary>
/// Singleton persistente. Guarda estado que tiene que sobrevivir entre cargas de escena,
/// como a qué punto exacto del laberinto hay que volver después de salir de un aula.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [HideInInspector] public string pendingSpawnPointId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}