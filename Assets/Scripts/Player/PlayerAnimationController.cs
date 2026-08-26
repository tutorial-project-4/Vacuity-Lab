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
    private static readonly int IdleStateHash = Animator.StringToHash("idle");
    private static readonly int DeathStateHash = Animator.StringToHash("Death");

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
    [SerializeField] private float groundedAnimationGraceTime = 0.08f;
    [SerializeField] private float attackAnimationLockTime = 0.35f;

    private bool wasAttacking;
    private bool wasKnockbacking;
    private bool wasGrounded;
    private float jumpStartTimer;
    private float groundedAnimationGraceTimer;
    private float attackAnimationLockTimer;
    private float animationVelocityY;
    private bool animationGrounded;
    private AnimatorUpdateMode originalAnimatorUpdateMode;
    private bool hasOriginalAnimatorUpdateMode;
    private bool wasDead;
    private PlayerAttack subscribedAttack;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        CacheComponents();
        SubscribeToAttack();
    }

    private void OnDisable()
    {
        UnsubscribeFromAttack();
    }

    private void LateUpdate()
    {
        CacheComponents();
        SubscribeToAttack();

        if (animator == null || movement == null)
        {
            return;
        }

        bool isAttacking = attack != null && attack.IsAttacking;
        bool isDead = health != null && health.IsDead;
        UpdateAnimatorTimeMode(isDead);

        if (!wasDead && isDead)
        {
            PlayDeathAnimation();
        }

        if (wasDead && !isDead)
        {
            ResetToIdleAnimation();
        }

        UpdateAttackLock(isAttacking);
        UpdateAnimationGrounded();
        UpdateAnimationVelocityY();

        bool holdAttackAnimation = attackAnimationLockTimer > 0f;

        animator.SetFloat(SpeedXHash, Mathf.Abs(movement.HorizontalSpeed));
        animator.SetFloat(VelocityYHash, animationVelocityY);
        animator.SetBool(IsGroundHash, animationGrounded);
        animator.SetBool(IsJumpHash, !holdAttackAnimation && !animationGrounded && movement.IsJumping);
        animator.SetBool(IsGlideHash, movement.IsGliding);
        animator.SetBool(IsDashHash, movement.IsDashing);
        animator.SetBool(IsDeadHash, isDead);

        UpdateHitTrigger();
        UpdateFacing();
        wasGrounded = movement.IsGrounded;
        wasDead = isDead;
    }

    private void UpdateAnimatorTimeMode(bool isDead)
    {
        if (animator == null)
        {
            return;
        }

        if (!hasOriginalAnimatorUpdateMode)
        {
            originalAnimatorUpdateMode = animator.updateMode;
            hasOriginalAnimatorUpdateMode = true;
        }

        animator.updateMode = isDead ? AnimatorUpdateMode.UnscaledTime : originalAnimatorUpdateMode;
    }

    private void PlayDeathAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(HitHash);
        animator.SetBool(IsDeadHash, true);
        animator.SetBool(IsDashHash, false);
        animator.SetBool(IsJumpHash, false);
        animator.SetBool(IsGlideHash, false);
        animator.Play(DeathStateHash, 0, 0f);
        animator.Update(0f);
    }

    private void ResetToIdleAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(AttackHash);
        animator.ResetTrigger(HitHash);
        animator.SetBool(IsDeadHash, false);
        animator.SetBool(IsDashHash, false);
        animator.SetBool(IsJumpHash, false);
        animator.SetBool(IsGlideHash, false);
        animator.Play(IdleStateHash, 0, 0f);
        animator.Update(0f);
    }

    private void UpdateAttackLock(bool isAttacking)
    {
        if (isAttacking && !wasAttacking)
        {
            attackAnimationLockTimer = attackAnimationLockTime;
        }
        else if (attackAnimationLockTimer > 0f)
        {
            attackAnimationLockTimer -= Time.deltaTime;
        }

        wasAttacking = isAttacking;
    }

    private void UpdateAnimationGrounded()
    {
        bool shouldSkipGroundGrace = movement.IsJumping && movement.VerticalSpeed > risingThreshold;
        if (movement.IsGrounded)
        {
            groundedAnimationGraceTimer = groundedAnimationGraceTime;
            animationGrounded = true;
            return;
        }

        if (shouldSkipGroundGrace)
        {
            groundedAnimationGraceTimer = 0f;
            animationGrounded = false;
            return;
        }

        if (groundedAnimationGraceTimer > 0f)
        {
            groundedAnimationGraceTimer -= Time.deltaTime;
            animationGrounded = true;
            return;
        }

        animationGrounded = false;
    }

    private void UpdateAnimationVelocityY()
    {
        float rawVelocityY = movement.VerticalSpeed;

        if (animationGrounded || movement.IsDashing || attackAnimationLockTimer > 0f)
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

    private void SubscribeToAttack()
    {
        if (subscribedAttack == attack)
        {
            return;
        }

        UnsubscribeFromAttack();
        subscribedAttack = attack;

        if (subscribedAttack != null)
        {
            subscribedAttack.AttackStarted += HandleAttackStarted;

            if (subscribedAttack.IsAttacking)
            {
                HandleAttackStarted();
            }
        }
    }

    private void UnsubscribeFromAttack()
    {
        if (subscribedAttack != null)
        {
            subscribedAttack.AttackStarted -= HandleAttackStarted;
            subscribedAttack = null;
        }
    }

    private void HandleAttackStarted()
    {
        CacheComponents();
        if (animator == null || (health != null && health.IsDead))
        {
            return;
        }

        attackAnimationLockTimer = attackAnimationLockTime;
        animator.ResetTrigger(AttackHash);
        animator.SetTrigger(AttackHash);
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
            animator = PlayerVisualResolver.FindAnimator(this, GetVisualRoot());
            hasOriginalAnimatorUpdateMode = false;
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
            spriteRenderer = PlayerVisualResolver.FindSpriteRenderer(this, GetVisualRoot());
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

    private Transform GetVisualRoot()
    {
        visualRoot = PlayerVisualResolver.FindVisualRoot(transform, visualRoot);
        return visualRoot;
    }

    private void OnValidate()
    {
        risingThreshold = Mathf.Max(0f, risingThreshold);
        fallingThreshold = Mathf.Min(0f, fallingThreshold);
        jumpStartHoldTime = Mathf.Max(0f, jumpStartHoldTime);
        jumpStartVelocity = Mathf.Max(risingThreshold, jumpStartVelocity);
        groundedAnimationGraceTime = Mathf.Max(0f, groundedAnimationGraceTime);
        attackAnimationLockTime = Mathf.Max(0f, attackAnimationLockTime);
    }
}
