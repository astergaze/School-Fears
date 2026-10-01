using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TentaclePathManager : MonoBehaviour
{
    [Header("Referencias Principales")]
    [Tooltip("Arrastra el GameObject 'tentaculos guias'")]
    [SerializeField] private Transform tentaculosRoot;
    [Tooltip("Arrastra la Spot Light")]
    [SerializeField] private Transform spotLightTransform;

    [Header("Indicador Visual en Suelo (opcional)")]
    [Tooltip("DEPRECADO: si tenés el efecto de tinta de LimpiadorDeTinta, dejá esto sin material " +
             "(o en 0,0,0) - ya no se usa para la lógica de Game Over, que ahora sigue directamente " +
             "a spotLightTransform. Esto es solo un marcador visual extra, opcional.")]
    [SerializeField] private Material indicatorMaterial;
    [SerializeField] private Vector3 indicatorScale = Vector3.zero;
    [SerializeField] private float groundYOffset = 0.05f;

    [Header("Movimiento")]
    [SerializeField] private float baseMoveSpeed = 3f;
    [SerializeField] private bool useHorizontalXZ = true;
    [Tooltip("Pausa (seg) entre que la luz llega a un nodo y arranca hacia el siguiente. 0 = sin pausa, movimiento continuo")]
    [SerializeField] private float pauseBetweenSteps = 0f;

    [Header("Dificultad Progresiva")]
    [Tooltip("Cuánto aumenta la velocidad de la luz por cada paso recorrido")]
    [SerializeField] private float moveSpeedIncreasePerStep = 0.4f;

    [Header("Eventos")]
    public UnityEvent onChaseStarted;
    public UnityEvent onGameOver;
    public UnityEvent<int> onStepCompleted;
    public UnityEvent onPathCompleted;

    private int currentStepIndex = 0;
    private Coroutine moveRoutine;
    private Coroutine currentMoveSegment;
    private GameObject groundIndicator;

    private bool sequenceStarted = false;
    private bool isGameOver = false;

    private void Start()
    {
        CreateGroundIndicator();
        InitializeNodes();
        PlaceInstantlyAtCurrentStep();
        // La luz queda posicionada en el nodo 0 pero no se mueve todavía:
        // eso lo dispara BeginChase() cuando el jugador la ve (ver PlayerLightVisibility).
    }

    /// <summary>
    /// Llamar desde PlayerLightVisibility.onLightSpotted (o manualmente) para arrancar
    /// el recorrido: a partir de acá la luz avanza SOLA por todos los nodos, cada vez
    /// más rápido, sin esperar ninguna acción del jugador.
    /// </summary>
    public void BeginChase()
    {
        Debug.Log($"[TentaclePathManager] BeginChase() invocado. sequenceStarted={sequenceStarted} isGameOver={isGameOver}");
        if (sequenceStarted || isGameOver) return;
        sequenceStarted = true;
        onChaseStarted?.Invoke();
        moveRoutine = StartCoroutine(RunPathAutonomously());
        Debug.Log("[TentaclePathManager] Secuencia iniciada (movimiento autónomo).");
    }

    /// <summary>
    /// Posición (en XZ) donde "lógicamente" está la luz ahora mismo, proyectada al piso.
    /// Se toma directamente de spotLightTransform -- la MISMA fuente que usa (o debería
    /// usar, vía su campo "Fuente De Luz") LimpiadorDeTinta para el círculo de tinta --
    /// así lo que el jugador VE siempre coincide con lo que el juego usa para decidir
    /// el Game Over. Ya no depende del indicador cilíndrico propio.
    /// </summary>
    public Vector3? GetGroundIndicatorPosition()
    {
        if (spotLightTransform == null) return null;
        return new Vector3(spotLightTransform.position.x, 0f, spotLightTransform.position.z);
    }

    /// <summary>
    /// Permite disparar el Game Over desde afuera (ej: FloorSyncGameOver cuando el
    /// jugador queda parado en un Piso distinto al que está iluminando la luz).
    /// </summary>
    public void ExternalGameOver()
    {
        TriggerGameOver();
    }

    private void CreateGroundIndicator()
    {
        // Marcador opcional - si indicatorMaterial es null o la escala es 0, no se nota
        // en pantalla. Se mantiene solo por compatibilidad / debug visual; la lógica de
        // juego ya NO depende de este objeto (ver GetGroundIndicatorPosition).
        groundIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        groundIndicator.name = "ActiveStep_Highlight (debug, opcional)";

        Collider col = groundIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);

        if (indicatorMaterial != null)
        {
            groundIndicator.GetComponent<MeshRenderer>().material = indicatorMaterial;
        }

        groundIndicator.transform.localScale = indicatorScale;
    }

    private void InitializeNodes()
    {
        if (tentaculosRoot == null) return;

        for (int i = 0; i < tentaculosRoot.childCount; i++)
        {
            Transform child = tentaculosRoot.GetChild(i);

            BoxCollider box = child.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = child.gameObject.AddComponent<BoxCollider>();
            }

            box.isTrigger = true;
            box.size = new Vector3(2f, 2.5f, 2f);
            box.center = new Vector3(0, 1f, 0);

            TentacleStepNode node = child.GetComponent<TentacleStepNode>();
            if (node == null)
                node = child.gameObject.AddComponent<TentacleStepNode>();

            node.Init(i, this);
        }
    }

    /// <summary>
    /// Ya NO controla el avance del camino (ahora es autónomo). Se deja como gancho
    /// informativo por si a futuro querés usarlo (puntaje, checkpoints, feedback visual
    /// de "vas bien"), pero podés dejar los triggers de los nodos tal cual están.
    /// </summary>
    public void OnPlayerReachedStep(int stepIndex)
    {
        // Silenciado por defecto para no inundar la consola; descomentar para debug puntual.
        // Debug.Log($"[TentaclePathManager] OnPlayerReachedStep({stepIndex}) recibido (informativo).");
    }

    private void PlaceInstantlyAtCurrentStep()
    {
        if (tentaculosRoot == null || tentaculosRoot.childCount == 0) return;

        Transform target = tentaculosRoot.GetChild(currentStepIndex);
        Vector3 targetPos = target.position;

        Vector3 lightDest = spotLightTransform != null ? spotLightTransform.position : Vector3.zero;
        if (useHorizontalXZ && spotLightTransform != null)
        {
            lightDest = new Vector3(targetPos.x, spotLightTransform.position.y, targetPos.z);
        }

        Vector3 groundDest = new Vector3(targetPos.x, targetPos.y + groundYOffset, targetPos.z);

        if (spotLightTransform != null) spotLightTransform.position = lightDest;
        if (groundIndicator != null) groundIndicator.transform.position = groundDest;
    }

    private IEnumerator RunPathAutonomously()
    {
        while (tentaculosRoot != null && currentStepIndex < tentaculosRoot.childCount - 1 && !isGameOver)
        {
            currentStepIndex++;
            float speed = baseMoveSpeed + moveSpeedIncreasePerStep * currentStepIndex;

            Transform target = tentaculosRoot.GetChild(currentStepIndex);
            Vector3 targetPos = target.position;

            Vector3 lightDest = spotLightTransform != null ? spotLightTransform.position : Vector3.zero;
            if (useHorizontalXZ && spotLightTransform != null)
            {
                lightDest = new Vector3(targetPos.x, spotLightTransform.position.y, targetPos.z);
            }

            Vector3 groundDest = new Vector3(targetPos.x, targetPos.y + groundYOffset, targetPos.z);

            currentMoveSegment = StartCoroutine(MoveSmoothly(lightDest, groundDest, speed));
            yield return currentMoveSegment;

            if (isGameOver) yield break;

            onStepCompleted?.Invoke(currentStepIndex);

            if (pauseBetweenSteps > 0f)
                yield return new WaitForSeconds(pauseBetweenSteps);
        }

        if (!isGameOver)
        {
            OnPathCompleted();
        }
    }

    private IEnumerator MoveSmoothly(Vector3 lightDest, Vector3 groundDest, float speed)
    {
        while (groundIndicator != null && Vector3.Distance(groundIndicator.transform.position, groundDest) > 0.05f)
        {
            if (spotLightTransform != null)
            {
                spotLightTransform.position = Vector3.MoveTowards(
                    spotLightTransform.position,
                    lightDest,
                    speed * Time.deltaTime
                );
            }

            groundIndicator.transform.position = Vector3.MoveTowards(
                groundIndicator.transform.position,
                groundDest,
                speed * Time.deltaTime
            );

            yield return null;
        }

        if (spotLightTransform != null) spotLightTransform.position = lightDest;
        if (groundIndicator != null) groundIndicator.transform.position = groundDest;
    }

    private void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (moveRoutine != null) StopCoroutine(moveRoutine);
        if (currentMoveSegment != null) StopCoroutine(currentMoveSegment);

        Debug.Log("Game Over: el jugador perdió el rastro de la luz.");
        onGameOver?.Invoke();
    }

    private void OnPathCompleted()
    {
        Debug.Log("Camino completado.");
        if (groundIndicator != null) Destroy(groundIndicator);
        onPathCompleted?.Invoke();
    }
}