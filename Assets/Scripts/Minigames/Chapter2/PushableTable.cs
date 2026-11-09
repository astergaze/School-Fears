using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mesa movible del minijuego del aula.
/// - Mirá la mesa y mantené E: la agarrás.
/// - Mientras mantengas E, caminar hacia la mesa la EMPUJA y caminar hacia atrás TIRA de ella.
///   Solo se mueve sobre uno de sus ejes (el que te separa de ella al agarrarla).
/// - Soltá E (o alejate) para soltarla.
/// - Una mesa no puede empujar a otra ni atravesar paredes: se frena y vos con ella.
///
/// Requiere en el prefab: BoxCollider ajustado a la mesa en el objeto raíz (con rotación X/Z = 0),
/// y el mismo Layer de interactuables que usa Player para detectar objetos.
/// Requiere el parche de Player.cs (SpeedMultiplier / CurrentMoveSpeed).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[DefaultExecutionOrder(-50)] // se mueve ANTES que Player.Update en el mismo frame
public class PushableTable : Interactable
{
    public static readonly List<PushableTable> All = new List<PushableTable>();

    [Header("Agarre")]
    [Tooltip("Distancia máxima (en horizontal, hasta el borde de la mesa) para poder agarrarla.")]
    [SerializeField] private float grabRange = 1.4f;
    [Tooltip("Si el jugador se aleja más que esto, suelta la mesa.")]
    [SerializeField] private float releaseDistance = 1.3f;
    [Tooltip("Fracción de la velocidad normal del jugador mientras carga la mesa (da sensación de peso).")]
    [SerializeField, Range(0.1f, 1f)] private float playerSpeedWhileHolding = 0.4f;

    [Header("Colisiones de la mesa")]
    [Tooltip("Qué capas frenan a la mesa. Todo menos el Player está bien (el Player se ignora solo).")]
    [SerializeField] private LayerMask blockers = ~0;
    [SerializeField] private float skin = 0.01f;

    [Header("Sonido (opcional)")]
    [SerializeField] private AudioClip scrapeLoop;
    [SerializeField, Range(0f, 1f)] private float scrapeVolume = 0.7f;

    private static readonly RaycastHit[] hitBuffer = new RaycastHit[32];

    private BoxCollider box;
    private Player holder;
    private InputAction moveAction;
    private InputAction interactAction;
    private Vector3 axis;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float lastMoveTime = -10f;
    private AudioSource scrape;

    public bool IsHeld => holder != null;
    private Quaternion Yaw => Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

    private void Reset()
    {
        // Valor por defecto del prompt al agregar el componente (agregá esta key al CSV)
        promptKey = "promptGrabTable";
    }

    protected override void Awake()
    {
        base.Awake();
        box = GetComponent<BoxCollider>();

        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        startPosition = transform.position;
        startRotation = transform.rotation;

        if (scrapeLoop != null)
        {
            scrape = gameObject.AddComponent<AudioSource>();
            scrape.clip = scrapeLoop;
            scrape.loop = true;
            scrape.playOnAwake = false;
            scrape.spatialBlend = 1f;
            scrape.volume = 0f;
        }
    }

    private void OnEnable() => All.Add(this);

    private void OnDisable()
    {
        All.Remove(this);
        Release();
    }

    // ───────────── Interacción ─────────────

    public override void Interact(Player player)
    {
        if (holder != null) return;
        if (HorizontalDistanceTo(player.transform.position) > grabRange) return;

        var map = player.InputActions.FindActionMap("Player");
        moveAction = map.FindAction("Move");
        interactAction = map.FindAction("Interact");

        // Eje de movimiento: el eje de la mesa más alineado con la línea mesa → jugador
        Vector3 toPlayer = player.transform.position - box.bounds.center;
        toPlayer.y = 0f;
        Vector3 local = Quaternion.Inverse(Yaw) * toPlayer;
        axis = Yaw * (Mathf.Abs(local.x) > Mathf.Abs(local.z) ? Vector3.right : Vector3.forward);

        holder = player;
        holder.SpeedMultiplier = playerSpeedWhileHolding;
    }

    private void Update()
    {
        if (holder != null) HoldUpdate();
        UpdateAudio();
    }

    private void HoldUpdate()
    {
        if (!interactAction.IsPressed() ||
            HorizontalDistanceTo(holder.transform.position) > releaseDistance)
        {
            Release();
            return;
        }

        bool locked = (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                      || SceneTransitioner.IsTransitioning;
        if (locked) return;

        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.0001f) return;

        // Misma conversión input → mundo que usa Player
        Transform cam = holder.playerCamera;
        Vector3 forward = cam.forward; forward.y = 0f; forward.Normalize();
        Vector3 right = cam.right; right.y = 0f; right.Normalize();
        Vector3 wish = right * input.x + forward * input.y;

        float amount = Vector3.Dot(wish, axis) * holder.CurrentMoveSpeed * Time.deltaTime;
        TryMove(axis * amount);
    }

    private void Release()
    {
        if (holder == null) return;
        holder.SpeedMultiplier = 1f;
        holder = null;
    }

    // ───────────── Movimiento con colisión ─────────────

    private void TryMove(Vector3 delta)
    {
        float length = delta.magnitude;
        if (length < 0.0001f) return;
        Vector3 dir = delta / length;

        Vector3 center = transform.TransformPoint(box.center);
        Vector3 scale = transform.lossyScale;
        Vector3 half = Vector3.Scale(box.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.5f
                       - Vector3.one * 0.02f; // un poco más chico: no "toca" piso ni vecinas
        half = Vector3.Max(half, Vector3.one * 0.01f);

        float allowed = length;
        int count = Physics.BoxCastNonAlloc(center, half, dir, hitBuffer, Yaw, length + skin,
                                            blockers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = hitBuffer[i].collider;
            if (c.transform.IsChildOf(transform)) continue;       // yo mismo
            if (c.GetComponentInParent<Player>() != null) continue; // el jugador no frena a la mesa
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hitBuffer[i].distance - skin));
        }

        if (allowed <= 0.0001f) return;

        transform.position += dir * allowed;
        Physics.SyncTransforms(); // el CharacterController ve la nueva posición este mismo frame
        lastMoveTime = Time.time;
    }

    private float HorizontalDistanceTo(Vector3 point)
    {
        Vector3 p = new Vector3(point.x, box.bounds.center.y, point.z);
        return Vector3.Distance(p, box.ClosestPoint(p));
    }

    private void UpdateAudio()
    {
        if (scrape == null) return;
        bool moving = Time.time - lastMoveTime < 0.12f;
        scrape.volume = Mathf.MoveTowards(scrape.volume, moving ? scrapeVolume : 0f, Time.unscaledDeltaTime * 8f);
        if (moving && !scrape.isPlaying) scrape.Play();
        else if (!moving && scrape.volume <= 0.001f && scrape.isPlaying) scrape.Pause();
    }

    // ───────────── Reset (por si querés una opción de "reacomodar") ─────────────

    public void ResetToStart()
    {
        Release();
        transform.SetPositionAndRotation(startPosition, startRotation);
        Physics.SyncTransforms();
    }

    public static void ResetAll()
    {
        foreach (var table in All.ToArray()) table.ResetToStart();
    }
}