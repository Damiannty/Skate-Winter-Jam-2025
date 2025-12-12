using UnityEngine;
using System.Collections; // Necesario para usar Coroutines

public class DirectionArrow : MonoBehaviour
{
    // --- Configuración Pública ---
    [Header("Configuración de la Flecha")]
    public float rotationSpeed = 10f; // Velocidad de rotación suave
    public float visibilityDuration = 5f; // Tiempo que la flecha permanece visible al presionar Alt
    public float fadeTime = 0.5f;       // Tiempo que toma el fade in/out

    // --- Componentes y Estado ---
    private Transform targetTransform;
    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private Coroutine currentFadeCoroutine;

    private void Start()
    {
        // 1. Obtener componentes
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("El objeto de la flecha necesita un SpriteRenderer.");
            enabled = false;
            return;
        }

        // 2. Configuración inicial (invisible)
        Color invisible = spriteRenderer.color;
        invisible.a = 0f;
        spriteRenderer.color = invisible;
        
        // 3. Inicializar valores desde el GameManager (como antes)
        if (GameManager.Instance == null)
        {
            Debug.LogError("DirectionArrow requiere que haya un GameManager en la escena.");
            enabled = false;
            return;
        }

        GameManager.Instance.OnNewOrderReceived += SetNewTarget;
        playerTransform = GameManager.Instance.playerTransform;
        SetNewTarget(GameManager.Instance.GetCurrentTarget()); // Configura el objetivo inicial
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewOrderReceived -= SetNewTarget;
        }
    }

    private void Update()
    {
        // 1. Detección de Input para mostrar la flecha
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            ShowArrow();
        }

        // 2. Lógica de Rotación (solo si hay un objetivo)
        if (targetTransform != null && playerTransform != null)
        {
           
           transform.position = playerTransform.position; // Mantener la flecha en la posición del jugador

            // Calcular la dirección y el ángulo (Lógica de rotación es la misma)
            Vector3 direction = targetTransform.position - playerTransform.position;
            direction.z = 0; 
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(new Vector3(0, 0, angle - 90));

            // Aplicar la rotación suave
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // --- Lógica de Visibilidad y Fading ---
    
    // Inicia el ciclo de visibilidad (Fade In, Espera, Fade Out)
    public void ShowArrow()
    {
        if (targetTransform == null) return;

        // Si ya hay un proceso de fade corriendo, lo detenemos
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        
        // Empezamos la nueva corrutina
        currentFadeCoroutine = StartCoroutine(VisibilityCycleCoroutine());
    }

    private IEnumerator VisibilityCycleCoroutine()
    {
        // FAZ 1: FADE IN (0% a 100% de Opacidad)
        yield return StartCoroutine(FadeCoroutine(1f));

        // FAZ 2: ESPERA
        yield return new WaitForSeconds(visibilityDuration);

        // FAZ 3: FADE OUT (100% a 0% de Opacidad)
        yield return StartCoroutine(FadeCoroutine(0f));

        // Limpieza
        currentFadeCoroutine = null;
    }

    private IEnumerator FadeCoroutine(float targetAlpha)
    {
        float startAlpha = spriteRenderer.color.a;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeTime);

            Color newColor = spriteRenderer.color;
            newColor.a = newAlpha;
            spriteRenderer.color = newColor;

            yield return null; // Espera al siguiente frame
        }

        // Aseguramos que la opacidad final sea exactamente el objetivo
        Color finalColor = spriteRenderer.color;
        finalColor.a = targetAlpha;
        spriteRenderer.color = finalColor;
    }
    
    // Llamado por el evento del GameManager
    private void SetNewTarget(Transform newTarget)
    {
        targetTransform = newTarget;
    }
}