using System.Collections;
using UnityEngine;

public class BarrelBumper : MonoBehaviour
{
    [Header("Configuración del Rebote")]
    [Tooltip("La fuerza con la que el jugador saldrá disparado.")]
    public float pushForce = 25f; // Sube esto un poco (20-30 suele ir bien)
    
    [Tooltip("Tiempo (segundos) que el jugador pierde el control tras el impacto.")]
    public float stunTime = 0.3f; 
    
    [Tooltip("Tiempo en segundos antes de que el barril se pueda usar de nuevo.")]
    public float resetTime = 1.5f;

    [Header("Referencias Visuales")]
    public SpriteRenderer spriteRenderer;
    public Color inactiveColor = Color.gray;
    private Color originalColor;

    private bool isActive = true;

    private void Start()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActive && other.CompareTag("Player"))
        {
            // CAMBIO: Buscamos el PlayerController, no solo el Rigidbody
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                Explode(player);
            }
        }
    }

    private void Explode(PlayerController player)
    {
        // 1. Calcular dirección (Jugador - Barril)
        Vector2 direction = (player.transform.position - transform.position).normalized;

        // --- TRUCO CRUCIAL PARA EL SUELO ---
        // Si el jugador choca lateralmente (Y cerca de 0), forzamos un poco hacia arriba.
        // Esto evita que roce con el suelo y pierda velocidad por fricción.
        if (direction.y < 0.2f)
        {
            direction.y = 0.5f; // Forzar ángulo hacia arriba
            direction = direction.normalized; // Re-normalizar para no aumentar la fuerza artificialmente
        }

        // 2. Usar el sistema de Knockback del jugador
        // Esto le dice al PlayerController: "¡Deja de leer las teclas durante 0.3 segundos!"
        player.ApplyKnockback(direction, pushForce, stunTime);

        // 3. Desactivar barril
        StartCoroutine(DisableBarrelRoutine());
    }

    private IEnumerator DisableBarrelRoutine()
    {
        isActive = false;
        spriteRenderer.color = inactiveColor;
        yield return new WaitForSeconds(resetTime);
        isActive = true;
        spriteRenderer.color = originalColor;
    }
}