using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Este scrip va en un GameObject vacío con un Collider (se fuerza a Trigger solo).
/// Apenas el player entra en el collider, dispara un diálogo con choices. Si elige
/// la opción marcada con leaveActionId, carga la escena indicada.
///
/// Para que un diálogo se diga UNA sola vez aunque se cambie de escena (entrar y salir
/// de habitaciones), completá "Persistent Flag Id" con un id único. Ese flag se guarda
/// en el GameManager (y también en las partidas guardadas).
/// </summary>
[RequireComponent(typeof(Collider))]
public class TriggerSceneChoice : MonoBehaviour
{
    [Header("Diálogo")]
    [SerializeField] private DialogueLine[] lines;

    [Header("Salida")]
    [Tooltip("Debe coincidir con el actionId que se le da a la opción que carga la escena")]
    [SerializeField] private string leaveActionId = "leave_scene";

    [Tooltip("Nombre exacto de la escena a cargar (tiene que estar en Build Settings)")]
    [SerializeField] private string sceneToLoad;

    [Header("Trigger")]
    [Tooltip("Si está tildado, el trigger se dispara una sola vez mientras la escena siga cargada. Al recargar la escena vuelve a dispararse, salvo que uses Persistent Flag Id.")]
    [SerializeField] private bool oneShot = true;

    [Tooltip("ID único (ej: 'maze_spawn_intro'). Si no está vacío, el diálogo se recuerda entre escenas y no se repite al volver. Cada diálogo necesita su propio id.")]
    [SerializeField] private string persistentFlagId;

    private bool hasTriggered = false;

    private bool AlreadyDoneForever =>
        !string.IsNullOrEmpty(persistentFlagId)
        && GameManager.Instance != null
        && GameManager.Instance.HasFlag(persistentFlagId);

    private void Awake()
    {
        // Nos aseguramos de que el collider sea trigger sin depender de configurarlo bien a mano
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        // Si el diálogo ya se dijo en esta partida, el trigger no hace falta más
        if (AlreadyDoneForever)
            gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (AlreadyDoneForever) return;
        if (hasTriggered && oneShot) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        if (!string.IsNullOrEmpty(persistentFlagId) && GameManager.Instance != null)
            GameManager.Instance.SetFlag(persistentFlagId);

        DialogueManager.Instance.ChoiceSelected -= HandleChoiceSelected;
        DialogueManager.Instance.ChoiceSelected += HandleChoiceSelected;

        DialogueManager.Instance.StartDialogue(lines);
    }

    private void HandleChoiceSelected(DialogueChoice choice)
    {
        DialogueManager.Instance.ChoiceSelected -= HandleChoiceSelected;

        if (choice.actionId != leaveActionId) return;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("TriggerSceneChoice: elegiste la opción de salida pero no asignaste 'Scene To Load' en el inspector.");
            return;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}