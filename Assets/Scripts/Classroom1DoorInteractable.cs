using System.Collections;
using UnityEngine;

public class Classroom1DoorInteractable : Interactable
{
    [Header("Claves e Ítems")]
    [SerializeField] private ItemData aula1Key;
    [SerializeField] private string rectorEventFlag = "rector_chase_triggered";
    [SerializeField] private string leaveActionId = "leave_scene";
    [SerializeField] private string targetSceneName = "Aula1";

    [Header("Diálogos - Primer Intento (Sin Llave)")]
    [SerializeField] private DialogueLine[] introLines;
    [SerializeField] private DialogueLine[] afterBangLines;

    [Header("Diálogos - Desbloqueo (Con Llave)")]
    [SerializeField] private DialogueLine[] unlockLines;

    [Header("Audio - Golpe de Puerta")]
    [SerializeField] private AudioClip doorBangSound;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("Audio - Pasos del Rector (Estilo FootstepPlayer)")]
    [Tooltip("Arrastra los mismos clips de pasos que usa FootstepPlayer")]
    [SerializeField] private AudioClip[] footstepClips;
    [Tooltip("Cantidad de pasos que se escucharán tras el golpe de puerta")]
    [SerializeField] private int stepsToPlay = 5;
    [Tooltip("Tiempo en segundos entre paso y paso")]
    [SerializeField] private float stepInterval = 0.45f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.85f;
    [SerializeField] private float footstepPitchMin = 0.65f; // Pitch bajo para dar peso al Rector
    [SerializeField] private float footstepPitchMax = 0.75f;

    [Header("Objetivo")]
    [SerializeField] private string hideObjectiveText = "¡Escóndete en un aula!";

    private int lastClipIndex = -1;

    public override void Interact(Player player)
    {
        bool hasKey = aula1Key != null && player.Inventory != null && player.Inventory.HasItem(aula1Key);
        bool eventTriggered = GameManager.Instance != null && GameManager.Instance.HasFlag(rectorEventFlag);

        if (hasKey)
        {
            DialogueManager.Instance.ChoiceSelected -= HandleUnlockChoice;
            DialogueManager.Instance.ChoiceSelected += HandleUnlockChoice;
            DialogueManager.Instance.StartDialogue(unlockLines);
        }
        else if (!eventTriggered)
        {
            GameManager.Instance?.SetFlag(rectorEventFlag);
            TriggerRectorAlertSequence();
        }
        else
        {
            if (introLines != null && introLines.Length > 0)
                DialogueManager.Instance.StartDialogue(introLines);
        }
    }

    private void TriggerRectorAlertSequence()
    {
        DialogueManager.Instance.OnDialogueEnded -= OnIntroDialogueEnded;
        DialogueManager.Instance.OnDialogueEnded += OnIntroDialogueEnded;
        DialogueManager.Instance.StartDialogue(introLines);
    }

    private void OnIntroDialogueEnded()
    {
        DialogueManager.Instance.OnDialogueEnded -= OnIntroDialogueEnded;
        StartCoroutine(RectorAlertRoutine());
    }

    private IEnumerator RectorAlertRoutine()
    {
        // 1. Golpe de puerta violento
        if (doorBangSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(doorBangSound, 1f, 1f, 1f);

        yield return new WaitForSeconds(0.3f);

        // 2. Ráfaga de pasos procesada con la misma lógica de FootstepPlayer
        if (footstepClips != null && footstepClips.Length > 0 && AudioManager.Instance != null)
        {
            for (int i = 0; i < stepsToPlay; i++)
            {
                AudioClip clip = AudioManager.Instance.PickRandom(footstepClips, ref lastClipIndex);
                AudioManager.Instance.PlaySFX(clip, footstepVolume, footstepPitchMin, footstepPitchMax);
                yield return new WaitForSeconds(stepInterval);
            }
        }

        // 3. Continuar con el diálogo de pánico de Alex
        DialogueManager.Instance.OnDialogueEnded -= OnAlertFinished;
        DialogueManager.Instance.OnDialogueEnded += OnAlertFinished;
        DialogueManager.Instance.StartDialogue(afterBangLines);
    }

    private void OnAlertFinished()
    {
        DialogueManager.Instance.OnDialogueEnded -= OnAlertFinished;

        if (ObjectivePopupUI.Instance != null)
            ObjectivePopupUI.Instance.ShowMessage(hideObjectiveText, 4.5f);
    }

    private void HandleUnlockChoice(DialogueChoice choice)
    {
        DialogueManager.Instance.ChoiceSelected -= HandleUnlockChoice;
        if (choice.actionId == leaveActionId)
        {
            SceneTransitioner.Instance.GoToScene(targetSceneName, openSound, closeSound);
        }
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnIntroDialogueEnded;
            DialogueManager.Instance.OnDialogueEnded -= OnAlertFinished;
            DialogueManager.Instance.ChoiceSelected -= HandleUnlockChoice;
        }
    }
}