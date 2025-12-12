using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AirStabilizer : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Qué tan rápido se endereza el personaje en el aire")]
    public float airCorrectionSpeed = 5f;
    
    [Tooltip("Si activas esto, el personaje rotará para ajustarse a las rampas cuando toque suelo")]
    public bool alignWithSlopes = true;
    [Tooltip("Velocidad de ajuste al suelo")]
    public float groundAlignmentSpeed = 10f;

    [Header("Referencias (Deben coincidir con PlayerController)")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);

    private Rigidbody2D rb;
    private bool isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        CheckGround();
        Stabilize();
    }

    private void CheckGround()
    {
        // Reutilizamos la misma lógica de detección que tu PlayerController
        // para asegurar que ambos scripts sepan lo mismo.
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        }
    }

    private void Stabilize()
    {
        if (!isGrounded)
        {
            // --- EN EL AIRE: ENDEREZAR ---
            
            // 1. Eliminar la fuerza de rotación física (para que no siga girando por inercia)
            rb.angularVelocity = 0f;

            // 2. Calcular el ángulo actual y el objetivo (0 grados = recto)
            float currentAngle = rb.rotation;
            float targetAngle = 0f;

            // 3. Mover suavemente la rotación hacia 0
            // Mathf.LerpAngle maneja automáticamente el salto de 360 a 0 grados
            float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, airCorrectionSpeed * Time.fixedDeltaTime);
            
            rb.MoveRotation(newAngle);
        }
        else if (alignWithSlopes)
        {
            // --- EN EL SUELO: ALINEAR CON RAMPA (Opcional pero recomendado para Skate) ---
            AlignToSlope();
        }
    }

    private void AlignToSlope()
    {
        // Lanzamos un rayo hacia abajo para detectar la inclinación de la rampa
        RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 1.5f, groundLayer);

        if (hit.collider != null)
        {
            // Obtenemos la "normal" (la flecha que apunta perpendicular a la superficie)
            Vector2 normal = hit.normal;

            // Convertimos esa normal en un ángulo en grados
            // Atan2 nos da el ángulo en radianes, convertimos a grados y restamos 90 porque el sprite mira hacia arriba
            float slopeAngle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg - 90f;

            // Rotamos suavemente hacia ese ángulo
            float newAngle = Mathf.LerpAngle(rb.rotation, slopeAngle, groundAlignmentSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }
    }
}
