using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("--- REFERENCIAS ---")]
    public PlayerController controller;
    public Animator animator;
    public Transform visualTransform; 

    [Header("--- AJUSTES ---")]
    public bool invertFlip = false; 

    [Header("--- NOMBRES DE ESTADOS (Animator) ---")]
    public string animIdle = "Idle";
    public string animRun = "Run";
    public string animJump = "Jump";
    public string animTrick = "Trick"; // <--- ASEGÚRATE DE QUE ESTE NOMBRE EXISTE EN EL ANIMATOR
    public string animCrash = "Crash"; // <--- Y ESTE TAMBIÉN

    private string currentAnim;
    private bool isFacingRight = true;

    private void Start()
    {
        if (controller == null) controller = GetComponent<PlayerController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        
        if (visualTransform == null) 
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            visualTransform = sr != null ? sr.transform : transform;
        }
    }

    private void Update()
    {
        if (controller == null || animator == null) return;

        UpdateFlip();
        UpdateAnimations();
    }

    private void UpdateFlip()
    {
        if (controller.IsGrounded && Mathf.Abs(controller.MoveInput) > 0.1f)
        {
            isFacingRight = (controller.MoveInput > 0);
        }

        Vector3 scale = visualTransform.localScale;
        float directionFactor = isFacingRight ? 1f : -1f;
        if (invertFlip) directionFactor *= -1f;

        scale.x = Mathf.Abs(scale.x) * directionFactor;
        visualTransform.localScale = scale;
    }

    private void UpdateAnimations()
    {
        string targetAnim = animIdle;

        // 1. PRIORIDAD MÁXIMA: GOLPE / CRASH
        if (controller.IsCrashed) 
        {
            targetAnim = animCrash;
        }
        // 2. PRIORIDAD ALTA: TRUCO
        else if (controller.IsDoingTrick) 
        {
            targetAnim = animTrick;
        }
        // 3. PRIORIDAD MEDIA: AIRE
        else if (!controller.IsGrounded) 
        {
            targetAnim = animJump;
        }
        // 4. PRIORIDAD BAJA: MOVIMIENTO
        else if (Mathf.Abs(controller.MoveInput) > 0.1f) 
        {
            targetAnim = animRun;
        }
        
        // Aplicar solo si cambia
        if (currentAnim != targetAnim)
        {
            animator.Play(targetAnim);
            currentAnim = targetAnim;
        }
    }
}