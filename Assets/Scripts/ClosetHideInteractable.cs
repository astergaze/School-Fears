using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Armario donde el jugador se esconde del Rector (Aula 1 y Aula 2).
/// - Si el evento del Rector NO está activo (o ya se escondió una vez): dice que está vacío.
/// - Si está activo: fade a negro, pasos del Rector, flags de patrulla, fade in y monólogo.
/// Durante toda la secuencia SceneTransitioner.IsTransitioning = true, así que el Player
/// no se mueve, no interactúa y el prompt de "Presiona E" queda oculto.
/// </summary>
public class ClosetHideInteractable : Interactable
{
    [Header("Condición de Activación")]
    [Tooltip("Flag que dispara la puerta del Aula 1 al ser golpeada.")]
    [SerializeField] private string rectorEventFlag = "rector_chase_triggered";

    [Tooltip("Diálogo si interactúa y NO hay peligro (ej: 'Está abierto... no hay nada dentro').")]
    [FormerlySerializedAs("normalLines")]
    [SerializeField] private DialogueLine[] emptyClosetLines;

    [Tooltip("Si está activo, solo se puede esconder una vez (después dice que está vacío).")]
    [SerializeField] private bool onlyHideOnce = true;

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

        GameManager gm = GameManager.Instance;
        bool isChasing = gm != null && gm.HasFlag(rectorEventFlag);
        bool alreadyHidden = onlyHideOnce && gm != null && gm.HasFlag(hiddenOnceFlag);

        if (!isChasing || alreadyHidden)
        {
            // Sin peligro: "Está abierto... no hay nada dentro"
            if (emptyClosetLines != null && emptyClosetLines.Length > 0 && DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(emptyClosetLines);
            return;
        }

        StartCoroutine(HideSequence());
    }

    private IEnumerator HideSequence()
    {
        isHiding = true;

        // Bloquea movimiento/interacción y mantiene el prompt apagado durante todo el fade
        SceneTransitioner.IsTransitioning = true;
        InteractionPromptUI.Instance?.HidePrompt();

        // 1. Entrar al armario y fade out a negro
        if (enterClosetSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(enterClosetSound);

        if (SceneTransitioner.Instance != null)
            yield return SceneTransitioner.Instance.FadeRoutine(1f);

        yield return new WaitForSeconds(1.0f);

        // 2. Ráfaga de pasos del Rector pasando por la puerta del aula
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

        // 3. Sonido de puerta cerrándose
        if (doorCloseSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(doorCloseSound);

        yield return new WaitForSeconds(1.2f);

        // 4. Registrar flags de avance
        GameManager.Instance?.SetFlag(rectorActiveFlag);
        GameManager.Instance?.SetFlag(hiddenOnceFlag);

        // 5. Fade in
        if (SceneTransitioner.Instance != null)
            yield return SceneTransitioner.Instance.FadeRoutine(0f);

        // Devolvemos el control antes del monólogo (el diálogo mantiene el prompt oculto por sí solo)
        SceneTransitioner.IsTransitioning = false;
        isHiding = false;

        if (postHideLines != null && postHideLines.Length > 0 && DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(postHideLines);
    }

    private void OnDisable()
    {
        // Si el objeto se desactiva a mitad de secuencia, no dejamos al jugador trabado ni a oscuras
        if (!isHiding) return;

        isHiding = false;
        SceneTransitioner.IsTransitioning = false;
        SceneTransitioner.Instance?.FadeScreen(0f);
    }
}