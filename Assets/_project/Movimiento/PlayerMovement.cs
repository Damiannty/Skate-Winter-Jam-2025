using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("--- REFERENCIAS ---")]
    [SerializeField] private Transform _skateTransform; 

    [Header("--- MOVIMIENTO SUELO ---")]
    public float maxSpeed = 12f;
    public float acceleration = 60f; 
    public float braking = 40f; 
    public float friction = 5f; 

    [Header("--- MOVIMIENTO PICADO ---")]
    [Tooltip("Fuerza del impulso al picar.")]
    public float diveAcceleration = 100f; 
    
    [Tooltip("Ángulo mínimo para activar el picado (Ej: 30 grados).")]
    public float diveAngleThreshold = 30f;

    [Header("--- SALTO & GRAVEDAD ---")]
    public float jumpForce = 20f;
    public float jumpCutMultiplier = 0.5f; 
    public float gravityScale = 5f;        
    public float fallGravityMult = 1.5f;

    [Header("--- DETECCIÓN ---")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f); 
    public LayerMask groundLayer;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    // --- VARIABLES ---
    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private float coyoteCounter;
    private float bufferCounter;
    private float knockbackCounter; 
    private bool isFacingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (_skateTransform == null) _skateTransform = transform;
    }

    private void Start()
    {
        rb.gravityScale = gravityScale;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        if (knockbackCounter > 0) { knockbackCounter -= Time.deltaTime; return; }

        moveInput = Input.GetAxisRaw("Horizontal");

        // Saltos
        if (Input.GetButtonDown("Jump")) bufferCounter = jumpBufferTime;
        else bufferCounter -= Time.deltaTime;

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            coyoteCounter = 0f; 
        }

        // Flip visual (Solo en suelo)
        if (isGrounded)
        {
            if (moveInput > 0 && !isFacingRight) Flip();
            else if (moveInput < 0 && isFacingRight) Flip();
        }
    }

    private void FixedUpdate()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if (knockbackCounter > 0) { ApplyGravity(); return; }

        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        ApplyMovement();
        ApplyGravity();
        CheckJump();
    }

    private void ApplyMovement()
    {
        // 1. OBTENER EL VECTOR DE FUERZA REAL
        // Esta función calcula hacia dónde nos moveríamos si aceleramos AHORA mismo
        // basándose en el Input (-1 o 1) y la rotación Z del Rigidbody.
        Vector3 intendedDir = GetInputDirectionVector();

        // ---------------------------------------------------------
        // CASO A: ESTAMOS EN EL AIRE (PICADO)
        // ---------------------------------------------------------
        if (!isGrounded)
        {
            // ¿Estamos intentando ir hacia ABAJO?
            // Si intendedDir.y es muy negativo, significa que la rotación + el input apuntan al suelo.
            // Convertimos el ángulo umbral a Seno (Ej: 30º -> 0.5)
            float verticalThreshold = -Mathf.Sin(diveAngleThreshold * Mathf.Deg2Rad);
            
            bool isDiving = intendedDir.y < verticalThreshold;

            if (isDiving && moveInput != 0)
            {
                // ¡PICADO! Aplicamos la fuerza en esa dirección exacta
                rb.AddForce(intendedDir * diveAcceleration);
                
                // Debug Cyan: Muestra que el picado funciona
                Debug.DrawRay(transform.position, intendedDir * 3, Color.cyan);
            }
            return; // En el aire no hay más control
        }

        // ---------------------------------------------------------
        // CASO B: ESTAMOS EN EL SUELO (NORMAL)
        // ---------------------------------------------------------
        
        // Debug Verde: Muestra hacia dónde te mueves en el suelo
        Debug.DrawRay(transform.position, intendedDir * 2, Color.green);

        float currentSpeedInDir = Vector2.Dot(rb.linearVelocity, intendedDir);
        float targetSpeed = Mathf.Abs(moveInput) * maxSpeed; // Usamos Abs porque intendedDir ya tiene el signo
        float speedDif = targetSpeed - currentSpeedInDir;

        float accelRate = 0;

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            // Si estamos intentando acelerar
            if (currentSpeedInDir < targetSpeed)
            {
                // Inercia Sonic: Si ya vamos volando, no frenamos
                if (currentSpeedInDir > maxSpeed) accelRate = 0;
                else accelRate = acceleration;
            }
            else
            {
                // Si vamos demasiado rápido en esa dirección (y no es por inercia permitida), frenamos suave
                 accelRate = 0; 
            }
        }
        else
        {
            // Fricción al soltar teclas
            accelRate = friction; 
            // Si soltamos teclas, intendedDir es 0, así que recalculamos un vector de frenado opuesto a velocidad
            if(rb.linearVelocity.magnitude > 0.1f)
            {
                intendedDir = -rb.linearVelocity.normalized;
                speedDif = rb.linearVelocity.magnitude;
            }
        }
        
        // CORRECCIÓN FRENADA OPUESTA
        // Si pulsamos dirección contraria a la que vamos
        float projectedVel = Vector2.Dot(rb.linearVelocity, intendedDir);
        if (moveInput != 0 && projectedVel < -0.1f)
        {
            accelRate = braking;
        }

        rb.AddForce(intendedDir * speedDif * accelRate);
    }

    private void ApplyGravity()
    {
        if (rb.linearVelocity.y < 0) rb.gravityScale = gravityScale * fallGravityMult;
        else rb.gravityScale = gravityScale;
    }

    private void CheckJump()
    {
        if (bufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            bufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        knockbackCounter = duration;
        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = direction * force;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // --- LA SOLUCIÓN MATEMÁTICA ---
    // Esta función ignora el Flip visual y calcula el vector basándose solo en Input y Rotación Z
    private Vector3 GetInputDirectionVector()
    {
        if (moveInput == 0) return Vector3.zero;

        // 1. Coger el vector "Derecha" (1, 0)
        Vector3 dir = Vector3.right;

        // 2. Rotarlo por el ángulo Z del Rigidbody
        dir = Quaternion.Euler(0, 0, rb.rotation) * dir;

        // 3. Multiplicarlo por el Input (-1 o 1)
        // Si pulsas Izquierda (-1), el vector se invierte correctamente
        dir *= moveInput;

        return dir;
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}