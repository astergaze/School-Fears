using UnityEngine;

/// <summary>
/// Puerta DENTRO del laberinto que lleva a un aula específica.
/// Le pide al SceneTransitioner que maneje sonido + fade + carga, pasándole
/// el id del SpawnPoint ubicado en esta misma puerta, para poder volver acá
/// (y no al inicio del laberinto) cuando el jugador salga del aula.
///
/// Opcional: si se asigna "Required Key", el jugador necesita ese ítem en el inventario
/// para entrar; si no lo tiene, se reproducen las "Locked Lines" y la puerta no se abre.
/// Con "Required Key" vacío se comporta exactamente como antes.
/// </summary>
public class MazeDoorToRoom : Interactable
{
    [Tooltip("Nombre exacto de la escena del aula (tiene que estar en Build Settings)")]
    [SerializeField] private string roomSceneName;

    [Tooltip("Id único de este punto de entrada. Tiene que coincidir con un SpawnPoint puesto acá mismo, en el laberinto.")]
    [SerializeField] private string spawnPointId;

    [Header("Llave (opcional)")]
    [Tooltip("Ítem necesario para entrar (ej: aula2key). Vacío = puerta siempre abierta.")]
    [SerializeField] private ItemData requiredKey;
    [Tooltip("Lo que se dice si la puerta está cerrada con llave (ej: 'Está cerrada con llave.').")]
    [SerializeField] private DialogueLine[] lockedLines;

    [Header("Sonido")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [Tooltip("Suena al intentar abrir sin la llave (picaporte trabado).")]
    [SerializeField] private AudioClip lockedSound;
    [Tooltip("Suena al abrir con la llave, justo antes de la transición.")]
    [SerializeField] private AudioClip unlockSound;

    public override void Interact(Player player)
    {
        if (requiredKey != null)
        {
            bool hasKey = player.Inventory != null && player.Inventory.HasItem(requiredKey);

            if (!hasKey)
            {
                if (lockedSound != null && AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(lockedSound);

                if (lockedLines != null && lockedLines.Length > 0 && DialogueManager.Instance != null)
                    DialogueManager.Instance.StartDialogue(lockedLines);
                else
                    Debug.LogWarning("MazeDoorToRoom: puerta con llave pero sin 'Locked Lines' asignadas.", this);

                return;
            }

            if (unlockSound != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(unlockSound);
        }

        SceneTransitioner.Instance.GoToScene(roomSceneName, openSound, closeSound, spawnPointId);
    }
}