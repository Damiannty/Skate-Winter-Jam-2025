using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("--- CONFIGURACIÓN TRUCOS ---")]
    [Tooltip("Tiempo máximo entre teclas (Q->E->Espacio).")]
    public float comboTimeout = 0.5f; 
    [Tooltip("Duración de la animación del TRUCO.")]
    public float trickDuration = 2.0f; 
    [Tooltip("Duración de la animación de CAÍDA (Crash).")]
    public float crashDuration = 2.0f; 

    [Header("--- INPUT SALTO (S -> W) ---")]
    public float jumpSequenceWindow = 0.25f; 

    [Header("--- MOVIMIENTO ---")]
    public float maxSpeed = 12f;
    public float acceleration = 60f; 
    public float braking = 40f; 
    public float friction = 5f; 
    public float diveAcceleration = 100f; 
    public float diveAngleThreshold = 30f;

    [Header("--- FÍSICAS ---")]
    public float jumpForce = 20f;
    public float jumpCutMultiplier = 0.5f; 
    public float gravityScale = 5f;        
    public float fallGravityMult = 1.5f;
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f); 
    public LayerMask groundLayer;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    // --- ESTADOS PÚBLICOS (Para el Animator) ---
    public bool IsGrounded { get; private set; }
    public bool IsDoingTrick { get; private set; } 
    public bool IsCrashed { get; private set; }    
    public float MoveInput { get; private set; }   

    // --- VARIABLES INTERNAS ---
    private Rigidbody2D rb;
    private float coyoteCounter;
    private float bufferCounter;
    
    // Temporizadores
    private float trickTimer;     // Cuenta atrás del truco
    private float crashTimer;     // Cuenta atrás del golpe
    private float lastDownPressTime = -100f;
    
    // Combo
    private int comboStep = 0;
    private float lastComboTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        // 1. GESTIÓN DE TEMPORIZADORES
        // Si hay tiempo de Crash, restamos
        if (crashTimer > 0)
        {
            crashTimer -= Time.deltaTime;
            IsCrashed = true;
            IsDoingTrick = false; // El golpe cancela el truco
            return; // BLOQUEO DE INPUTS TOTAL
        }
        else
        {
            IsCrashed = false;
        }

        // Si hay tiempo de Truco, restamos
        if (trickTimer > 0)
        {
            trickTimer -= Time.deltaTime;
            IsDoingTrick = true;
        }
        else
        {
            IsDoingTrick = false;
        }

        // 2. INPUTS
        MoveInput = Input.GetAxisRaw("Horizontal");
        
        HandleJumpSequence(); // S -> W
        HandleAirCombo();     // Q -> E -> Espacio

        // Salto variable
        if ((Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow)) && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            coyoteCounter = 0f; 
        }
    }

    private void FixedUpdate()
    {
        IsGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        // --- LÓGICA DE ATERRIZAJE FALLIDO ---
        // Si tocamos suelo MIENTRAS el temporizador de truco está activo
        if (IsGrounded && trickTimer > 0)
        {
            Debug.Log("¡CRASH! Aterrizaje durante truco.");
            trickTimer = 0; // Cancelar truco
            crashTimer = crashDuration; // Activar Crash
            IsCrashed = true;
        }

        // Si estamos estrellados, solo aplicamos gravedad y fricción de suelo
        if (IsCrashed) 
        { 
            ApplyGravity();
            // Frenado suave al caerse
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 5f * Time.fixedDeltaTime);
            return; 
        }

        if (IsGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        ApplyMovement();
        ApplyGravity();
        CheckJump();
    }

    // --- SISTEMA DE COMBOS Q-E-ESPACIO ---
    private void HandleAirCombo()
    {
        // Si tocamos suelo, reseteamos el combo
        if (IsGrounded) 
        { 
            if (comboStep > 0) Debug.Log("Combo reseteado por tocar suelo");
            comboStep = 0; 
            return; 
        }

        // Si tardamos mucho, reseteamos
        if (Time.time - lastComboTime > comboTimeout && comboStep > 0) 
        {
            comboStep = 0;
        }

        // Si ya estamos haciendo el truco, no hacemos nada
        if (IsDoingTrick) return;

        // PASO 1: Q
        if (comboStep == 0)
        {
            if (Input.GetKeyDown(KeyCode.Q)) 
            { 
                comboStep = 1; 
                lastComboTime = Time.time;
                Debug.Log("Combo: Q detectada");
            }
        }
        // PASO 2: E
        else if (comboStep == 1)
        {
            if (Input.GetKeyDown(KeyCode.E)) 
            { 
                comboStep = 2; 
                lastComboTime = Time.time;
                Debug.Log("Combo: E detectada");
            }
        }
        // PASO 3: Espacio
        else if (comboStep == 2)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("¡TRUCO ACTIVADO!");
                trickTimer = trickDuration; // ACTIVAR TEMPORIZADOR DEL TRUCO
                comboStep = 0; 
            }
        }
    }

    // --- RESTO DE MOVIMIENTO ---
    private void ApplyMovement()
    {
        Vector3 intendedDir = GetInputDirectionVector();

        if (!IsGrounded) // AIRE
        {
            float verticalThreshold = -Mathf.Sin(diveAngleThreshold * Mathf.Deg2Rad);
            bool isDiving = intendedDir.y < verticalThreshold;
            if (isDiving && MoveInput != 0) rb.AddForce(intendedDir * diveAcceleration);
            return; 
        }

        // SUELO
        float currentSpeedInDir = Vector2.Dot(rb.linearVelocity, intendedDir);
        float targetSpeed = Mathf.Abs(MoveInput) * maxSpeed; 
        float speedDif = targetSpeed - currentSpeedInDir;
        float accelRate = (Mathf.Abs(MoveInput) > 0.01f) ? acceleration : friction;
        
        if(Mathf.Abs(MoveInput) > 0.01f && currentSpeedInDir > maxSpeed) accelRate = 0; // Inercia
        if(MoveInput == 0 && rb.linearVelocity.magnitude > 0.1f) 
        {
             intendedDir = -rb.linearVelocity.normalized;
             speedDif = rb.linearVelocity.magnitude;
        }

        float projectedVel = Vector2.Dot(rb.linearVelocity, intendedDir);
        if (MoveInput != 0 && projectedVel < -0.1f) accelRate = braking;

        rb.AddForce(intendedDir * speedDif * accelRate);
    }

    private void HandleJumpSequence()
    {
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) lastDownPressTime = Time.time;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (Time.time - lastDownPressTime <= jumpSequenceWindow)
            {
                bufferCounter = jumpBufferTime;
                lastDownPressTime = -100f; 
            }
        }
        bufferCounter -= Time.deltaTime;
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

    private void ApplyGravity()
    {
        if (rb.linearVelocity.y < 0) rb.gravityScale = gravityScale * fallGravityMult;
        else rb.gravityScale = gravityScale;
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        crashTimer = duration; // Usamos el crash timer para el knockback también
        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = direction * force;
    }

    private Vector3 GetInputDirectionVector()
    {
        if (MoveInput == 0) return Vector3.zero;
        Vector3 dir = Quaternion.Euler(0, 0, rb.rotation) * Vector3.right;
        dir *= MoveInput;
        return dir;
    }
}