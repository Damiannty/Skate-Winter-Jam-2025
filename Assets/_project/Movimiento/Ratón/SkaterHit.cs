using UnityEngine;

public class SkaterHit : MonoBehaviour
{
    [SerializeField] private Transform skaterTransform;

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Player hit!");
    }
}
