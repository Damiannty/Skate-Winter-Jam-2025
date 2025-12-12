using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("--- REFERENCIAS ---")]
    [Tooltip("Arrastra aquí el objeto visual que rota (si tienes), o déjalo vacío.")]
    [SerializeField] private Transform _skateTransform; 

    [Header("--- MOVIMIENTO HORIZONTAL ---")]
    public float maxSpeed = 12f;
    public float acceleration = 60f; 
    public float deceleration = 40f; 
    public float turnSpeed = 80f;    
    public float airControlMult = 0.8f; 

    [Header("--- SALTO & GRAVEDAD ---")]
    public float jumpForce = 20f;
    public float jumpCutMultiplier = 0.5f; 
    public float gravityScale = 5f;        
    public float fallGravityMult = 1.5f;   

    [Header("--- AYUDAS (GAME FEEL) ---")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("--- DETECCIÓN ---")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f); 
    public LayerMask groundLayer;

    // --- VARIABLES INTERNAS ---
    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    
    // Timers
    private float coyoteCounter;
    private float bufferCounter;
    private float knockbackCounter; // Tiempo que estamos aturdidos por un golpe

    // Visuales
    private bool isFacingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Fallback de seguridad: si no asignaste skateTransform, usa el propio transform
        if (_skateTransform == null) _skateTransform = transform;
    }

    private void Start()
    {
        // CONFIGURACIÓN DE SEGURIDAD (Ignora lo que ponga en el Inspector)
        rb.gravityScale = gravityScale;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // IMPORTANTE: Descongelamos la rotación Z para que el estabilizador funcione,
        // PERO si no usas el script AirStabilizer, deberías congelarla aquí:
        // rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
    }

    private void Update()
    {
        // 1. SISTEMA DE KNOCKBACK (Si nos golpean, ignoramos inputs)
        if (knockbackCounter > 0)
        {
            knockbackCounter -= Time.deltaTime;
            return; // Salimos de la función, no leemos teclas
        }

        // 2. INPUT DE MOVIMIENTO
        moveInput = Input.GetAxisRaw("Horizontal");

        // 3. BUFFER DE SALTO (Recordar tecla pulsada)
        if (Input.GetButtonDown("Jump"))
        {
            bufferCounter = jumpBufferTime;
        }
        else
        {
            bufferCounter -= Time.deltaTime;
        }

        // 4. SALTO VARIABLE (Soltar botón)
        // Usamos rb.velocity (compatible) en lugar de linearVelocity
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            coyoteCounter = 0f; 
        }

        // 5. VOLTEAR SPRITE
        if (moveInput > 0 && !isFacingRight) Flip();
        else if (moveInput < 0 && isFacingRight) Flip();
    }

    private void FixedUpdate()
    {
        // 1. DETECCIÓN DE SUELO
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        // 2. GESTIÓN DE KNOCKBACK EN FÍSICAS
        if (knockbackCounter > 0)
        {
            // Si estamos aturdidos, solo aplicamos gravedad y salimos.
            // NO aplicamos movimiento horizontal voluntario.
            ApplyGravity();
            return; 
        }

        // 3. COYOTE TIME
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // 4. APLICAR LÓGICA PRINCIPAL
        ApplyMovement();
        ApplyGravity();
        CheckJump();
    }

    private void ApplyMovement()
    {
        // --- MODIFICACIÓN PARA RESPETAR INERCIA ---
    
        // Si vamos MÁS RÁPIDO que la velocidad máxima (porque caímos en una rampa),
        // y no estamos intentando frenar (el input va en la misma dirección),
        // entonces NO aplicamos fuerza y dejamos que la inercia actúe sola.
        
        bool movingFast = Mathf.Abs(rb.linearVelocity.x) > maxSpeed;
        bool inputAlignsWithSpeed = Mathf.Sign(moveInput) == Mathf.Sign(rb.linearVelocity.x);

        if (movingFast && inputAlignsWithSpeed && moveInput != 0)
        {
            // Estamos super rápidos y el jugador quiere seguir en esa dirección.
            // No hacemos nada (no limitamos velocidad), dejamos que la física de la rampa mande.
            return; 
        }
        
        // Calcular velocidad deseada
        float targetSpeed = moveInput * maxSpeed;
        
        // Calcular la fuerza necesaria (Diferencia entre actual y deseada)
        float speedDif = targetSpeed - rb.linearVelocity.x;

        // Elegir aceleración (Normal, Frenado o Giro Rápido)
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        if (Mathf.Abs(targetSpeed) > 0.01f && Mathf.Sign(targetSpeed) != Mathf.Sign(rb.linearVelocity.x))
        {
            accelRate = turnSpeed;
        }

        // Menos control en el aire
        if (!isGrounded)
        {
            accelRate *= airControlMult;
        }

        // Aplicar fuerza
        float movement = speedDif * accelRate;

        // Si el objeto rota (por las rampas), aplicamos la fuerza en su dirección local "Right"
        // Si no rota, _skateTransform.right será igual a Vector3.right (Global)
        rb.AddForce(movement * _skateTransform.right.normalized);
    }

    private void ApplyGravity()
    {
        // Caída rápida estilo Celeste
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = gravityScale * fallGravityMult;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }
    }

    private void CheckJump()
    {
        // Para saltar necesitamos: Buffer (tecla reciente) Y Coyote (suelo reciente)
        if (bufferCounter > 0f && coyoteCounter > 0f)
        {
            // Salto por velocidad directa (Más preciso y snappy)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // Resetear contadores
            bufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    // --- FUNCIÓN PÚBLICA PARA EL BARRIL ---
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        knockbackCounter = duration; // Bloqueamos controles
        rb.linearVelocity = Vector2.zero;  // Paramos en seco
        rb.linearVelocity = direction * force; // Lanzamos
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            // Verde si toca suelo, Rojo si no
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}