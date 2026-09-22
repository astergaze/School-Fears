using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public InputActionAsset InputActions;
    public CharacterController playerCharacterController;

    private InputAction playerMoveAction;
    private InputAction playerInteractAction;
    private InputAction playerInventoryAction;

    [SerializeField] public Transform playerCamera;

    private Vector2 playerMoveAmount;
    private Vector3 InteractOrigin => transform.TransformPoint(playerCharacterController.center);

    private float playerWalkSpeed = 5.0f;
    private float playerRotateDampening = 0.1f;
    private float turnSmoothingVelocity;
    private float verticalVelocity = 0f;
    private float gravity = -9.81f;

    [Header("Interaction")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactableLayer;

    private Interactable currentInteractable;
    public Inventory Inventory { get; private set; }
    public static Player Instance { get; private set; }

    private void OnEnable()
    {
        if (Instance != this) return;
        InputActions.FindActionMap("Player").Enable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        InputActions.FindActionMap("Player").Disable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentInteractable = null;
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Inventory = GetComponent<Inventory>();
        var playerMap = InputActions.FindActionMap("Player");
        playerMoveAction     = playerMap.FindAction("Move");
        playerInteractAction = playerMap.FindAction("Interact");
        playerInventoryAction = playerMap.FindAction("Inventory");
        // playerMoveAction = InputSystem.actions.FindAction("Move");
        // playerInteractAction = InputSystem.actions.FindAction("Interact");
        // playerInventoryAction = InputSystem.actions.FindAction("Inventory");
    }

    private void Update()
    {
        bool dialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;

        if (!dialogueOpen)
        {
            playerMoveAmount = playerMoveAction.ReadValue<Vector2>();
            PlayerMoveAndRotate();
        }
        else
        {
            playerMoveAmount = Vector2.zero; // por si quedó input residual del frame en que se abrió el diálogo
        }

        CheckForInteractable();

        if (!dialogueOpen && playerInteractAction.WasPressedThisFrame())
            PlayerInteract();
        if (playerInventoryAction.WasPressedThisFrame())
            InventoryUI.Instance.Toggle();
    }

    private void CheckForInteractable()
    {
        Vector3 origin = InteractOrigin;
        Collider[] hits = Physics.OverlapSphere(origin, interactRange, interactableLayer);

        Interactable best = null;
        float bestDot = -1f;

        foreach (Collider col in hits)
        {
            Interactable interactable = col.GetComponent<Interactable>();
            if (interactable == null) continue;

            // Direccion desde el player hacia el objeto
            Vector3 dirToObject = (col.bounds.center - origin).normalized;
            // Cuanto esta alineado con hacia donde mira la camara
            float dot = Vector3.Dot(playerCamera.forward, dirToObject);

            if (dot > bestDot)
            {
                bestDot = dot;
                best = interactable;
            }
        }

        if (best != currentInteractable)
        {
            currentInteractable?.OnLoseFocus();
            currentInteractable = best;
            currentInteractable?.OnFocus();
        }
    }

    private void PlayerInteract()
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact(this);
        }
        else
        {
            currentInteractable = null;
        }
    }

    /// <summary>
    /// Reposiciona al player de forma segura, desactivando el CharacterController
    /// un instante para evitar glitches de colisión al mover el transform directo.
    /// Usar esto en vez de setear transform.position/rotation a mano.
    /// </summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        if (playerCharacterController != null)
            playerCharacterController.enabled = true;
    }

    private void PlayerMoveAndRotate()
    {
        // 1. Obtenemos las direcciones forward y right de la cámara proyectadas en el plano XZ (sin inclinación vertical)
        Vector3 camForward = playerCamera.forward;
        Vector3 camRight = playerCamera.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // --- CÁLCULO DE GRAVEDAD ---
        // Si el jugador toca el suelo, reiniciamos la velocidad vertical
        // Usamos -2f en lugar de 0f para que se mantenga bien pegado al piso en las bajadas
        if (playerCharacterController.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        // Aumentamos la velocidad de caída con el paso del tiempo
        verticalVelocity += gravity * Time.deltaTime;
        // ---------------------------

        // 2. Calculamos el vector de movimiento relativo a la cámara
        Vector3 moveDirection = (camRight * playerMoveAmount.x + camForward * playerMoveAmount.y);

        // verticalVelocity ahora tiene la fuerza de la gravedad acumulada
        Vector3 verticalMove = new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime;

        // 3. Movemos el CharacterController
        playerCharacterController.Move(moveDirection * playerWalkSpeed * Time.deltaTime + verticalMove);

        // 4. Si quieres que el cuerpo del player rote automáticamente con la vista horizontal:
        if (camForward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(camForward);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = playerCharacterController != null ? transform.TransformPoint(playerCharacterController.center) : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}