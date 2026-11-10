using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Agacharse. Va en el Player (persistente). Solo funciona en escenas que lo habiliten
/// (poné un CrouchEnabler en la escena). Al cargar cualquier escena se desactiva solo, así que
/// en el resto del juego el jugador no puede agacharse.
///
/// Qué hace al agacharse: achica el CharacterController (manteniendo los pies en el piso),
/// baja los transforms de "ojos" (la cámara) y baja la velocidad usando Player.SpeedMultiplier.
/// No se puede levantar si hay algo encima (techo, mesa, etc.).
/// </summary>
[DefaultExecutionOrder(50)] // antes que el Cinemachine Brain, para que la cámara ya vea la altura nueva
public class CrouchController : MonoBehaviour
{
    public static CrouchController Instance { get; private set; }

    /// <summary>true cuando el jugador está (casi) completamente agachado. Lo usa el Rector para saber cómo verlo.</summary>
    public static bool IsCrouching => Instance != null && Instance.blend > 0.6f;

    public bool Allowed { get; private set; }

    [Header("Referencias")]
    [Tooltip("Si queda vacío se busca en el mismo objeto / hijos.")]
    [SerializeField] private CharacterController controller;
    [Tooltip("Transforms que definen la altura de los ojos: el CinemachineCamera (o el objeto que sigue) que cuelga del Player. Se bajan 'Eye Drop' metros al agacharse.")]
    [SerializeField] private Transform[] eyeTransforms;

    [Header("Agachado")]
    [Tooltip("Altura del CharacterController agachado (parado suele ser ~1.8).")]
    [SerializeField] private float crouchHeight = 1.0f;
    [Tooltip("Cuántos metros baja la cámara al agacharse. Los ojos agachados tienen que quedar POR DEBAJO del borde de las mesas.")]
    [SerializeField] private float eyeDrop = 0.8f;
    [Tooltip("Qué tan rápido pasa de parado a agachado (1 / segundos).")]
    [SerializeField] private float transitionSpeed = 5f;
    [Tooltip("Multiplica la velocidad del jugador mientras está agachado.")]
    [SerializeField, Range(0.2f, 1f)] private float crouchSpeedMultiplier = 0.45f;

    [Header("Input")]
    [Tooltip("Opcional: una acción 'Crouch' de tu Input Actions (así funciona con el rebind). Si queda vacío se usan las teclas de abajo.")]
    [SerializeField] private InputActionReference crouchAction;
    [SerializeField] private Key hotkey = Key.LeftCtrl;
    [SerializeField] private Key altHotkey = Key.C;
    [Tooltip("false = mantener para estar agachado. true = apretar una vez para agacharse y otra para levantarse.")]
    [SerializeField] private bool toggleMode = false;

    private float standHeight;
    private Vector3 standCenter;
    private Vector3[] baseEyeLocal;
    private float blend;           // 0 = parado, 1 = agachado
    private float appliedBlend = -1f;
    private bool wantCrouch;
    private bool startOverride;    // arranca agachado hasta que el jugador toque la tecla
    private bool controllingSpeed;
    private readonly Collider[] overlapBuffer = new Collider[8];

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (controller == null) controller = GetComponentInParent<CharacterController>();
        if (controller == null) controller = GetComponentInChildren<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("CrouchController: no encontré un CharacterController. Asignalo en el Inspector.");
            enabled = false;
            return;
        }

        standHeight = controller.height;
        standCenter = controller.center;
        crouchHeight = Mathf.Clamp(crouchHeight, controller.radius * 2f + 0.01f, standHeight);

        baseEyeLocal = new Vector3[eyeTransforms != null ? eyeTransforms.Length : 0];
        for (int i = 0; i < baseEyeLocal.Length; i++)
            if (eyeTransforms[i] != null) baseEyeLocal[i] = eyeTransforms[i].localPosition;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (crouchAction != null && crouchAction.action != null) crouchAction.action.Enable();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Por defecto en cada escena NO se puede agachar; el CrouchEnabler de la escena lo habilita en su Start
        Allowed = false;
        wantCrouch = false;
        startOverride = false;
        ForceStand();
    }

    // ================= API =================

    public void SetAllowed(bool allowed)
    {
        Allowed = allowed;
        if (!allowed) { wantCrouch = false; startOverride = false; }
    }

    /// <summary>Arranca agachado (ej: Alex escondido detrás del mostrador) hasta que el jugador toque la tecla.</summary>
    public void StartCrouched()
    {
        if (!Allowed) return;
        startOverride = true;
        blend = 1f;   // sin transición: ya está agachado al empezar
    }

    private void ForceStand()
    {
        blend = 0f;
        Apply();
        ReleaseSpeed();
    }

    // ================= Update =================

    private void Update()
    {
        bool dialogue = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
        bool busy = dialogue || SceneTransitioner.IsTransitioning || Time.timeScale <= 0f;

        if (Allowed && !busy) ReadInput();

        bool shouldCrouch = Allowed && (wantCrouch || startOverride);

        // No levantarse si hay algo encima
        if (!shouldCrouch && blend > 0f && !CanStandUp()) shouldCrouch = true;

        blend = Mathf.MoveTowards(blend, shouldCrouch ? 1f : 0f, transitionSpeed * Time.deltaTime);

        Apply();
        ApplySpeed();
    }

    private void ReadInput()
    {
        bool pressed;
        bool held;

        if (crouchAction != null && crouchAction.action != null)
        {
            pressed = crouchAction.action.WasPressedThisFrame();
            held = crouchAction.action.IsPressed();
        }
        else
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            pressed = kb[hotkey].wasPressedThisFrame || kb[altHotkey].wasPressedThisFrame;
            held = kb[hotkey].isPressed || kb[altHotkey].isPressed;
        }

        if (toggleMode)
        {
            if (pressed)
            {
                if (startOverride) { startOverride = false; wantCrouch = false; }
                else wantCrouch = !wantCrouch;
            }
        }
        else
        {
            if (pressed) startOverride = false;
            wantCrouch = held;
        }
    }

    private bool CanStandUp()
    {
        float radius = controller.radius * 0.95f;
        float currentHeight = Mathf.Lerp(standHeight, crouchHeight, Mathf.SmoothStep(0f, 1f, blend));
        float feetLocalY = standCenter.y - standHeight * 0.5f;
        Transform t = controller.transform;

        Vector3 feet = t.TransformPoint(new Vector3(standCenter.x, feetLocalY, standCenter.z));
        Vector3 bottom = feet + Vector3.up * Mathf.Max(radius, currentHeight - radius);
        Vector3 top = feet + Vector3.up * Mathf.Max(radius, standHeight - radius);

        int n = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            Collider c = overlapBuffer[i];
            if (c == null || c.transform.IsChildOf(t)) continue;
            return false;
        }
        return true;
    }

    private void Apply()
    {
        bool changed = !Mathf.Approximately(appliedBlend, blend);
        float s = Mathf.SmoothStep(0f, 1f, blend);

        if (changed)
        {
            float h = Mathf.Lerp(standHeight, crouchHeight, s);
            float feetY = standCenter.y - standHeight * 0.5f;
            controller.height = h;
            controller.center = new Vector3(standCenter.x, feetY + h * 0.5f, standCenter.z);
        }

        // Los ojos se reescriben todo el rato mientras estén agachados, por si otro script los mueve
        if (changed || blend > 0f)
        {
            for (int i = 0; i < baseEyeLocal.Length; i++)
            {
                if (eyeTransforms[i] == null) continue;
                Vector3 p = baseEyeLocal[i];
                p.y -= eyeDrop * s;
                eyeTransforms[i].localPosition = p;
            }
        }
        appliedBlend = blend;
    }

    private void ApplySpeed()
    {
        var p = Player.Instance;
        if (p == null) return;

        if (blend > 0f)
        {
            p.SpeedMultiplier = Mathf.Lerp(1f, crouchSpeedMultiplier, blend);
            controllingSpeed = true;
        }
        else ReleaseSpeed();
    }

    private void ReleaseSpeed()
    {
        if (!controllingSpeed) return;
        if (Player.Instance != null) Player.Instance.SpeedMultiplier = 1f;
        controllingSpeed = false;
    }
}
