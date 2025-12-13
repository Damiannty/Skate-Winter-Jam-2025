using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ParallaxConHijos : MonoBehaviour
{
    [Header("Configuración")]
    public float parallaxFactorX;
    public float parallaxFactorY;
    
    private Camera cam;
    private Vector3 lastCameraPosition;
    private float spriteWidth;
    private List<Transform> children;

    void Start()
    {
        cam = Camera.main;
        lastCameraPosition = cam.transform.position;

        // 1. Detectar los hijos y ordenarlos por su posición X
        children = new List<Transform>();
        foreach (Transform child in transform)
        {
            children.Add(child);
        }
        
        // Ordenamos la lista para saber cuál está a la izq y cuál a la derecha
        children = children.OrderBy(t => t.position.x).ToList();

        // 2. Calcular el ancho de UN solo sprite (asumimos que todos miden lo mismo)
        SpriteRenderer firstSprite = children[0].GetComponent<SpriteRenderer>();
        if (firstSprite != null)
        {
            spriteWidth = firstSprite.bounds.size.x;
        }
    }

    void LateUpdate()
    {
        MoverParallax();
        ChequearReubicacionInfinita();
    }

    void MoverParallax()
    {
        Vector3 deltaMovement = cam.transform.position - lastCameraPosition;
        
        transform.position += new Vector3(
            deltaMovement.x * parallaxFactorX,
            deltaMovement.y * parallaxFactorY,
            0);

        lastCameraPosition = cam.transform.position;
    }

    void ChequearReubicacionInfinita()
    {
        // Solo funciona si tenemos hijos y sabemos cuánto miden
        if (children.Count < 2) return;

        // Distancia desde la cámara hasta los bordes de la pantalla (aprox)
        float screenWidthInUnits = (cam.orthographicSize * cam.aspect) * 2;
        // Le damos un margen de error (buffer) para que no se vea el "pop"
        float buffer = spriteWidth / 2;

        // --- LÓGICA DE CINTA TRANSPORTADORA ---

        // 1. CHEQUEAR IZQUIERDA (Si vamos a la derecha)
        // El primer hijo de la lista es el que está más a la izquierda
        Transform firstChild = children[0];
        Transform lastChild = children[children.Count - 1];

        // Si el hijo de la izquierda ya se salió MUCHO de la cámara por la izquierda...
        if (cam.transform.position.x - screenWidthInUnits > firstChild.position.x + buffer)
        {
            // Lo movemos al final de la fila (a la derecha del último)
            firstChild.position = new Vector3(lastChild.position.x + spriteWidth, firstChild.position.y, firstChild.position.z);
            
            // Actualizamos la lista: el que era primero ahora es el último
            children.RemoveAt(0);
            children.Add(firstChild);
        }

        // 2. CHEQUEAR DERECHA (Si vamos a la izquierda/atrás)
        // Recalculamos quiénes son el primero y el último tras el cambio anterior
        firstChild = children[0];
        lastChild = children[children.Count - 1];

        // Si el hijo de la derecha se salió por la derecha de la cámara...
        if (cam.transform.position.x + screenWidthInUnits < lastChild.position.x - buffer)
        {
            // Lo movemos al principio de la fila (a la izquierda del primero)
            lastChild.position = new Vector3(firstChild.position.x - spriteWidth, lastChild.position.y, lastChild.position.z);

            // Actualizamos la lista: el que era último ahora es el primero
            children.RemoveAt(children.Count - 1);
            children.Insert(0, lastChild);
        }
    }
}