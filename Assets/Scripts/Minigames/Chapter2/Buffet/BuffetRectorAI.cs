using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// La figura etérea del buffet (el Rector / "regente").
///
/// Estados:
///  - Dormant: quieto al lado de la puerta con sombra, hasta que alguien llame a StartPatrol().
///  - Patrol:  recorre los Patrol Waypoints en bucle, a velocidad lenta.
///  - Chase:   te vio parado (o agachado SIN cobertura): te persigue.
///  - Search:  te perdió de vista: va al último lugar donde te vio, mira alrededor y vuelve a patrullar.
///
/// Cómo funciona el escondite: NO hay un "estás escondido sí/no" mágico. El Rector lanza un raycast
/// desde sus ojos hasta tu cabeza. Si hay una mesa/mostrador en el medio (collider incluido en
/// Sight Blockers), no te ve. Agachado, tu "cabeza" para el Rector queda a Crouched Target Height
/// metros del piso, por debajo del borde de las mesas; parado, es la posición de tu cámara.
/// Agacharse SIN nada adelante no te esconde: el raycast llega igual.
/// Esta velocidad es propia: NO depende de Player.CurrentMoveSpeed, así que agacharte no lo enlentece.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BuffetRectorAI : MonoBehaviour
{
    public enum State { Dormant, Patrol, Chase, Search }
    public State CurrentState => state;

    [Header("Patrulla")]
    [SerializeField] private Transform[] patrolWaypoints;
    [SerializeField] private float waypointTolerance = 0.8f;
    [Tooltip("Segundos que se queda parado en cada waypoint (0 = no se frena).")]
    [SerializeField] private float pauseAtWaypoint = 0f;
    [Tooltip("Si está activo, arranca a patrullar solo al empezar la escena (sin esperar a StartPatrol).")]
    [SerializeField] private bool startActive = false;

    [Header("Velocidades (m/s) — el jugador camina a 5")]
    [SerializeField] private float patrolSpeed = 2.0f;
    [SerializeField] private float chaseSpeed = 4.6f;

    [Header("Visión")]
    [Tooltip("Hijo del Rector a la altura de los ojos (~1.8 m). Si queda vacío usa Eye Height.")]
    [SerializeField] private Transform eyes;
    [SerializeField] private float eyeHeight = 1.8f;
    [SerializeField] private float viewDistance = 14f;
    [SerializeField, Range(30f, 360f)] private float viewAngle = 110f;
    [Tooltip("Dentro de esta distancia te nota aunque estés fuera del cono (pero igual necesita línea de visión: una mesa en el medio lo bloquea).")]
    [SerializeField] private float closeSenseDistance = 2.5f;
    [Tooltip("Capas que bloquean la vista: mesas, mostradores, paredes. El Player y el propio Rector se ignoran solos.")]
    [SerializeField] private LayerMask sightBlockers = ~0;
    [Tooltip("Altura (metros desde el piso) a la que te ve cuando estás agachado. Tiene que ser MENOR que el borde de las mesas.")]
    [SerializeField] private float crouchedTargetHeight = 0.6f;
    [Tooltip("Segundos viéndote antes de pasar a perseguir (te da un instante para reaccionar).")]
    [SerializeField] private float spotTime = 0.25f;
    [Tooltip("Segundos sin verte antes de dejar de perseguir e ir al último lugar donde te vio.")]
    [SerializeField] private float loseSightTime = 1.5f;
    [Tooltip("Segundos que mira alrededor al llegar al último lugar visto, antes de volver a patrullar.")]
    [SerializeField] private float searchLookTime = 3f;

    [Header("Captura")]
    [SerializeField] private float catchDistance = 1.5f;
    [SerializeField] private AudioClip catchSound;
    [Tooltip("Engancharlo a GameOverController.Trigger().")]
    public UnityEvent onCaught;
    [Tooltip("Opcional: escena a la que ir al capturarte (en vez de / además de Game Over). Vacío = no cambia de escena.")]
    [SerializeField] private string caughtSceneName = "";

    [Header("Eventos")]
    public UnityEvent onSpotted;

    [Header("Pasos (mismos clips que Alex)")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float stepDistance = 2.2f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.85f;
    [SerializeField] private float footstepPitchMin = 0.65f;
    [SerializeField] private float footstepPitchMax = 0.75f;

    private NavMeshAgent agent;
    private State state = State.Dormant;
    private int waypointIndex;
    private bool waiting;
    private float waitTimer;
    private float seenTimer;
    private float lostTimer;
    private float searchTimer;
    private float repathTimer;
    private Vector3 lastKnownPos;
    private bool arrivedAtLastKnown;
    private bool caught;

    private Vector3 lastPosition;
    private float distanceAccumulated;
    private int lastClipIndex = -1;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[32];

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false;      // que no frene en cada waypoint
    }

    private void Start()
    {
        lastPosition = transform.position;
        agent.speed = patrolSpeed;
        SetStopped(true);

        if (!agent.isOnNavMesh)
            Debug.LogWarning("BuffetRectorAI: el Rector no está sobre el NavMesh. ¿Bakeaste? ¿Está apoyado en el piso?", this);

        if (startActive) StartPatrol();
    }

    // isStopped solo se puede tocar si el agente está sobre el NavMesh (si no, Unity tira error)
    private void SetStopped(bool stopped)
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = stopped;
    }

    // ================= API pública =================

    public void StartPatrol()
    {
        if (state != State.Dormant || caught) return;
        if (!agent.isOnNavMesh) { Debug.LogWarning("BuffetRectorAI: no se puede patrullar fuera del NavMesh.", this); return; }
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) { Debug.LogWarning("BuffetRectorAI: no hay Patrol Waypoints.", this); return; }

        state = State.Patrol;
        SetStopped(false);
        agent.speed = patrolSpeed;
        GoToCurrentWaypoint();
    }

    public void StopAndFreeze()
    {
        state = State.Dormant;
        if (agent.isOnNavMesh) SetStopped(true);
    }

    // ================= Update =================

    private void Update()
    {
        if (caught || state == State.Dormant) return;

        var player = Player.Instance;
        if (player == null) return;

        UpdateFootsteps();

        bool sees = CanSeePlayer();

        switch (state)
        {
            case State.Patrol: UpdatePatrol(sees); break;
            case State.Chase: UpdateChase(sees); break;
            case State.Search: UpdateSearch(sees); break;
        }

        // Captura: cerca Y con línea de visión (una mesa en el medio te salva)
        if (!caught && HorizontalDistanceToPlayer() <= catchDistance && HasLineOfSight())
            Catch();
    }

    private void UpdatePatrol(bool sees)
    {
        if (sees)
        {
            seenTimer += Time.deltaTime;
            if (seenTimer >= spotTime) { EnterChase(); return; }
        }
        else seenTimer = Mathf.Max(0f, seenTimer - Time.deltaTime);

        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) { waiting = false; SetStopped(false); AdvanceWaypoint(); }
            return;
        }

        if (Arrived())
        {
            if (pauseAtWaypoint > 0f) { waiting = true; waitTimer = pauseAtWaypoint; SetStopped(true); }
            else AdvanceWaypoint();
        }
    }

    private void UpdateChase(bool sees)
    {
        if (sees)
        {
            lostTimer = 0f;
            lastKnownPos = Player.Instance.transform.position;
            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f) { SetDestinationNear(lastKnownPos); repathTimer = 0.15f; }
        }
        else
        {
            lostTimer += Time.deltaTime;
            if (lostTimer >= loseSightTime) EnterSearch();
        }
    }

    private void UpdateSearch(bool sees)
    {
        if (sees) { EnterChase(); return; }

        if (!arrivedAtLastKnown)
        {
            if (Arrived()) { arrivedAtLastKnown = true; searchTimer = 0f; SetStopped(true); }
            return;
        }

        // Mira alrededor girando en el lugar
        transform.Rotate(0f, 70f * Time.deltaTime, 0f);
        searchTimer += Time.deltaTime;
        if (searchTimer >= searchLookTime) ReturnToPatrol();
    }

    // ================= Transiciones =================

    private void EnterChase()
    {
        state = State.Chase;
        seenTimer = 0f;
        lostTimer = 0f;
        waiting = false;
        SetStopped(false);
        agent.speed = chaseSpeed;
        lastKnownPos = Player.Instance.transform.position;
        SetDestinationNear(lastKnownPos);
        onSpotted?.Invoke();
    }

    private void EnterSearch()
    {
        state = State.Search;
        arrivedAtLastKnown = false;
        SetStopped(false);
        agent.speed = patrolSpeed * 1.4f;
        SetDestinationNear(lastKnownPos);
    }

    private void ReturnToPatrol()
    {
        state = State.Patrol;
        seenTimer = 0f;
        SetStopped(false);
        agent.speed = patrolSpeed;
        GoToCurrentWaypoint();
    }

    // ================= Waypoints =================

    private void GoToCurrentWaypoint()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return;
        SetDestinationNear(patrolWaypoints[waypointIndex].position);
    }

    private void AdvanceWaypoint()
    {
        waypointIndex = (waypointIndex + 1) % patrolWaypoints.Length;
        GoToCurrentWaypoint();
    }

    private bool Arrived()
    {
        if (agent.pathPending) return false;
        if (agent.pathStatus == NavMeshPathStatus.PathInvalid) return true;   // destino inalcanzable: no quedarse trabado
        return agent.remainingDistance <= waypointTolerance;
    }

    private void SetDestinationNear(Vector3 pos)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(pos);
    }

    // ================= Visión =================

    private Vector3 EyePosition => eyes != null ? eyes.position : transform.position + Vector3.up * eyeHeight;

    /// <summary>Punto del jugador hacia el que el Rector "mira".</summary>
    private Vector3 PlayerTargetPoint()
    {
        var p = Player.Instance;
        if (CrouchController.IsCrouching && p.playerCharacterController != null)
        {
            float feetY = p.playerCharacterController.bounds.min.y;
            Vector3 pos = p.transform.position;
            return new Vector3(pos.x, feetY + crouchedTargetHeight, pos.z);
        }
        return p.playerCamera != null ? p.playerCamera.position : p.transform.position + Vector3.up * 1.6f;
    }

    private bool CanSeePlayer()
    {
        Vector3 eye = EyePosition;
        Vector3 target = PlayerTargetPoint();
        Vector3 to = target - eye;
        float dist = to.magnitude;

        if (dist > viewDistance) return false;

        if (dist > closeSenseDistance)
        {
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            Vector3 fwd = transform.forward; fwd.y = 0f;
            if (flat.sqrMagnitude > 0.0001f && Vector3.Angle(fwd, flat) > viewAngle * 0.5f) return false;
        }

        return !LineBlocked(eye, target);
    }

    private bool HasLineOfSight() => !LineBlocked(EyePosition, PlayerTargetPoint());

    private bool LineBlocked(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.01f) return false;

        Transform playerRoot = Player.Instance != null ? Player.Instance.transform : null;
        int n = Physics.RaycastNonAlloc(from, dir / dist, hitBuffer, dist, sightBlockers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            Collider c = hitBuffer[i].collider;
            if (c == null) continue;
            if (c.transform.IsChildOf(transform)) continue;                       // el propio Rector
            if (playerRoot != null && c.transform.IsChildOf(playerRoot)) continue; // el jugador
            return true;
        }
        return false;
    }

    private float HorizontalDistanceToPlayer()
    {
        Vector3 d = Player.Instance.transform.position - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    // ================= Captura =================

    private void Catch()
    {
        caught = true;
        SetStopped(true);
        if (agent.isOnNavMesh) agent.velocity = Vector3.zero;

        if (catchSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(catchSound);

        onCaught?.Invoke();

        if (!string.IsNullOrEmpty(caughtSceneName) && SceneTransitioner.Instance != null)
            SceneTransitioner.Instance.GoToScene(caughtSceneName, null, null);
    }

    // ================= Pasos =================

    private void UpdateFootsteps()
    {
        if (!agent.isOnNavMesh || agent.isStopped || agent.velocity.sqrMagnitude < 0.05f)
        {
            lastPosition = transform.position;
            return;
        }

        Vector3 delta = transform.position - lastPosition;
        delta.y = 0f;
        distanceAccumulated += delta.magnitude;
        lastPosition = transform.position;

        if (distanceAccumulated >= stepDistance)
        {
            distanceAccumulated = 0f;
            if (footstepClips == null || footstepClips.Length == 0 || AudioManager.Instance == null) return;
            AudioClip clip = AudioManager.Instance.PickRandom(footstepClips, ref lastClipIndex);
            AudioManager.Instance.PlaySFX(clip, footstepVolume, footstepPitchMin, footstepPitchMax);
        }
    }

    // ================= Gizmos =================

    private void OnDrawGizmosSelected()
    {
        Vector3 eye = EyePosition;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);

        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireSphere(transform.position, closeSenseDistance);

        Gizmos.color = Color.yellow;
        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawRay(eye, left * viewDistance);
        Gizmos.DrawRay(eye, right * viewDistance);

        if (patrolWaypoints != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolWaypoints.Length; i++)
            {
                if (patrolWaypoints[i] == null) continue;
                Transform next = patrolWaypoints[(i + 1) % patrolWaypoints.Length];
                Gizmos.DrawSphere(patrolWaypoints[i].position, 0.15f);
                if (next != null) Gizmos.DrawLine(patrolWaypoints[i].position, next.position);
            }
        }
    }
}
