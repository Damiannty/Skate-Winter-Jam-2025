using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    // --- Singleton (Para acceso global) ---
    public static GameManager Instance { get; private set; }
    
    [Header("Configuración General")]
    public Transform playerTransform; // Arrastra aquí al jugador
    
    // CAMBIO CLAVE: Ahora usamos la lista de handlers, no solo transforms
    public List<DoorCollisionHandler> doorHandlers; 

    [Header("Configuración de Puntuación")]
    public float baseScorePerDelivery = 1000f; // Puntos base por entregar
    public float timePenaltyFactor = 10f;      // Puntos que pierdes por cada segundo que tardas
    public float fallPenalty = 50f;            // Puntos que pierdes por cada caída

    // --- Variables de Estado ---
    private float currentTimer;
    private bool isDelivering;
    
    // Almacena el handler de la puerta actual activa
    private DoorCollisionHandler currentActiveDoor; 
    private int fallCount;
    private float trickScore = 0; // Puntos obtenidos por trucos en la entrega actual

    // Eventos (Opcional: Para actualizar la UI cuando algo cambia)
    public delegate void DeliveryAction(Transform target);
    public event DeliveryAction OnNewOrderReceived;
    public event System.Action<float> OnDeliveryCompleted;

    private void Awake()
    {
        // Configuración del Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Fallback: Si la lista está vacía, busca todas las puertas en la escena
        if (doorHandlers == null || doorHandlers.Count == 0)
        {
            doorHandlers = FindObjectsByType<DoorCollisionHandler>(FindObjectsSortMode.None).ToList();
        }
        
        StartNewDelivery();
    }

    private void Update()
    {
        if (isDelivering)
        {
            currentTimer += Time.deltaTime;
            // UIManager.Instance.UpdateTimer(currentTimer);
        }
    }

    // --- Lógica Principal ---

    public void StartNewDelivery()
    {
        // 1. Limpieza: Desactivar la puerta anterior (si existe)
        if (currentActiveDoor != null)
        {
            currentActiveDoor.isGoalDoor = false;
        }
        
        // 2. Reseteamos variables para la nueva entrega
        currentTimer = 0f;
        fallCount = 0;
        trickScore = 0f; // Resetear el puntaje de trucos para el nuevo pedido
        isDelivering = true;

        // 3. Seleccionar siguiente puerta basada en distancia
        currentActiveDoor = SelectNextDoorWeighted();

        // 4. Activación: Decirle a esa puerta "Tú eres la meta"
        if (currentActiveDoor != null)
        {
            currentActiveDoor.isGoalDoor = true;

            Debug.Log($"Nuevo pedido! Ve a: {currentActiveDoor.name}");

            // Notificar UI (Pasamos el Transform de la puerta activa para la flecha/UI)
            UIManager.Instance.ShowNewTarget(currentActiveDoor.transform);
            OnNewOrderReceived?.Invoke(currentActiveDoor.transform);
        }
    }

    // Llamado EXCLUSIVAMENTE por DoorCollisionHandler cuando el jugador entra en la puerta correcta
    public void CompleteDelivery()
    {
        if (!isDelivering) return;

        isDelivering = false;

        // 1. Cálculo de Puntuación (usamos el trickScore acumulado en esta instancia)
        float finalScore = CalculateScore(trickScore);

        // 2. Notificar UI y eventos
        UIManager.Instance.UpdateScore(finalScore); // Si tienes este método en tu UIManager
        UIManager.Instance.ShowDeliveryComplete();
        OnDeliveryCompleted?.Invoke(finalScore);

        // 3. Iniciar siguiente pedido
        StartNewDelivery(); 
    }

    // Llamar la funcion cuando termine un combo de trucos
    public void AddTrickScore(float points)
    {
        if (isDelivering)
        {
            trickScore += points;
            Debug.Log($"Puntos por trucos acumulados: {trickScore}");
        }
    }

    public void RegisterFall()
    {
        if (isDelivering)
        {
            fallCount++;
            Debug.Log($"Caída registrada. Total: {fallCount}");
        }
    }

    // --- Algoritmos Internos ---

    private DoorCollisionHandler SelectNextDoorWeighted()
    {
        // Filtrar la lista para no incluir la puerta donde estamos ahora
        List<DoorCollisionHandler> validDoors = doorHandlers
            // Usamos d.transform.position para obtener la posición de la puerta
            .Where(d => Vector2.Distance(playerTransform.position, d.transform.position) > 2.0f)
            .ToList();

        if (validDoors.Count == 0) return doorHandlers[Random.Range(0, doorHandlers.Count)];

        // Algoritmo de "Ruleta Ponderada"
        float totalDistance = 0f;
        
        // Calcular la suma total de distancias
        foreach (var door in validDoors)
        {
            totalDistance += Vector2.Distance(playerTransform.position, door.transform.position);
        }

        // Elegir un valor aleatorio dentro de esa suma total
        float randomValue = Random.Range(0, totalDistance);
        float currentSum = 0f;

        // Encontrar qué puerta corresponde a ese valor
        foreach (var door in validDoors)
        {
            currentSum += Vector2.Distance(playerTransform.position, door.transform.position);
            
            if (currentSum >= randomValue)
            {
                return door;
            }
        }

        // Fallback
        return validDoors.Last();
    }

    private float CalculateScore(float trickPoints)
    {
        // Fórmula: Puntuación Final = (Base + Trucos) - (Tiempo * factor) - (Caídas * castigo)
        
        float timeDeduction = currentTimer * timePenaltyFactor;
        float fallDeduction = fallCount * fallPenalty;

        float total = (baseScorePerDelivery + trickPoints) - (timeDeduction + fallDeduction);

        // Aseguramos que no dé puntuación negativa
        return Mathf.Max(0, total);
    }
    
    // Método auxiliar para saber cuál es el objetivo actual (útil para la flecha)
    public Transform GetCurrentTarget()
    {
        // Devuelve el Transform de la puerta activa
        return currentActiveDoor != null ? currentActiveDoor.transform : null;
    }
}