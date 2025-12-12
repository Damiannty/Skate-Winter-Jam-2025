using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlopeMomentum : MonoBehaviour
{
    [Header("--- DEBUG ---")]
    public SpriteRenderer visualDebugger; // Verde = Suelo, Rojo = Aire

    [Header("--- CONTROLES AÉREOS ---")]
    public KeyCode tiltLeftKey = KeyCode.Q;
    public KeyCode tiltRightKey = KeyCode.E;
    public float airRotationSpeed = 200f;
    public float autoStabilizeSpeed = 10f;

    [Tooltip("Ángulo máximo de inclinación permitido. Evita que des la vuelta completa y te estampes. (Ej: 60 grados)")]
    public float maxRotationAngle = 60f;

    [Tooltip("Altura mínima para permitir rotar.")]
    public float safeRotationHeight = 1.2f;

    [Header("--- ESTABILIDAD EN RAMPAS ---")]
    [Tooltip("Fuerza extra hacia abajo cuando estás en el suelo para evitar salir volando en las bajadas.")]
    public float slopeStickForce = 50f; 

    [Header("--- FÍSICA SONIC ---")]
    public float momentumPreservation = 1f;
    public float minFallSpeed = 5f;

    [Header("--- DETECCIÓN ---")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public Vector2 groundBoxSize = new Vector2(0.8f, 0.2f);
    public float groundAlignSpeed = 20f; 

    private Rigidbody2D rb;
    private bool isGrounded;
    private float rotationInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Update()
    {
        // DEBUG VISUAL
        if (visualDebugger != null) visualDebugger.color = isGrounded ? Color.green : Color.red;

        // INPUT (Solo si no estamos en suelo)
        if (!isGrounded)
        {
            rotationInput = 0;
            if (Input.GetKey(tiltLeftKey)) rotationInput = 1f;
            if (Input.GetKey(tiltRightKey)) rotationInput = -1f;
        }
        else
        {
            rotationInput = 0;
        }
    }

    private void FixedUpdate()
    {
        // 1. DETECCIÓN DE SUELO
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundBoxSize, 0f, groundLayer);
        }

        // 2. MATAR ROTACIÓN FÍSICA SIEMPRE
        rb.angularVelocity = 0f;

        if (isGrounded)
        {
            // --- EN EL SUELO ---
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Bloqueo físico
            AlignToSlopeAndStick(); // Alinear y PEGAR al suelo
        }
        else
        {
            // --- EN EL AIRE ---
            rb.constraints = RigidbodyConstraints2D.None; // Desbloqueo para rotar
            HandleAirRotation();
        }
    }

    private void HandleAirRotation()
    {
        bool tooClose = Physics2D.Raycast(transform.position, Vector2.down, safeRotationHeight, groundLayer);

        // Calculamos la rotación deseada
        float currentAngle = rb.rotation;
        
        // CORRECCIÓN DE ÁNGULOS UNITY (0 a 360 -> -180 a 180 para poder limitar bien)
        if (currentAngle > 180) currentAngle -= 360;

        if (rotationInput != 0 && !tooClose)
        {
            float rotateAmount = rotationInput * airRotationSpeed * Time.fixedDeltaTime;
            float nextAngle = currentAngle + rotateAmount;

            // LIMITADOR (CLAMP): Aquí evitamos que gire más de lo permitido
            nextAngle = Mathf.Clamp(nextAngle, -maxRotationAngle, maxRotationAngle);

            rb.MoveRotation(nextAngle);
        }
        else
        {
            // Auto-Estabilizar hacia 0
            float angle = Mathf.LerpAngle(currentAngle, 0f, autoStabilizeSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(angle);
        }
    }

    private void AlignToSlopeAndStick()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, -transform.up, 2.5f, groundLayer);

        if (hit.collider != null)
        {
            Vector2 normal = hit.normal;
            
            // 1. ALINEACIÓN VISUAL
            float targetSlopeAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;
            float angle = Mathf.LerpAngle(rb.rotation, targetSlopeAngle, groundAlignSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(angle);

            // 2. SLOPE STICK (LA SOLUCIÓN A LOS SALTOS)
            // Aplicamos una fuerza hacia abajo PERPENDICULAR a la rampa (opuesto a la normal)
            // Esto "pega" al jugador a la rampa mientras baja rápido.
            Vector2 stickDirection = -normal;
            rb.AddForce(stickDirection * slopeStickForce, ForceMode2D.Force);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Física Sonic (convertir caída en velocidad)
        // CAMBIA linearVelocity A velocity SI USAS UNITY ANTIGUO
        if (rb.linearVelocity.y < -minFallSpeed) 
        {
            ConvertFallToSpeed(collision);
        }
    }

    private void ConvertFallToSpeed(Collision2D collision)
    {
        ContactPoint2D contact = collision.contacts[0];
        Vector2 normal = contact.normal;
        if (normal.y > 0.95f) return;

        Vector2 surfaceDir = new Vector2(normal.y, -normal.x);
        Vector2 impactVelocity = -collision.relativeVelocity; // Invertimos para obtener vector real
        
        float projectedSpeed = Vector2.Dot(impactVelocity, surfaceDir);
        Vector2 finalVelocity = surfaceDir * projectedSpeed * momentumPreservation;

        if (finalVelocity.magnitude > rb.linearVelocity.magnitude)
        {
            rb.linearVelocity = finalVelocity;
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundBoxSize);
        }
    }
}