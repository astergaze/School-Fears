using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Este scrip va en un GameObject vacío con un Collider (se fuerza a Trigger solo).
/// Apenas el player entra en el collider, dispara un diálogo con choices. Si elige
/// la opción marcada con leaveActionId, carga la escena indicada.
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
    [Tooltip("Si está tildado, el trigger se dispara una sola vez; después queda inactivo aunque el player vuelva a entrar.")]
    [SerializeField] private bool oneShot = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        // Nos aseguramos de que el collider sea trigger sin depender de configurarlo bien a mano
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && oneShot) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

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