using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("--- MOVIMIENTO HORIZONTAL ---")] [SerializeField]
    private Transform _skateTransform;
    public float maxSpeed = 12f;
    public float acceleration = 60f; // Qué tan rápido arranca
    public float deceleration = 40f; // Qué tan rápido frena (suelo)
    public float turnSpeed = 80f;    // Qué tan rápido gira (cambio de dirección)
    public float airControlMult = 0.8f; // (0 a 1) Cuánto control pierdes en el aire. 1 = control total.

    [Header("--- SALTO & GRAVEDAD ---")]
    public float jumpForce = 20f;
    public float jumpCutMultiplier = 0.5f; // Al soltar botón
    public float gravityScale = 5f;        // Gravedad base (pesada)
    public float fallGravityMult = 1.5f;   // Gravedad al caer

    [Header("--- AYUDAS (GAME FEEL) ---")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("--- DETECCIÓN ---")]
    public Transform groundCheck;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f); // Tamaño caja detección
    public LayerMask groundLayer;

    // Variables Internas
    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool isJumping; // Para saber si estamos en medio de una subida de salto

    // Timers
    private float coyoteCounter;
    private float bufferCounter;

    // Referencias Visuales
    private bool isFacingRight = true;

    private void Start()
    {
        // 1. Asegurar que la física es DINÁMICA (afectada por gravedad)
        rb.bodyType = RigidbodyType2D.Dynamic;
        
        // 2. Asegurar que la masa es 1
        rb.mass = 1f;

        // 3. Imprimir el valor real de la gravedad para ver si es 0
        Debug.Log("Gravedad inicial del script: " + gravityScale);
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 1. LECTURA DE INPUTS (Siempre en Update)
        moveInput = Input.GetAxisRaw("Horizontal");

        // GESTIÓN DEL BUFFER DE SALTO
        if (Input.GetButtonDown("Jump"))
        {
            bufferCounter = jumpBufferTime;
        }
        else
        {
            bufferCounter -= Time.deltaTime;
        }

        // SALTO VARIABLE (Soltar botón)
        // Si soltamos el botón Y estamos subiendo, cortamos la velocidad
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            coyoteCounter = 0f; // Cancelar coyote para evitar resaltos raros
        }

        // VOLTEAR PERSONAJE (Visual)
        if (moveInput > 0 && !isFacingRight) Flip();
        else if (moveInput < 0 && isFacingRight) Flip();
    }

    private void FixedUpdate()
    {
        // 2. DETECCIÓN DE SUELO
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        // GESTIÓN DEL COYOTE TIME
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            // Si acabamos de aterrizar, reseteamos gravedad por si acaso
            if (!wasGrounded) isJumping = false; 
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        // 3. APLICAR LÓGICAS
        ApplyMovement();
        ApplyGravity();
        CheckJump();
    }

    private void ApplyMovement()
    {
        // Calculamos la velocidad objetivo
        float targetSpeed = moveInput * maxSpeed;
        
        // Obtenemos la diferencia entre la velocidad actual y la deseada
        float speedDif = targetSpeed - rb.linearVelocity.x;

        // Decidimos qué tasa de aceleración usar (Acelerar, Frenar o Girar)
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        // Si estamos girando (Input contrario a velocidad actual), usamos TurnSpeed
        if (Mathf.Abs(targetSpeed) > 0.01f && Mathf.Sign(targetSpeed) != Mathf.Sign(rb.linearVelocity.x))
        {
            accelRate = turnSpeed;
        }

        // Si estamos en el aire, reducimos un poco el control (opcional)
        if (!isGrounded)
        {
            accelRate *= airControlMult;
        }

        // Aplicamos la fuerza de movimiento
        // Usamos ForceMode2D.Force para movimiento fluido basado en masa
        float movement = speedDif * accelRate;
        rb.AddForce(movement * _skateTransform.right.normalized);
    }

    private void ApplyGravity()
    {
        // Si estamos cayendo, gravedad fuerte. Si subimos, gravedad normal.
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
    if (bufferCounter > 0f && coyoteCounter > 0f)
    {
        // BORRA O COMENTA ESTA LÍNEA:
        // rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // PON ESTA LÍNEA EN SU LUGAR:
        // Esto asegura que el salto siempre sea igual, sin importar fuerzas previas
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        isJumping = true;
        bufferCounter = 0f;
        coyoteCounter = 0f;
    }
}

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // Dibuja la caja de detección de suelo en el editor
    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}