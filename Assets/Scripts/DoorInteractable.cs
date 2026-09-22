using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteractable : Interactable
{
    [Header("Diálogo")]
    [SerializeField] private DialogueLine[] lines; 

    [Header("Salida")]
    [Tooltip("Debe coincidir con el actionId que se le da a la opcion")]
    [SerializeField] private string leaveActionId = "leave_scene";

    [Tooltip("Nombre exacto de la escena a cargar (tiene que estar agregada en Build Settings)")]
    [SerializeField] private string sceneToLoad;

    public override void Interact(Player player)
    {
        DialogueManager.Instance.ChoiceSelected -= HandleChoiceSelected;
        DialogueManager.Instance.ChoiceSelected += HandleChoiceSelected;

        DialogueManager.Instance.StartDialogue(lines);
    }

    private void HandleChoiceSelected(DialogueChoice choice)
    {
        DialogueManager.Instance.ChoiceSelected -= HandleChoiceSelected;

        if (choice.actionId == leaveActionId)
        {
            if (string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.LogError("DoorInteractable: elegiste salir pero no asignaste 'Scene To Load' en el inspector.");
                return;
            }

            SceneManager.LoadScene(sceneToLoad);
        }
    }
    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ChoiceSelected -= HandleChoiceSelected;
        }
    }
}