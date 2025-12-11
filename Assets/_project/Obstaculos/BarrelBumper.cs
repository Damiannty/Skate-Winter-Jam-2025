using System.Collections;
using UnityEngine;

public class BarrelBumper : MonoBehaviour
{
    [Header("Configuración del Rebote")]
    [Tooltip("La fuerza con la que el jugador saldrá disparado.")]
    public float pushForce = 15f;
    
    [Tooltip("Tiempo en segundos antes de que el barril se pueda usar de nuevo.")]
    public float resetTime = 1.5f;

    [Header("Referencias Visuales")]
    public SpriteRenderer spriteRenderer;
    public Color inactiveColor = Color.gray;
    private Color originalColor;

    private bool isActive = true;

    private void Start()
    {
        // Guardamos el color original para restaurarlo después
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo interactuamos si el barril está activo y es el jugador
        if (isActive && other.CompareTag("Player"))
        {
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();

            if (playerRb != null)
            {
                Explode(playerRb);
            }
        }
    }

    private void Explode(Rigidbody2D playerRb)
    {
        // 1. Calcular la dirección: (Posición Jugador - Posición Barril)
        Vector2 direction = (playerRb.transform.position - transform.position).normalized;

        // 2. Aplicar la velocidad estilo "Celeste" (Sobrescribir velocidad actual)
        // Esto detiene cualquier inercia previa y lanza al jugador en la nueva dirección.
        playerRb.linearVelocity = direction * pushForce;

        // 3. Desactivar el barril temporalmente (Feedback visual y lógico)
        StartCoroutine(DisableBarrelRoutine());
    }

    private IEnumerator DisableBarrelRoutine()
    {
        isActive = false;
        spriteRenderer.color = inactiveColor; // Cambiar color a "apagado"

        // Aquí podrías instanciar partículas o reproducir un sonido
        // Ejemplo: Instantiate(explosionParticles, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(resetTime);

        isActive = true;
        spriteRenderer.color = originalColor; // Restaurar color
    }
}
