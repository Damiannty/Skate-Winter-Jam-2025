using UnityEngine;

public class DoorCollisionHandler : MonoBehaviour
{
    public bool isGoalDoor = false; // El GameManager cambiará esto a true o false

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Verificar si es el jugador
        if (other.CompareTag("Player"))
        {
            // 2. CRÍTICO: Verificar si esta es la puerta correcta
            if (isGoalDoor)
            {
                Debug.Log("¡Entrega Correcta en: " + gameObject.name + "!");
                
                // Avisar al Manager
                // (Recuerda pasar el puntaje de trucos si tu función lo pide, aquí pongo 0 como ejemplo)
                GameManager.Instance.CompleteDelivery(); 
                
                // Opcional: Desactivarse inmediatamente para no volver a triggerear
                isGoalDoor = false; 
            }
            else
            {
                Debug.Log("Esta no es la puerta de destino.");
            }
        }
    }
}