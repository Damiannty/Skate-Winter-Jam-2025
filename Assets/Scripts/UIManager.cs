using System.Collections;
using System.Collections.Generic; // Necesario para Dictionary
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Elementos de Texto")]
    [SerializeField] private TextMeshProUGUI deliveryCompleteText;
    [SerializeField] private TextMeshProUGUI nextTargetText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Configuración de Fade")]
    public float fadeTime = 0.5f;
    public float messageDuration = 2.0f;

    [Header("Configuración Pop-Up")]
    public float popDuration = 1.0f; // Cuánto dura la animación
    public float maxScale = 1.5f;    // Qué tan grande se hace (1.5 veces su tamaño)

    // --- CORRECCIÓN: Rastreo individual de corrutinas ---
    // Esto guarda qué corrutina está ejecutando cada texto
    private Dictionary<TextMeshProUGUI, Coroutine> activeCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Inicializamos limpios
        SetAlpha(deliveryCompleteText, 0f);
        SetAlpha(nextTargetText, 0f);
    }

    // --- Función General ---

    public void DisplayMessageCycle(TextMeshProUGUI textElement, float duration)
    {
        if (textElement == null) return;

        // 1. Si este texto ESPECÍFICO ya tiene una animación corriendo, la detenemos.
        // (Pero NO detenemos las de otros textos)
        if (activeCoroutines.ContainsKey(textElement))
        {
            if (activeCoroutines[textElement] != null)
            {
                StopCoroutine(activeCoroutines[textElement]);
            }
            activeCoroutines.Remove(textElement);
        }

        // 2. Iniciamos la nueva corrutina y la guardamos en el diccionario
        Coroutine newCoroutine = StartCoroutine(VisibilityCycleCoroutine(textElement, duration));
        activeCoroutines.Add(textElement, newCoroutine);
    }

    // --- Calls del GameManager ---

    public void ShowDeliveryComplete()
    {
        deliveryCompleteText.text = "¡ENTREGA COMPLETADA!";
        DisplayMessageCycle(deliveryCompleteText, messageDuration);
        ResetTimer();
    }

    public void UpdateTimer(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        int milliseconds = (int)((time * 10) % 10); // Obtiene una décima de segundo

        // Usamos 'D2' para asegurar dos dígitos (00, 01, etc.)
        timerText.text = string.Format("{0:D2}:{1:D2}.{2}", minutes, seconds, milliseconds);
    }

    public void ResetTimer()
    {
       timerText.text = "00:00.0";
    }
    public void ShowNewTarget(Transform target, bool hasReachedGoal)
    {
        if (hasReachedGoal)
        {
          nextTargetText.text = "Nuevo Destino:\n" + target.name;
        DisplayMessageCycle(nextTargetText, messageDuration);
        }
        else
        {
            nextTargetText.text = "Destino Actual:\n" + target.name;
            DisplayMessageCycle(nextTargetText, messageDuration);
        }
        
    }

    public void ShowFinalScore(float score)
    {
        //Mostrar el puntuaje final del envio
        ShowPopUpText(scoreText, "Puntaje Final: " + Mathf.RoundToInt(score).ToString());
        
    }

    // --- Lógica Interna (Corrutinas) ---

    private IEnumerator VisibilityCycleCoroutine(TextMeshProUGUI textElement, float duration)
    {
        // Fade In
        yield return StartCoroutine(FadeCoroutine(textElement, 1f));

        // Espera
        yield return new WaitForSeconds(duration);

        // Fade Out
        yield return StartCoroutine(FadeCoroutine(textElement, 0f));

        // Limpieza: Una vez terminado, quitamos el registro del diccionario
        if (activeCoroutines.ContainsKey(textElement))
        {
            activeCoroutines.Remove(textElement);
        }
    }

    private IEnumerator FadeCoroutine(TextMeshProUGUI textElement, float targetAlpha)
    {
        float startAlpha = textElement.color.a;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeTime);
            SetAlpha(textElement, newAlpha);
            yield return null;
        }

        SetAlpha(textElement, targetAlpha);
    }

    private void SetAlpha(TextMeshProUGUI textElement, float alpha)
    {
        if (textElement == null) return;
        Color color = textElement.color;
        color.a = alpha;
        textElement.color = color;
    }

    // Función privada para inicializar desde la llamada de GameManager a UpdateScore
    private void ShowPopUpText(TextMeshProUGUI textElement, string message)
    {
        if (textElement == null) return;

        textElement.text = message;
        textElement.gameObject.SetActive(true);

        // Reiniciamos valores por si se reusa el objeto
        textElement.transform.localScale = Vector3.one; 
        Color c = textElement.color;
        c.a = 1f;
        textElement.color = c;

        // Si ya se estaba animando este texto específico, paramos su corrutina anterior
        // (Nota: Requiere que uses el diccionario de corrutinas que implementamos antes, 
        // si no lo usas, simplemente pon StartCoroutine directamente)
        if (activeCoroutines.ContainsKey(textElement) && activeCoroutines[textElement] != null)
        {
            StopCoroutine(activeCoroutines[textElement]);
            activeCoroutines.Remove(textElement);
        }

        Coroutine newRoutine = StartCoroutine(PopUpCoroutine(textElement));
        
        // Guardamos la referencia (si usas el sistema de diccionario)
        activeCoroutines.Add(textElement, newRoutine);
    }

    private IEnumerator PopUpCoroutine(TextMeshProUGUI textElement)
    {
        float timer = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.one * maxScale;
        Color startColor = textElement.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f); // Transparente

        while (timer < popDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / popDuration;

            // 1. Crecimiento Suave
            textElement.transform.localScale = Vector3.Lerp(startScale, endScale, progress);

            // 2. Desvanecimiento (Opcional: puedes hacer que empiece a desvanecerse a la mitad con 'progress > 0.5f')
            textElement.color = Color.Lerp(startColor, endColor, progress);

            yield return null;
        }

        // Al terminar, ocultamos el objeto
        textElement.gameObject.SetActive(false);
        
        // Limpieza del diccionario
        if (activeCoroutines.ContainsKey(textElement))
        {
            activeCoroutines.Remove(textElement);
        }
    }
}