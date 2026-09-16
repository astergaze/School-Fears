using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public InputActionAsset InputActions;
    public CharacterController playerCharacterController;

    private InputAction playerMoveAction;
    private InputAction playerInteractAction;
    private InputAction playerInventoryAction;

    [SerializeField] public Transform playerCamera;

    private Vector2 playerMoveAmount;

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

    private void OnEnable() => InputActions.FindActionMap("Player").Enable();
    private void OnDisable() => InputActions.FindActionMap("Player").Disable();

    private void Awake()
    {
        Instance = this;
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
        playerMoveAmount = playerMoveAction.ReadValue<Vector2>();
        PlayerMoveAndRotate();
        CheckForInteractable();

        if (playerInteractAction.WasPressedThisFrame())
            PlayerInteract();
        if (playerInventoryAction.WasPressedThisFrame())
            InventoryUI.Instance.Toggle();
    }

    private void CheckForInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactableLayer);

        Interactable best = null;
        float bestDot = -1f;

        foreach (Collider col in hits)
        {
            Interactable interactable = col.GetComponent<Interactable>();
            if (interactable == null) continue;

            // Direccion desde el player hacia el objeto
            Vector3 dirToObject = (col.transform.position - transform.position).normalized;
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
        currentInteractable?.Interact(this);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}