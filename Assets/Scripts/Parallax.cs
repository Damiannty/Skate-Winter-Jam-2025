using UnityEngine;

public class InfiniteParallax : MonoBehaviour
{
    [Header("Configuración General")]
    public Camera cam;
    
    [Header("Configuración Ejes")]
    [Range(0f, 1f)] public float parallaxFactorX;
    [Range(-1f, 1f)] public float parallaxFactorY;

    private float length;       // Longitud TOTAL de la tira de imágenes
    private float startPosX;    
    private float startPosY;    

    void Start()
    {
        if (cam == null) cam = Camera.main;

        startPosX = transform.position.x;
        startPosY = transform.position.y;

        // --- MEJORA: CALCULAR ANCHO TOTAL DE TODOS LOS HIJOS ---
        // Esto permite poner 3 sprites juntos dentro de este objeto y que el script
        // sepa que ahora la imagen es el triple de larga.
        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        
        if (sprites.Length > 0)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;

            foreach (var spr in sprites)
            {
                // Buscamos dónde empieza el sprite más a la izquierda y dónde termina el de la derecha
                if (spr.bounds.min.x < minX) minX = spr.bounds.min.x;
                if (spr.bounds.max.x > maxX) maxX = spr.bounds.max.x;
            }
            
            length = maxX - minX; // El ancho total real
        }
        else
        {
            Debug.LogError("¡No hay Sprites dentro de " + gameObject.name + "!");
        }
    }

    void LateUpdate()
    {
        float temp = (cam.transform.position.x * (1 - parallaxFactorX));
        float distX = (cam.transform.position.x * parallaxFactorX);
        float distY = (cam.transform.position.y - startPosY) * parallaxFactorY;

        transform.position = new Vector3(startPosX + distX, startPosY + distY, transform.position.z);

        // El salto se produce cuando nos pasamos de la longitud
        if (temp > startPosX + length) startPosX += length;
        else if (temp < startPosX - length) startPosX -= length;
    }
}