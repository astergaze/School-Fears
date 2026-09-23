using UnityEngine;

/// <summary>
/// Poné este script en el mismo GameObject que el Player (necesita su CharacterController).
/// Dispara un sonido de paso cada vez que el player recorre X metros mientras está en el piso,
/// eligiendo un clip al azar (sin repetir el anterior) y variando pitch/volumen.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FootstepPlayer : MonoBehaviour
{
    [Header("Clips")]
    [Tooltip("Poné acá TODOS los clips de paso individuales (de steps1 y steps3, mezclados)")]
    [SerializeField] private AudioClip[] footstepClips;

    [Header("Timing")]
    [Tooltip("Distancia recorrida entre paso y paso. Bajalo si querés pasos más seguidos.")]
    [SerializeField] private float stepDistance = 2f;

    [Header("Variación")]
    [SerializeField] private float volume = 0.8f;
    [SerializeField] private float pitchMin = 0.92f;
    [SerializeField] private float pitchMax = 1.08f;

    private CharacterController controller;
    private Vector3 lastPosition;
    private float distanceAccumulated;
    private int lastClipIndex = -1;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!controller.isGrounded)
        {
            lastPosition = transform.position; // evitamos que el aire cuente como distancia caminada
            return;
        }

        Vector3 horizontalDelta = transform.position - lastPosition;
        horizontalDelta.y = 0f;
        distanceAccumulated += horizontalDelta.magnitude;
        lastPosition = transform.position;

        if (distanceAccumulated >= stepDistance)
        {
            distanceAccumulated = 0f;
            PlayStep();
        }
    }

    private void PlayStep()
    {
        AudioClip clip = AudioManager.Instance.PickRandom(footstepClips, ref lastClipIndex);
        AudioManager.Instance.PlaySFX(clip, volume, pitchMin, pitchMax);
    }
}