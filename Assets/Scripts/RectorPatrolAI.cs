using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RectorPatrolAI : MonoBehaviour
{
    [Header("Configuración de Velocidad")]
    [Tooltip("Multiplicador respecto a la velocidad base del jugador (1.5 = 50% más rápido).")]
    [SerializeField] private float speedMultiplier = 1.5f;

    [Header("Patrulla (Puntos de Rastro)")]
    [SerializeField] private Transform[] patrolWaypoints;
    [SerializeField] private float waypointTolerance = 1.2f;

    [Header("Condición de Activación")]
    [SerializeField] private string rectorActiveFlag = "rector_patrol_active";

    [Header("Detención (Muerte/Captura)")]
    [SerializeField] private string detentionSceneName = "Detencion";
    [SerializeField] private float catchDistance = 1.6f;
    [SerializeField] private AudioClip catchSound;

    [Header("Pasos del Rector (Estilo FootstepPlayer)")]
    [Tooltip("Arrastra los mismos 8 clips de pasos que usa Alex")]
    [SerializeField] private AudioClip[] footstepClips;
    [Tooltip("Distancia en metros recorrida por el Rector entre cada pisada")]
    [SerializeField] private float stepDistance = 2.2f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.85f;
    [SerializeField] private float footstepPitchMin = 0.65f;
    [SerializeField] private float footstepPitchMax = 0.75f;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private Transform playerTransform;
    private bool isCatching = false;

    // Control de Pasos
    private Vector3 lastPosition;
    private float distanceAccumulated;
    private int lastClipIndex = -1;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        lastPosition = transform.position;

        if (Player.Instance != null)
        {
            playerTransform = Player.Instance.transform;
            agent.speed = Player.Instance.CurrentMoveSpeed * speedMultiplier;
        }

        bool isActive = GameManager.Instance != null && GameManager.Instance.HasFlag(rectorActiveFlag);
        gameObject.SetActive(isActive);

        if (isActive && patrolWaypoints != null && patrolWaypoints.Length > 0)
        {
            SetNextWaypoint();
        }
    }

    private void Update()
    {
        if (isCatching) return;

        // Si se activó la flag mientras la escena estaba corriendo
        if (!agent.enabled && GameManager.Instance != null && GameManager.Instance.HasFlag(rectorActiveFlag))
        {
            gameObject.SetActive(true);
            agent.enabled = true;
            SetNextWaypoint();
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            return;
        }

        if (Player.Instance != null)
        {
            agent.speed = Player.Instance.CurrentMoveSpeed * speedMultiplier;
        }

        // Pasos dinámicos en patrulla
        UpdateFootsteps();

        // Distancia de captura
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distToPlayer <= catchDistance)
        {
            CatchPlayer();
            return;
        }

        // Avance de waypoints
        if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
        {
            SetNextWaypoint();
        }
    }

    private void UpdateFootsteps()
    {
        if (!agent.isOnNavMesh || agent.velocity.sqrMagnitude < 0.05f)
        {
            lastPosition = transform.position;
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
        if (footstepClips == null || footstepClips.Length == 0 || AudioManager.Instance == null) return;

        AudioClip clip = AudioManager.Instance.PickRandom(footstepClips, ref lastClipIndex);
        AudioManager.Instance.PlaySFX(clip, footstepVolume, footstepPitchMin, footstepPitchMax);
    }

    private void SetNextWaypoint()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;

        agent.SetDestination(patrolWaypoints[currentWaypointIndex].position);
        currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
    }

    private void CatchPlayer()
    {
        isCatching = true;
        agent.isStopped = true;

        // 1. Marcar que Alex fue capturado por el Rector
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag("sent_to_detention");
        }

        if (catchSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(catchSound);

        // 2. Cargar escena de detención (asegurar string exacto "detencion")
        SceneTransitioner.Instance.GoToScene(detentionSceneName, null, null);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}