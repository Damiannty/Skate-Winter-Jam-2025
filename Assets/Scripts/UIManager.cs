using System.Collections;
using System.Collections.Generic; // Necesario para Dictionary
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Elementos de Texto")]
    [SerializeField] private TextMeshProUGUI deliveryCompleteText;
    [SerializeField] private TextMeshProUGUI nextTargetText;

    [Header("Configuración de Fade")]
    public float fadeTime = 0.5f;
    public float messageDuration = 2.0f;

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
    }

    public void ShowNewTarget(Transform target)
    {
        nextTargetText.text = "Nuevo Destino:\n" + target.name;
        DisplayMessageCycle(nextTargetText, messageDuration);
    }

    public void UpdateScore(float score)
    {
        // Aquí podrías actualizar un elemento de UI que muestre el puntaje
        // Por ejemplo:
        // scoreText.text = "Puntaje: " + score.ToString("F2");
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
}