using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerAnimationController : MonoBehaviour
{
    private static readonly int SpeedXHash = Animator.StringToHash("SpeedX");
    private static readonly int VelocityYHash = Animator.StringToHash("VelocityY");
    private static readonly int IsGroundHash = Animator.StringToHash("IsGround");
    private static readonly int IsJumpHash = Animator.StringToHash("IsJump");
    private static readonly int IsGlideHash = Animator.StringToHash("IsGlide");
    private static readonly int IsDashHash = Animator.StringToHash("isDash");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HitHash = Animator.StringToHash("Hit");

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private PlayerAttack attack;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Visual")]
    [SerializeField] private bool flipSpriteWithFacing = true;

    [Header("Air Animation")]
    [SerializeField] private float risingThreshold = 0.1f;
    [SerializeField] private float fallingThreshold = -0.1f;
    [SerializeField] private float jumpStartHoldTime = 0.08f;
    [SerializeField] private float jumpStartVelocity = 1f;

    private bool wasAttacking;
    private bool wasKnockbacking;
    private bool wasGrounded;
    private float jumpStartTimer;
    private float animationVelocityY;

    private void Awake()
    {
        CacheComponents();
    }

    private void LateUpdate()
    {
        CacheComponents();

        if (animator == null || movement == null)
        {
            return;
        }

        UpdateAnimationVelocityY();

        animator.SetFloat(SpeedXHash, Mathf.Abs(movement.HorizontalSpeed));
        animator.SetFloat(VelocityYHash, animationVelocityY);
        animator.SetBool(IsGroundHash, movement.IsGrounded);
        animator.SetBool(IsJumpHash, movement.IsJumping);
        animator.SetBool(IsGlideHash, movement.IsGliding);
        animator.SetBool(IsDashHash, movement.IsDashing);
        animator.SetBool(IsDeadHash, health != null && health.IsDead);

        UpdateAttackTrigger();
        UpdateHitTrigger();
        UpdateFacing();
        wasGrounded = movement.IsGrounded;
    }

    private void UpdateAnimationVelocityY()
    {
        float rawVelocityY = movement.VerticalSpeed;

        if (movement.IsGrounded || movement.IsDashing)
        {
            jumpStartTimer = 0f;
            animationVelocityY = 0f;
            return;
        }

        bool justLeftGroundUpward = wasGrounded && rawVelocityY > risingThreshold;
        if (justLeftGroundUpward)
        {
            jumpStartTimer = jumpStartHoldTime;
        }

        if (jumpStartTimer > 0f)
        {
            jumpStartTimer -= Time.deltaTime;
            animationVelocityY = Mathf.Max(rawVelocityY, jumpStartVelocity);
            return;
        }

        if (rawVelocityY > risingThreshold || rawVelocityY < fallingThreshold)
        {
            animationVelocityY = rawVelocityY;
        }
    }

    private void UpdateAttackTrigger()
    {
        bool isAttacking = attack != null && attack.IsAttacking;
        if (isAttacking && !wasAttacking)
        {
            animator.SetTrigger(AttackHash);
        }

        wasAttacking = isAttacking;
    }

    private void UpdateHitTrigger()
    {
        bool isKnockbacking = controller != null && controller.IsKnockbacking;
        if (isKnockbacking && !wasKnockbacking)
        {
            animator.SetTrigger(HitHash);
        }

        wasKnockbacking = isKnockbacking;
    }

    private void UpdateFacing()
    {
        if (!flipSpriteWithFacing || spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.flipX = movement.FacingDirection < 0;
    }

    private void CacheComponents()
    {
        if (animator == null)
        {
            animator = FindVisualAnimator();
        }

        if (movement == null)
        {
            movement = GetComponent<PlayerMovement>();
        }

        if (controller == null)
        {
            controller = GetComponent<PlayerController>();
        }

        if (health == null)
        {
            health = GetComponent<PlayerHealth>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = FindVisualSpriteRenderer();
        }

        if (attack == null)
        {
#if UNITY_2023_1_OR_NEWER
            attack = FindFirstObjectByType<PlayerAttack>();
#else
            attack = FindObjectOfType<PlayerAttack>();
#endif
        }
    }

    private Animator FindVisualAnimator()
    {
        Transform visual = GetVisualRoot();
        if (visual != null && visual.TryGetComponent(out Animator visualAnimator))
        {
            return visualAnimator;
        }

        return GetComponent<Animator>();
    }

    private SpriteRenderer FindVisualSpriteRenderer()
    {
        Transform visual = GetVisualRoot();
        if (visual != null && visual.TryGetComponent(out SpriteRenderer visualRenderer))
        {
            return visualRenderer;
        }

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].transform != transform)
            {
                return renderers[i];
            }
        }

        return GetComponent<SpriteRenderer>();
    }

    private Transform GetVisualRoot()
    {
        if (visualRoot == null)
        {
            visualRoot = transform.Find("PlayerVisual");
        }

        return visualRoot;
    }

    private void OnValidate()
    {
        risingThreshold = Mathf.Max(0f, risingThreshold);
        fallingThreshold = Mathf.Min(0f, fallingThreshold);
        jumpStartHoldTime = Mathf.Max(0f, jumpStartHoldTime);
        jumpStartVelocity = Mathf.Max(risingThreshold, jumpStartVelocity);
    }
}
