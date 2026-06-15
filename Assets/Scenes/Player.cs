using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public InputActionAsset InputActions;
    public CharacterController playerCharacterController;

    private InputAction playerMoveAction;
    private InputAction playerInteractAction;

    [SerializeField] public Transform playerCamera;

    private Vector2 playerMoveAmount;

    private float playerWalkSpeed = 5.0f;
    private float playerRotateDampening = 0.1f;
    private float turnSmoothingVelocity;
    private float verticalVelocity = 0f;

    [Header("Interaction")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactableLayer;

    private Interactable currentInteractable;

    private void OnEnable() => InputActions.FindActionMap("Player").Enable();
    private void OnDisable() => InputActions.FindActionMap("Player").Disable();

    private void Awake()
    {
        playerMoveAction = InputSystem.actions.FindAction("Move");
        playerInteractAction = InputSystem.actions.FindAction("Interact");
    }

    private void Update()
    {
        playerMoveAmount = playerMoveAction.ReadValue<Vector2>();
        PlayerMoveAndRotate();
        CheckForInteractable();

        if (playerInteractAction.WasPressedThisFrame())
            PlayerInteract();
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
        Vector3 playerDirection = new Vector3(playerMoveAmount.x, 0f, playerMoveAmount.y).normalized;
        Vector3 verticalMove = new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime;

        if (playerDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(playerDirection.x, playerDirection.z) * Mathf.Rad2Deg + playerCamera.eulerAngles.y;
            float smoothTargetAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothingVelocity, playerRotateDampening);

            transform.rotation = Quaternion.Euler(0f, smoothTargetAngle, 0f);

            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            playerCharacterController.Move(moveDirection.normalized * playerWalkSpeed * Time.deltaTime + verticalMove);
        }
        else
        {
            playerCharacterController.Move(verticalMove);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}