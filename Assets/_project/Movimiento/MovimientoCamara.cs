using UnityEngine;

public class MovimientoCamara : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform target;
    public Vector3 offset;
    

    // Update is called once per frame
    void Update()
    {
        transform.position = target.position + offset; 
        if (offset.z == 0f)
            offset.z = -10f;
    }
}
