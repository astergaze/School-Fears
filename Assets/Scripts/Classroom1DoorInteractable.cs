using System.Collections;
using UnityEngine;

/// <summary>
/// Puerta del Aula 1 (capítulo 2).
/// La PRIMERA vez que el jugador interactúa, tenga llave o no, se dispara el evento del Rector:
/// golpe de puerta + pasos + diálogo de pánico + objetivo "Escóndete en un aula".
/// Después del evento:
///   - Con llave: se abre el diálogo de desbloqueo y puede entrar al Aula 1 (que tiene su propio armario).
///   - Sin llave: dice que está cerrada.
/// </summary>
public class Classroom1DoorInteractable : Interactable
{
    [Header("Claves e Ítems")]
    [SerializeField] private ItemData aula1Key;
    [SerializeField] private string rectorEventFlag = "rector_chase_triggered";
    [SerializeField] private string leaveActionId = "leave_scene";
    [SerializeField] private string targetSceneName = "Aula1";

    [Header("Primer intento - SIN llave")]
    [SerializeField] private DialogueLine[] introLines;

    [Header("Primer intento - CON llave (opcional)")]
    [Tooltip("Si queda vacío, con llave se salta directo al golpe de puerta del Rector.")]
    [SerializeField] private DialogueLine[] introLinesWithKey;

    [Header("Después del golpe (con y sin llave)")]
    [SerializeField] private DialogueLine[] afterBangLines;

    [Header("Intentos posteriores - SIN llave")]
    [Tooltip("Si queda vacío, se repiten las Intro Lines (comportamiento anterior).")]
    [SerializeField] private DialogueLine[] lockedLines;

    [Header("Desbloqueo (con llave, después del evento)")]
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
    private bool sequenceRunning = false; // evita reentrar mientras corre golpe + pasos + diálogos

    public override void Interact(Player player)
    {
        if (sequenceRunning) return;

        bool hasKey = aula1Key != null && player.Inventory != null && player.Inventory.HasItem(aula1Key);
        bool eventTriggered = GameManager.Instance != null && GameManager.Instance.HasFlag(rectorEventFlag);

        // Primera interacción: el Rector aparece sí o sí, tenga llave o no.
        if (!eventTriggered)
        {
            StartAlertSequence(hasKey);
            return;
        }

        // Evento ya disparado
        if (hasKey)
        {
            if (DialogueManager.Instance == null) return;
            DialogueManager.Instance.ChoiceSelected -= HandleUnlockChoice;
            DialogueManager.Instance.ChoiceSelected += HandleUnlockChoice;
            DialogueManager.Instance.StartDialogue(unlockLines);
        }
        else
        {
            DialogueLine[] lines = (lockedLines != null && lockedLines.Length > 0) ? lockedLines : introLines;
            if (lines != null && lines.Length > 0 && DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(lines);
        }
    }

    private void StartAlertSequence(bool hasKey)
    {
        sequenceRunning = true;
        GameManager.Instance?.SetFlag(rectorEventFlag);

        DialogueLine[] lines = hasKey ? introLinesWithKey : introLines;

        if (lines != null && lines.Length > 0 && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnIntroDialogueEnded;
            DialogueManager.Instance.OnDialogueEnded += OnIntroDialogueEnded;
            DialogueManager.Instance.StartDialogue(lines);
        }
        else
        {
            // Sin diálogo de intro: directo al golpe
            StartCoroutine(RectorAlertRoutine());
        }
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

        // 3. Diálogo de pánico de Alex (si no hay líneas, cerramos la secuencia directo)
        if (afterBangLines != null && afterBangLines.Length > 0 && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnAlertFinished;
            DialogueManager.Instance.OnDialogueEnded += OnAlertFinished;
            DialogueManager.Instance.StartDialogue(afterBangLines);
        }
        else
        {
            OnAlertFinished();
        }
    }

    private void OnAlertFinished()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= OnAlertFinished;

        sequenceRunning = false;

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
        sequenceRunning = false;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnIntroDialogueEnded;
            DialogueManager.Instance.OnDialogueEnded -= OnAlertFinished;
            DialogueManager.Instance.ChoiceSelected -= HandleUnlockChoice;
        }
    }
}