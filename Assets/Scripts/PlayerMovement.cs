using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Variables")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    [Header("reference node")]
    public Transform orientation; // Tu nodo vacío
    public Transform playerObj;   // Tu modelo 3D
    public Transform cameraTransform; // La Main Camera

    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // El script controla la rotación, no la física.
        
        // Ocultar y bloquear el cursor para inmersión total.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // 1. Recolectar el input del usuario.
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // 2. Calcular la dirección de la cámara, ignorando la elevación (Y).
        Vector3 viewDir = transform.position - new Vector3(cameraTransform.position.x, transform.position.y, cameraTransform.position.z);
        orientation.forward = viewDir.normalized;

        // 3. Calcular el vector de movimiento final basado en la orientación.
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // 4. Rotar el modelo 3D para que mire hacia donde camina.
        if (moveDirection != Vector3.zero)
        {
            playerObj.forward = Vector3.Slerp(playerObj.forward, moveDirection.normalized, Time.deltaTime * rotationSpeed);
        }
    }

    private void FixedUpdate()
    {
        // 5. Aplicar la fuerza al mundo físico.
        MovePlayer();
    }

    private void MovePlayer()
    {
        // Calculamos la velocidad objetivo manteniendo la gravedad actual.
        Vector3 targetVelocity = moveDirection.normalized * moveSpeed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }
}