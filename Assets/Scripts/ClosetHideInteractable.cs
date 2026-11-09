using System.Collections;
using UnityEngine;

public class ClosetHideInteractable : Interactable
{
    [Header("Condición de Activación")]
    [Tooltip("Flag que dispara la puerta del Aula 1 al ser golpeada.")]
    [SerializeField] private string rectorEventFlag = "rector_chase_triggered";
    [Tooltip("Diálogo si interactúa ANTES de que el Rector empiece a buscarlo.")]
    [SerializeField] private DialogueLine[] normalLines;

    [Header("Flags de Progreso")]
    [Tooltip("Flag que activa la patrulla global del Rector al salir del armario.")]
    [SerializeField] private string rectorActiveFlag = "rector_patrol_active";
    [SerializeField] private string hiddenOnceFlag = "has_hidden_in_closet";

    [Header("Audio - Secuencia de Armario")]
    [SerializeField] private AudioClip enterClosetSound;
    [SerializeField] private AudioClip doorCloseSound;

    [Header("Audio - Pasos del Rector (Estilo FootstepPlayer)")]
    [Tooltip("Arrastra la misma lista de clips que FootstepPlayer")]
    [SerializeField] private AudioClip[] footstepClips;
    [Tooltip("Cantidad de pasos que se escucharán pasando por fuera del aula")]
    [SerializeField] private int stepsToPlay = 8;
    [Tooltip("Cadencia de los pasos de caminata por el pasillo")]
    [SerializeField] private float stepInterval = 0.5f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.8f;
    [SerializeField] private float footstepPitchMin = 0.65f;
    [SerializeField] private float footstepPitchMax = 0.75f;

    [Header("Diálogo tras salir del escondite")]
    [SerializeField] private DialogueLine[] postHideLines;

    private bool isHiding = false;
    private int lastClipIndex = -1;

    public override void Interact(Player player)
    {
        if (isHiding) return;

        // Validar si el evento del Rector ya fue disparado
        bool isChasing = GameManager.Instance != null && GameManager.Instance.HasFlag(rectorEventFlag);

        if (!isChasing)
        {
            // Diálogo por defecto cuando no hay peligro
            if (normalLines != null && normalLines.Length > 0 && DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(normalLines);
            return;
        }

        // Si el peligro está activo, inicia la secuencia de escondite
        StartCoroutine(HideSequence(player));
    }

    private IEnumerator HideSequence(Player player)
    {
        isHiding = true;

        // 1. Entrar al armario y Fade out a negro
        if (enterClosetSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(enterClosetSound);

        yield return SceneTransitioner.Instance.FadeRoutine(1f);
        InteractionPromptUI.Instance?.HidePrompt();

        yield return new WaitForSeconds(1.0f);

        // 2. Simular ráfaga de pasos del Rector pasando por la puerta del aula
        if (footstepClips != null && footstepClips.Length > 0 && AudioManager.Instance != null)
        {
            for (int i = 0; i < stepsToPlay; i++)
            {
                AudioClip clip = AudioManager.Instance.PickRandom(footstepClips, ref lastClipIndex);
                AudioManager.Instance.PlaySFX(clip, footstepVolume, footstepPitchMin, footstepPitchMax);
                yield return new WaitForSeconds(stepInterval);
            }
        }

        yield return new WaitForSeconds(0.8f);

        // 3. Sonido de puerta cerrándose (simula que se aleja o cierra la puerta del aula)
        if (doorCloseSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(doorCloseSound);

        yield return new WaitForSeconds(1.2f);

        // 4. Registrar flags de avance
        GameManager.Instance?.SetFlag(rectorActiveFlag);
        GameManager.Instance?.SetFlag(hiddenOnceFlag);

        // 5. Fade In y monólogo interno de Alex
        yield return SceneTransitioner.Instance.FadeRoutine(0f);

        if (postHideLines != null && postHideLines.Length > 0 && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(postHideLines);
        }

        isHiding = false;
    }
}