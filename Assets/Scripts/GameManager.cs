using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Necesario para funciones de listas avanzadas

public class GameManager : MonoBehaviour
{
    // --- Singleton (Para acceso global) ---
    public static GameManager Instance { get; private set; }

    [Header("Configuración General")]
    public Transform playerTransform; // Arrastra aquí al jugador
    public List<Transform> doorLocations; // Arrastra aquí todos los puntos de entrega (GameObjects vacíos o puertas)

    [Header("Configuración de Puntuación")]
    public float baseScorePerDelivery = 1000f; // Puntos base por entregar
    public float timePenaltyFactor = 10f;      // Puntos que pierdes por cada segundo que tardas
    public float fallPenalty = 50f;            // Puntos que pierdes por cada caída

    // --- Variables de Estado ---
    private float currentTimer;
    private bool isDelivering;
    private Transform currentTargetDoor;
    private int fallCount;

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
        // Iniciar el primer encargo al arrancar el juego (o llámalo cuando quieras)
        StartNewDelivery();
    }

    private void Update()
    {
        if (isDelivering)
        {
            currentTimer += Time.deltaTime;
            // Aquí podrías actualizar un texto de UI con el tiempo:
            // UIManager.Instance.UpdateTimer(currentTimer);
        }
    }

    // --- Lógica Principal ---

    public void StartNewDelivery()
    {
        // 1. Reseteamos variables
        currentTimer = 0f;
        fallCount = 0;
        isDelivering = true;

        // 2. Seleccionar siguiente puerta basada en distancia
        currentTargetDoor = SelectNextDoorWeighted();

        Debug.Log($"Nuevo pedido! Ve a: {currentTargetDoor.name}");

        // Notificar a otros scripts (como la UI o la flecha de guía)
        OnNewOrderReceived?.Invoke(currentTargetDoor);
    }

    // Llamado cuando el jugador toca la puerta destino
    // 'trickScore' debe venir de tu script de trucos
    public void CompleteDelivery(float trickScore)
    {
        if (!isDelivering) return;

        isDelivering = false;

        // 1. Cálculo de Puntuación
        float finalScore = CalculateScore(trickScore);

        Debug.Log($"Entregado! Puntuación Total: {finalScore}");
        
        // Notificar UI de puntuación final
        OnDeliveryCompleted?.Invoke(finalScore);

        // 2. Opcional: Iniciar siguiente pedido inmediatamente o esperar input
        StartNewDelivery(); 
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

    private Transform SelectNextDoorWeighted()
    {
        // Filtrar la lista para no incluir la puerta donde estamos ahora (si estamos en una)
        // Asumimos que si estamos cerca de una puerta (distancia < 2), es la actual.
        List<Transform> validDoors = doorLocations.Where(d => Vector2.Distance(playerTransform.position, d.position) > 2.0f).ToList();

        if (validDoors.Count == 0) return doorLocations[Random.Range(0, doorLocations.Count)];

        // Algoritmo de "Ruleta Ponderada"
        float totalDistance = 0f;
        
        // Calcular la suma total de distancias
        foreach (Transform door in validDoors)
        {
            totalDistance += Vector2.Distance(playerTransform.position, door.position);
        }

        // Elegir un valor aleatorio dentro de esa suma total
        float randomValue = Random.Range(0, totalDistance);
        float currentSum = 0f;

        // Encontrar qué puerta corresponde a ese valor
        foreach (Transform door in validDoors)
        {
            currentSum += Vector2.Distance(playerTransform.position, door.position);
            
            // Si superamos el valor aleatorio, esta es la elegida.
            // Matemáticamente, esto hace que las distancias más grandes ocupen más "espacio" en la ruleta
            if (currentSum >= randomValue)
            {
                return door;
            }
        }

        // Fallback por si acaso
        return validDoors.Last();
    }

    private float CalculateScore(float trickPoints)
    {
        // Fórmula: (Puntos Trucos) + (Base) - (Tiempo * factor) - (Caídas * castigo)
        
        float timeDeduction = currentTimer * timePenaltyFactor;
        float fallDeduction = fallCount * fallPenalty;

        float total = (baseScorePerDelivery + trickPoints) - (timeDeduction + fallDeduction);

        // Aseguramos que no dé puntuación negativa
        return Mathf.Max(0, total);
    }
    
    // Método auxiliar para saber cuál es el objetivo actual (útil para la UI de la brújula)
    public Transform GetCurrentTarget()
    {
        return currentTargetDoor;
    }
}