using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RotateToMouse : MonoBehaviour
{
    public float rotationSpeed = 720f; // degrees per second

    private Rigidbody2D rb;
    private Camera cam;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
    }

    void FixedUpdate()
    {
        if (!Input.GetMouseButton(1)) return; // Right Mouse Button

        Vector2 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mouseWorldPos - rb.position;

        // Angle so LOCAL X axis points toward the mouse
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float newAngle = Mathf.MoveTowardsAngle(
            rb.rotation,
            targetAngle,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(newAngle);
    }
}