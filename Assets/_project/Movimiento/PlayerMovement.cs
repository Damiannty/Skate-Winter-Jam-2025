using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("--- REFERENCIAS ---")]
    [SerializeField] private Transform _skateTransform; 
    private SpriteRenderer _spriteRenderer; 

    [Header("--- INPUT DE SECUENCIA (SALTO) ---")]
    public float jumpSequenceWindow = 0.25f; 

    [Header("--- TRUCOS (COMBO AÉREO) ---")]
    public float comboTimeout = 0.5f; 
    
    [Tooltip("Tiempo (segundos) que dura el truco. Durante este tiempo eres ROJO y si tocas suelo te estrellas.")]
    public float trickDuration = 1.0f; // <--- NUEVA VARIABLE MODIFICABLE

    [Tooltip("Tiempo (segundos) que te quedas aturdido (VERDE) y sin control si aterrizas mal.")]
    public float crashDuration = 1.5f; // <--- NUEVA VARIABLE MODIFICABLE

    private int comboStep = 0; 
    private float lastComboTime; 
    private bool isDoingTrick = false; 
    private Coroutine currentTrickRoutine; 

    [Header("--- MOVIMIENTO SUELO ---")]
    public float maxSpeed = 12f;
    public float acceleration = 60f; 
    public float braking = 40f; 
    public float friction = 5f; 

    [Header("--- MOVIMIENTO PICADO ---")]
    public float diveAcceleration = 100f; 
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

    private float lastDownPressTime = -100f; 
    private Color originalColor; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer != null) originalColor = _spriteRenderer.color;
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
        // SI ESTAMOS BLOQUEADOS (Por Barril o por Crash)
        if (knockbackCounter > 0)
        {
            knockbackCounter -= Time.deltaTime;
            
            // Si el tiempo de crash se acaba, restauramos el color
            if (knockbackCounter <= 0 && _spriteRenderer != null)
            {
                _spriteRenderer.color = originalColor;
            }
            return; 
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        HandleJumpSequence();
        HandleAirCombo();

        bool upKeyReleased = Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);
        if (upKeyReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            coyoteCounter = 0f; 
        }

        if (isGrounded)
        {
            if (moveInput > 0 && !isFacingRight) Flip();
            else if (moveInput < 0 && isFacingRight) Flip();
        }
    }

    // --- LÓGICA DE TRUCOS ---
    private void HandleAirCombo()
    {
        if (isGrounded) { comboStep = 0; return; }
        if (Time.time - lastComboTime > comboTimeout && comboStep > 0) comboStep = 0;
        if (isDoingTrick) return; 

        if (comboStep == 0)
        {
            if (Input.GetKeyDown(KeyCode.Q)) { comboStep = 1; lastComboTime = Time.time; }
        }
        else if (comboStep == 1)
        {
            if (Input.GetKeyDown(KeyCode.E)) { comboStep = 2; lastComboTime = Time.time; }
        }
        else if (comboStep == 2)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (currentTrickRoutine != null) StopCoroutine(currentTrickRoutine);
                currentTrickRoutine = StartCoroutine(PerformTrickVisuals());
                comboStep = 0; 
            }
        }
    }

    private IEnumerator PerformTrickVisuals()
    {
        isDoingTrick = true; 

        if (_spriteRenderer != null) _spriteRenderer.color = Color.red;

        // USA LA VARIABLE NUEVA (trickDuration)
        yield return new WaitForSeconds(trickDuration);

        if (_spriteRenderer != null) _spriteRenderer.color = originalColor;
        isDoingTrick = false; 
    }

    // --- LÓGICA DE CHOQUE (CRASH) ---
    private void TriggerCrash()
    {
        if (currentTrickRoutine != null) StopCoroutine(currentTrickRoutine);
        isDoingTrick = false;

        if (_spriteRenderer != null) _spriteRenderer.color = Color.green;

        // USA LA VARIABLE NUEVA (crashDuration)
        knockbackCounter = crashDuration; 
        
        Debug.Log("¡CRASH! Aterrizaje fallido.");
    }

    private void HandleJumpSequence()
    {
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) lastDownPressTime = Time.time;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            float timeSinceDown = Time.time - lastDownPressTime;
            if (timeSinceDown <= jumpSequenceWindow)
            {
                bufferCounter = jumpBufferTime;
                lastDownPressTime = -100f; 
            }
        }
        bufferCounter -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        // DETECCIÓN DE ATERRIZAJE FALLIDO
        if (isGrounded && isDoingTrick)
        {
            TriggerCrash();
        }

        if (knockbackCounter > 0) { ApplyGravity(); return; }

        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        ApplyMovement();
        ApplyGravity();
        CheckJump();
    }

    private void ApplyMovement()
    {
        Vector3 intendedDir = GetInputDirectionVector();

        if (!isGrounded)
        {
            float verticalThreshold = -Mathf.Sin(diveAngleThreshold * Mathf.Deg2Rad);
            bool isDiving = intendedDir.y < verticalThreshold;

            if (isDiving && moveInput != 0) rb.AddForce(intendedDir * diveAcceleration);
            return; 
        }

        float currentSpeedInDir = Vector2.Dot(rb.linearVelocity, intendedDir);
        float targetSpeed = Mathf.Abs(moveInput) * maxSpeed; 
        float speedDif = targetSpeed - currentSpeedInDir;

        float accelRate = 0;

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            if (currentSpeedInDir < targetSpeed)
            {
                if (currentSpeedInDir > maxSpeed) accelRate = 0;
                else accelRate = acceleration;
            }
            else accelRate = 0; 
        }
        else
        {
            accelRate = friction; 
            if(rb.linearVelocity.magnitude > 0.1f)
            {
                intendedDir = -rb.linearVelocity.normalized;
                speedDif = rb.linearVelocity.magnitude;
            }
        }
        
        float projectedVel = Vector2.Dot(rb.linearVelocity, intendedDir);
        if (moveInput != 0 && projectedVel < -0.1f) accelRate = braking;

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

    private Vector3 GetInputDirectionVector()
    {
        if (moveInput == 0) return Vector3.zero;
        Vector3 dir = Vector3.right;
        dir = Quaternion.Euler(0, 0, rb.rotation) * dir;
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