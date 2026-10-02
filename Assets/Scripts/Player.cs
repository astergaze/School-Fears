using System.Collections;
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

    private bool IsDialogueOpen => DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;

    public Inventory Inventory { get; private set; }
    public static Player Instance { get; private set; }

    private void OnEnable()
    {
        if (Instance != this) return;
        InputActions.FindActionMap("Player").Enable();
        SceneManager.sceneLoaded += OnSceneLoaded;
        LockCursor();
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        InputActions.FindActionMap("Player").Disable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnlockCursor();
    }

    private void Start()
    {
        LockCursor();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentInteractable = null;
        InteractionPromptUI.Instance?.HidePrompt();
        Player.LockCursor();
        StartCoroutine(EnsureCursorLockedNextFrame());
    }

    private IEnumerator EnsureCursorLockedNextFrame()
    {
        yield return null;
        LockCursor();
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
    }

    private void Update()
    {
        bool dialogueOpen = IsDialogueOpen;
        bool transitioning = SceneTransitioner.IsTransitioning;

        if (!dialogueOpen && !transitioning)
        {
            playerMoveAmount = playerMoveAction.ReadValue<Vector2>();
            PlayerMoveAndRotate();
        }
        else
        {
            playerMoveAmount = Vector2.zero;
        }

        CheckForInteractable();

        // Si hay diálogo o transición, el prompt se mantiene apagado
        if (dialogueOpen || transitioning)
        {
            InteractionPromptUI.Instance?.HidePrompt();
        }
        else if (currentInteractable != null)
        {
            InteractionPromptUI.Instance?.ShowPrompt(currentInteractable.GetPromptText());
        }

        if (!dialogueOpen && !transitioning && playerInteractAction.WasPressedThisFrame())
            PlayerInteract();

        if (!transitioning && playerInventoryAction.WasPressedThisFrame())
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

            Vector3 dirToObject = (col.bounds.center - origin).normalized;
            float dot = Vector3.Dot(playerCamera.forward, dirToObject);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = interactable;
            }
        }

        // Cambio de foco entre un objeto y otro (o perder el foco por completo)
        if (best != currentInteractable)
        {
            if (currentInteractable != null)
            {
                currentInteractable.OnLoseFocus();
                InteractionPromptUI.Instance?.HidePrompt();
            }

            currentInteractable = best;

            if (currentInteractable != null)
            {
                currentInteractable.OnFocus();
            }
        }
    }

    private void PlayerInteract()
    {
        if (currentInteractable != null)
        {
            InteractionPromptUI.Instance?.HidePrompt();
            currentInteractable.Interact(this);
        }
        else
        {
            currentInteractable = null;
        }
    }

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
        Vector3 camForward = playerCamera.forward;
        Vector3 camRight = playerCamera.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        if (playerCharacterController.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 moveDirection = (camRight * playerMoveAmount.x + camForward * playerMoveAmount.y);
        Vector3 verticalMove = new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime;

        playerCharacterController.Move(moveDirection * playerWalkSpeed * Time.deltaTime + verticalMove);

        if (camForward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(camForward);
        }
    }

    public static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = playerCharacterController != null ? transform.TransformPoint(playerCharacterController.center) : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, interactRange);
    }
}