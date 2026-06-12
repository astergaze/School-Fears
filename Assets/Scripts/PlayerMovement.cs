using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 10f;
    public float rotationSpeed = 10f;

    [Header("Sprint")]
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Reference Nodes")]
    public Transform orientation;   // Arrastra el hijo "Orientation" aquí
    public Transform playerObj;     // Arrastra el hijo "PlayerObj" aquí

    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private Rigidbody rb;
    private bool isSprinting;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput   = Input.GetAxisRaw("Vertical");
        isSprinting     = Input.GetKey(sprintKey);

        // Dirección relativa a Orientation, ignorando el eje Y
        moveDirection = orientation.forward * verticalInput
                      + orientation.right   * horizontalInput;
        moveDirection.y = 0f;

        // Rota el mesh (PlayerObj) hacia donde se mueve
        if (moveDirection != Vector3.zero)
        {
            playerObj.forward = Vector3.Slerp(
                playerObj.forward,
                moveDirection.normalized,
                Time.deltaTime * rotationSpeed
            );
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        Vector3 targetVelocity = moveDirection.normalized * currentSpeed;

        // Preserva la velocidad Y del Rigidbody para que la gravedad funcione
        rb.linearVelocity = new Vector3(
            targetVelocity.x,
            rb.linearVelocity.y,
            targetVelocity.z
        );
    }
}