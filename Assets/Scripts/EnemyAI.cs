using UnityEngine;

public enum AIState { Patrolling, Chasing }

[RequireComponent(typeof(Rigidbody))]
public class EnemyAI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform visualModel; // <--- Arrastra aquí el objeto hijo (el pez/armature)

    [Header("Corrección Visual")]
    [SerializeField] private Vector3 visualRotationOffset = new Vector3(0, -90, 0); // Ajusta esto si tu modelo apunta de lado

    [Header("Detección Visión 3D")]
    [SerializeField] private float detectionRadius = 15f;    // Distancia máxima de visión
    [SerializeField] private float fieldOfViewAngle = 110f;  // Ángulo cónico de visión
    [SerializeField] private LayerMask obstacleMask;         // Capas que bloquean la vista (Terreno, rocas)

    [Header("Sigilo (luz del jugador)")]
    [Tooltip("Con la luz apagada solo te detecta si estás así de cerca (en cualquier dirección).")]
    [SerializeField] private float darkDetectionRadius = 3f;
    [Tooltip("Segundos que sigue buscando en tu última posición conocida tras perderte.")]
    [SerializeField] private float loseTrackTime = 2f;
    [Tooltip("Si lo tocas sin que te haya visto, este tiempo de gracia antes de que su contacto te mate.")]
    [SerializeField] private float alertGraceTime = 1f;

    [Header("Movimiento y Persecución")]
    [SerializeField] private float patrolSpeed = 4f;
    [SerializeField] private float chaseSpeed = 8f;
    [SerializeField] private float rotationSpeed = 3f;

    [Header("Puntos de Patrulla")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointTolerance = 2f;

    private Rigidbody rb;
    private AIState currentState = AIState.Patrolling;
    private int currentWaypointIndex = 0;

    private PlayerMovment playerMovement;
    private bool canSeePlayer;
    private Vector3 lastKnownPosition;
    private float lostTimer;
    private float alertTime = -Mathf.Infinity;

    // Solo mata al jugador si ya lo está persiguiendo (y pasó el tiempo de gracia tras ser alertado por contacto)
    public bool CanKillPlayer =>
        currentState == AIState.Chasing && Time.time - alertTime >= alertGraceTime;

    // Llamado cuando el jugador lo toca sin que lo haya visto: se gira y empieza a perseguirlo
    public void Alert(Vector3 playerPosition)
    {
        if (currentState == AIState.Chasing) return;

        currentState = AIState.Chasing;
        lastKnownPosition = playerPosition;
        lostTimer = loseTrackTime;
        alertTime = Time.time;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        // Si no se asignó manualmente, buscar al jugador por Tag
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null) playerMovement = player.GetComponentInParent<PlayerMovment>();
        // Si no asignaste el modelo visual manualmente, intenta buscarlo en los hijos
        if (visualModel == null && transform.childCount > 0)
        {
            visualModel = transform.GetChild(0);
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // Comprobar detección de visión continuamente
        canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            currentState = AIState.Chasing;
            lastKnownPosition = player.position;
            lostTimer = loseTrackTime;
        }
        else if (currentState == AIState.Chasing)
        {
            // Te perdió (p.ej. apagaste la luz): busca un momento donde te vio por última vez
            lostTimer -= Time.fixedDeltaTime;
            if (lostTimer <= 0f) currentState = AIState.Patrolling;
        }

        // Ejecutar estado actual
        switch (currentState)
        {
            case AIState.Patrolling:
                PatrolLogic();
                break;

            case AIState.Chasing:
                ChaseLogic();
                break;
        }
    }

    // --- LÓGICA DE DETECCIÓN (CAMPO DE VISIÓN 3D) ---
    private bool CanSeePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;

        // Con la luz apagada: solo te "siente" si estás muy cerca, en cualquier dirección
        bool lightOn = PlayerLightOn();
        float radius = lightOn ? detectionRadius : darkDetectionRadius;

        // 1. Verificar si está dentro del radio de detección
        if (distanceToPlayer <= radius)
        {
            // 2. Verificar si está dentro del ángulo de visión frontal (solo aplica con luz)
            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            if (!lightOn || angle <= fieldOfViewAngle / 2f)
            {
                // 3. Raycast para comprobar si hay obstáculos intermedios (rocas, terreno)
                if (!Physics.Raycast(transform.position, directionToPlayer.normalized, distanceToPlayer, obstacleMask))
                {
                    return true; // El enemigo ve directamente al jugador
                }
            }
        }

        return false;
    }

    // La luz está encendida si el objeto "light" del jugador está activo
    private bool PlayerLightOn()
    {
        if (playerMovement == null || playerMovement.light == null) return true;
        return playerMovement.light.activeInHierarchy;
    }

    // --- LÓGICA DE PATRULLA ---
    private void PatrolLogic()
    {
        if (waypoints.Length == 0) return;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        MoveAndRotateTowards(targetWaypoint.position, patrolSpeed);

        // Si llega al waypoint actual, pasar al siguiente
        float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint.position);
        if (distanceToWaypoint <= waypointTolerance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    // --- LÓGICA DE PERSECUCIÓN ---
    private void ChaseLogic()
    {
        // Si te ve, va directo a ti; si te perdió, va a donde te vio por última vez
        Vector3 target = canSeePlayer ? player.position : lastKnownPosition;
        MoveAndRotateTowards(target, chaseSpeed);
    }

    // --- NAVEGACIÓN Y ROTACIÓN SUAVE EN 3D ---
    private void MoveAndRotateTowards(Vector3 targetPosition, float speed)
    {
        Vector3 moveDirection = (targetPosition - transform.position).normalized;

        if (moveDirection != Vector3.zero)
        {
            // Rotación progresiva hacia el objetivo
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed));
            if (visualModel != null)
            {
                Quaternion correctedRotation = targetRotation * Quaternion.Euler(visualRotationOffset);
                visualModel.rotation = Quaternion.Slerp(visualModel.rotation, correctedRotation, Time.fixedDeltaTime * (rotationSpeed * 2f));
            }
        }

        // Aplicación de velocidad física
        rb.linearVelocity = transform.forward * speed;
    }

    // --- GIZMOS DE DEPURACIÓN EN LA VISTA DE ESCENA ---
    private void OnDrawGizmosSelected()
    {
        // Dibujar el radio de visión
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Radio de detección con la luz apagada
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, darkDetectionRadius);

        // Dibujar los límites del cono de visión
        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle / 2, transform.up) * transform.forward;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle / 2, transform.up) * transform.forward;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1 * detectionRadius);
        Gizmos.DrawRay(transform.position, fovLine2 * detectionRadius);

        if (player != null && CanSeePlayer())
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}