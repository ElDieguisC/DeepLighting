using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovment : MonoBehaviour
{
    [Header("Movimiento Hidrodinámico")]
    [SerializeField] private float swimForce = 25f;
    [SerializeField] private float verticalForce = 20f;
    [SerializeField] private float maxSpeed = 7f;

    [Header("Control de Cámara / Rotación")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float rotationSmoothness = 10f;

    private Rigidbody rb;
    private float yaw;
    private float pitch;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Bloquear y ocultar el cursor para control en primera/tercera persona
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Lectura de entrada de ratón para apuntar/rotar
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -75f, 75f); // Limitar el ángulo vertical
    }

    void FixedUpdate()
    {
        // 1. Orientación del pez según la vista
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSmoothness));

        // 2. Lectura de ejes (WASD / Stick)
        float moveForward = Input.GetAxisRaw("Vertical");    // W/S
        float moveStrafe = Input.GetAxisRaw("Horizontal");   // A/D

        // Controles de elevación (Espacio = Subir, Shift Izq = Bajar)
        float moveVertical = 0f;
        if (Input.GetKey(KeyCode.Space)) moveVertical = 1f;
        if (Input.GetKey(KeyCode.LeftShift)) moveVertical = -1f;

        // 3. Cálculo de direcciones de empuje relativas a la mirada
        Vector3 forceDirection = (transform.forward * moveForward + transform.right * moveStrafe).normalized;
        Vector3 verticalDirection = Vector3.up * moveVertical;

        // 4. Aplicación de fuerzas de nado
        if (forceDirection.magnitude > 0.1f)
        {
            rb.AddForce(forceDirection * swimForce, ForceMode.Acceleration);
        }

        if (moveVertical != 0f)
        {
            rb.AddForce(verticalDirection * verticalForce, ForceMode.Acceleration);
        }

        // 5. Límite de velocidad máxima bajo el agua
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
}