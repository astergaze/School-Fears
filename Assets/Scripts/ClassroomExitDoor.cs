using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puerta de salida ubicada en las aulas.
/// Permite volver al laberinto de forma normal, pero si el jugador posee las 4 notas
/// en su inventario, inyecta dinámicamente la opción especial hacia 'Hallway'.
/// </summary>
public class ClassroomExitDoor : Interactable
{
    [Header("Diálogo Base")]
    [Tooltip("Líneas normales de la puerta (por ej: '¿Salir al pasillo?' con opciones de volver al maze o quedarse)")]
    [SerializeField] private DialogueLine[] baseLines;

    [Header("Salida Normal (Al Laberinto)")]
    [SerializeField] private string normalLeaveActionId = "leave_scene";
    [SerializeField] private string normalSceneToLoad = "maze";
    [Tooltip("SpawnPoint en el laberinto al cual volver (para no spawnear en la entrada)")]
    [SerializeField] private string returnSpawnPointId;

    [Header("Condición de Notas")]
    [Tooltip("Lista de ScriptableObjects de las notas requeridas")]
    [SerializeField] private List<ItemData> requiredNotes = new List<ItemData>();
    [Tooltip("Cantidad mínima si preferís chequear por conteo en vez de assets exactos")]
    [SerializeField] private int requiredNotesCount = 4;

    [Header("Opción Especial (Hallway)")]
    [Tooltip("ID de localización para la opción especial (ej: 'doorSpecialHallwayOptn'). Si no existe en CSV, usa directSpecialChoiceText")]
    [SerializeField] private string specialChoiceTextId = "doorSpecialHallwayOptn";
    [SerializeField] private string directSpecialChoiceText = "¿El pomo se ve... raro?";
    [SerializeField] private string specialActionId = "go_to_hallway";
    [SerializeField] private string hallwaySceneName = "Hallway";

    [Header("Transición Especial")]
    [Tooltip("Si querés que Alex diga algo antes de cambiar de escena al elegir esta opción")]
    [SerializeField] private DialogueLine[] specialResponseLines;

    [Header("Sonidos")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    public override void Interact(Player player)
    {
        // 1. Clonar las líneas base para no mutar el asset/inspector permanentemente
        DialogueLine[] preparedLines = PrepareLinesWithDynamicChoice(player);

        // 2. Suscribirse a la elección
        DialogueManager.Instance.ChoiceSelected -= HandleChoiceSelected;
        DialogueManager.Instance.ChoiceSelected += HandleChoiceSelected;

        // 3. Iniciar diálogo
        DialogueManager.Instance.StartDialogue(preparedLines);
    }

    private DialogueLine[] PrepareLinesWithDynamicChoice(Player player)
    {
        if (baseLines == null || baseLines.Length == 0) return baseLines;

        // Verificamos si el jugador cumple con las 4 notas
        bool hasAllNotes = CheckPlayerHasAllNotes(player);

        // Creamos una copia superficial del array de líneas
        DialogueLine[] linesCopy = new DialogueLine[baseLines.Length];
        for (int i = 0; i < baseLines.Length; i++)
        {
            DialogueLine original = baseLines[i];
            linesCopy[i] = new DialogueLine
            {
                speaker = original.speaker,
                textId = original.textId,
                choices = original.choices
            };
        }

        // Si no tiene todas las notas, dejamos las choices tal cual estaban
        if (!hasAllNotes) return linesCopy;

        // Buscamos la última línea que tenga choices para anexarle la opción especial
        for (int i = linesCopy.Length - 1; i >= 0; i--)
        {
            if (linesCopy[i].choices != null && linesCopy[i].choices.Length > 0)
            {
                List<DialogueChoice> choicesList = new List<DialogueChoice>(linesCopy[i].choices);

                // Armamos la choice dinámica
                DialogueChoice specialChoice = new DialogueChoice
                {
                    textId = ResolveChoiceTextId(),
                    actionId = specialActionId,
                    state = ChoiceState.Available,
                    responseLines = specialResponseLines // Si tiene líneas, el DialogueManager las reproduce antes de terminar
                };

                // La agregamos al principio o antes de la opción de cancelar
                choicesList.Insert(0, specialChoice);

                linesCopy[i].choices = choicesList.ToArray();
                break;
            }
        }

        return linesCopy;
    }

    private bool CheckPlayerHasAllNotes(Player player)
    {
        if (player == null || player.Inventory == null) return false;

        // Chequeo estricto por items específicos si están asignados
        if (requiredNotes != null && requiredNotes.Count > 0)
        {
            foreach (var note in requiredNotes)
            {
                if (note != null && !player.Inventory.HasItem(note))
                    return false;
            }
            return true;
        }

        // Chequeo alternativo por GameManager o tipo de item QuestItem
        if (GameManager.Instance != null && GameManager.Instance.CollectedNotesCount >= requiredNotesCount)
            return true;

        return false;
    }

    private string ResolveChoiceTextId()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(specialChoiceTextId))
        {
            // Si existe en el CSV devolvemos la key, si no, fallback
            string loc = LocalizationManager.Instance.GetText(specialChoiceTextId);
            if (!loc.StartsWith("[")) return specialChoiceTextId;
        }
        return directSpecialChoiceText;
    }

    protected virtual void HandleChoiceSelected(DialogueChoice choice)
    {
        // 1. Opción normal de volver al laberinto
        if (choice.actionId == normalLeaveActionId)
        {
            DialogueManager.Instance.ChoiceSelected -= HandleChoiceSelected;
            SceneTransitioner.Instance.GoToScene(normalSceneToLoad, openSound, closeSound, returnSpawnPointId);
        }
        // 2. Opción secreta/especial hacia Hallway
        else if (choice.actionId == specialActionId)
        {
            DialogueManager.Instance.ChoiceSelected -= HandleChoiceSelected;

            // Si definiste responseLines, esperamos a que termine ese diálogo antes de cargar la escena
            if (specialResponseLines != null && specialResponseLines.Length > 0)
            {
                DialogueManager.Instance.OnDialogueEnded -= TransitionToHallway;
                DialogueManager.Instance.OnDialogueEnded += TransitionToHallway;
            }
            else
            {
                TransitionToHallway();
            }
        }
    }

    private void TransitionToHallway()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= TransitionToHallway;

        SceneTransitioner.Instance.GoToScene(hallwaySceneName, openSound, closeSound);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ChoiceSelected -= HandleChoiceSelected;
            DialogueManager.Instance.OnDialogueEnded -= TransitionToHallway;
        }
    }
}