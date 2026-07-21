using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float moveStep = 0.01f;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 6.5f;
    [SerializeField] private float gravity = 18f;
    [SerializeField] private float fallGravityMultiplier = 1.5f;
    [SerializeField] private float lowJumpGravityMultiplier = 2f;
    [SerializeField] private float maxFallSpeed = 10f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Glide")]
    [SerializeField] private float glideDuration = 2f;
    [SerializeField] private float glideFallSpeed = 2f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.6f;

    [Header("Collision")]
    [SerializeField] private LayerMask solidLayer;
    [SerializeField] private int maxCollisionChecksPerMove = 200;

    private BoxCollider2D bodyCollider;
    private float xRemainder;
    private float yRemainder;
    private float ySpeed;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float glideTimer;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;
    private bool canGlide;
    private bool canAirDash = true;

    public Vector2 AttackDirection { get; private set; } = Vector2.right;
    public int FacingDirection { get; private set; } = 1;
    public bool IsGrounded { get; private set; }
    public bool IsControlLocked { get; private set; }
    public bool IsGliding { get; private set; }
    public bool IsDashing { get; private set; }

    private void Awake()
    {
        bodyCollider = GetComponent<BoxCollider2D>();
        moveStep = Mathf.Max(moveStep, 0.0001f);

        if (solidLayer.value == 0)
        {
            solidLayer = LayerMask.GetMask("Solid");
        }

        ResetGlide();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (IsControlLocked)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        dashCooldownTimer -= deltaTime;
        float horizontal = 0f;

        if (keyboard.aKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            horizontal += 1f;
        }

        if (horizontal != 0f)
        {
            FacingDirection = horizontal > 0f ? 1 : -1;
            AttackDirection = new Vector2(FacingDirection, 0f);
        }

        if (keyboard.wKey.isPressed)
        {
            AttackDirection = Vector2.up;
        }
        else if (keyboard.sKey.isPressed)
        {
            AttackDirection = Vector2.down;
        }

        IsGrounded = CheckGrounded();

        if (IsGrounded)
        {
            coyoteTimer = coyoteTime;
            canAirDash = true;
            ResetGlide();

            if (ySpeed < 0f)
            {
                ySpeed = 0f;
            }
        }
        else
        {
            coyoteTimer -= deltaTime;
        }

        if (CanStartDash(keyboard))
        {
            StartDash(horizontal);
        }

        if (IsDashing)
        {
            UpdateDash(deltaTime);
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= deltaTime;
        }

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            ySpeed = jumpSpeed;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            IsGliding = false;
            canGlide = true;
            glideTimer = glideDuration;
        }

        if (CanStartGlide(keyboard))
        {
            IsGliding = true;
            jumpBufferTimer = 0f;
        }

        if (IsGliding)
        {
            ySpeed = -glideFallSpeed;
            glideTimer -= deltaTime;

            if (!keyboard.spaceKey.isPressed || glideTimer <= 0f)
            {
                EndGlide();
            }
        }
        else
        {
            float gravityMultiplier = 1f;
            if (ySpeed < 0f)
            {
                gravityMultiplier = fallGravityMultiplier;
            }
            else if (ySpeed > 0f && !keyboard.spaceKey.isPressed)
            {
                gravityMultiplier = lowJumpGravityMultiplier;
            }

            ySpeed = Mathf.Max(ySpeed - gravity * gravityMultiplier * deltaTime, -maxFallSpeed);
        }

        MoveX(horizontal * moveSpeed * deltaTime, null);
        MoveY(ySpeed * deltaTime, OnVerticalCollide);
    }

    public void MoveX(float amount, Action onCollide)
    {
        xRemainder += amount;
        int move = Mathf.RoundToInt(xRemainder / moveStep);

        if (move == 0)
        {
            return;
        }

        xRemainder -= move * moveStep;
        int sign = move > 0 ? 1 : -1;
        int remainingChecks = maxCollisionChecksPerMove;

        while (move != 0 && remainingChecks > 0)
        {
            remainingChecks--;
            Vector2 nextPosition = (Vector2)transform.position + new Vector2(sign * moveStep, 0f);

            if (!CollideAt(nextPosition))
            {
                transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
                move -= sign;
            }
            else
            {
                xRemainder = 0f;
                onCollide?.Invoke();
                break;
            }
        }

        if (move != 0 && remainingChecks <= 0)
        {
            xRemainder = 0f;
        }
    }

    public void MoveY(float amount, Action onCollide)
    {
        yRemainder += amount;
        int move = Mathf.RoundToInt(yRemainder / moveStep);

        if (move == 0)
        {
            return;
        }

        yRemainder -= move * moveStep;
        int sign = move > 0 ? 1 : -1;
        int remainingChecks = maxCollisionChecksPerMove;

        while (move != 0 && remainingChecks > 0)
        {
            remainingChecks--;
            Vector2 nextPosition = (Vector2)transform.position + new Vector2(0f, sign * moveStep);

            if (!CollideAt(nextPosition))
            {
                transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
                move -= sign;
            }
            else
            {
                yRemainder = 0f;
                onCollide?.Invoke();
                break;
            }
        }

        if (move != 0 && remainingChecks <= 0)
        {
            yRemainder = 0f;
        }
    }

    private bool CollideAt(Vector2 position)
    {
        Vector2 checkCenter = position + Vector2.Scale(bodyCollider.offset, transform.lossyScale);
        Vector2 checkSize = Vector2.Scale(bodyCollider.size, transform.lossyScale);
        return Physics2D.OverlapBox(checkCenter, checkSize, 0f, solidLayer) != null;
    }

    private bool CheckGrounded()
    {
        return CollideAt((Vector2)transform.position + Vector2.down * moveStep);
    }

    private void OnVerticalCollide()
    {
        ySpeed = 0f;
        IsGliding = false;
    }

    public void SetControlLocked(bool isLocked)
    {
        IsControlLocked = isLocked;

        if (isLocked)
        {
            xRemainder = 0f;
            yRemainder = 0f;

            if (IsDashing)
            {
                EndDash();
            }

            if (IsGliding)
            {
                EndGlide();
            }
        }
    }

    public void StopVerticalMovement()
    {
        ySpeed = 0f;
        yRemainder = 0f;
        IsGliding = false;
    }

    public void ResetMovementState(bool lockControl = false)
    {
        xRemainder = 0f;
        yRemainder = 0f;
        ySpeed = 0f;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        glideTimer = glideDuration;
        dashTimer = 0f;
        dashCooldownTimer = 0f;
        dashDirection = Vector2.zero;
        canGlide = true;
        canAirDash = true;
        IsGliding = false;
        IsDashing = false;
        IsGrounded = false;
        IsControlLocked = lockControl;
    }

    private bool CanStartDash(Keyboard keyboard)
    {
        return !IsDashing
            && dashCooldownTimer <= 0f
            && (IsGrounded || canAirDash)
            && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame);
    }

    private void StartDash(float horizontal)
    {
        dashDirection = horizontal != 0f
            ? new Vector2(Mathf.Sign(horizontal), 0f)
            : new Vector2(FacingDirection, 0f);

        if (IsGliding)
        {
            EndGlide();
        }

        IsDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        ySpeed = 0f;
        yRemainder = 0f;
        jumpBufferTimer = 0f;

        if (!IsGrounded)
        {
            canAirDash = false;
        }
    }

    private void UpdateDash(float deltaTime)
    {
        MoveX(dashDirection.x * dashSpeed * deltaTime, EndDash);

        dashTimer -= deltaTime;
        if (dashTimer <= 0f)
        {
            EndDash();
        }
    }

    private void EndDash()
    {
        IsDashing = false;
        dashTimer = 0f;
    }

    private bool CanStartGlide(Keyboard keyboard)
    {
        return canGlide
            && !IsGrounded
            && ySpeed < 0f
            && glideTimer > 0f
            && keyboard.spaceKey.isPressed;
    }

    private void EndGlide()
    {
        IsGliding = false;
        canGlide = false;
    }

    private void ResetGlide()
    {
        IsGliding = false;
        canGlide = true;
        glideTimer = glideDuration;
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D boxCollider = bodyCollider != null ? bodyCollider : GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Vector2 gizmoCenter = (Vector2)transform.position + Vector2.Scale(boxCollider.offset, transform.lossyScale);
        Vector2 gizmoSize = Vector2.Scale(boxCollider.size, transform.lossyScale);
        Gizmos.DrawWireCube(gizmoCenter, gizmoSize);
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        moveStep = Mathf.Max(0.0001f, moveStep);
        jumpSpeed = Mathf.Max(0f, jumpSpeed);
        gravity = Mathf.Max(0f, gravity);
        fallGravityMultiplier = Mathf.Max(1f, fallGravityMultiplier);
        lowJumpGravityMultiplier = Mathf.Max(1f, lowJumpGravityMultiplier);
        maxFallSpeed = Mathf.Max(0f, maxFallSpeed);
        coyoteTime = Mathf.Max(0f, coyoteTime);
        jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
        glideDuration = Mathf.Max(0f, glideDuration);
        glideFallSpeed = Mathf.Max(0f, glideFallSpeed);
        dashSpeed = Mathf.Max(0f, dashSpeed);
        dashDuration = Mathf.Max(0f, dashDuration);
        dashCooldown = Mathf.Max(0f, dashCooldown);
        maxCollisionChecksPerMove = Mathf.Max(1, maxCollisionChecksPerMove);
    }
}
