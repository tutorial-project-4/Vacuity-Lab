using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    [Header("Hit Reaction")]
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float knockbackSpeed = 5f;
    [SerializeField] private float knockbackUpwardSpeed = 1.5f;
    [SerializeField] private float blinkInterval = 0.08f;

    private PlayerMovement movement;
    private SpriteRenderer spriteRenderer;
    private Coroutine knockbackRoutine;
    private Coroutine blinkRoutine;
    private bool cutsceneLocked;

    public bool CanAttack { get; private set; } = true;
    public bool IsKnockbacking { get; private set; }

    private void Awake()
    {
        CacheComponents();
    }

    public void ReceiveHit(Vector2 damageSourcePosition, float invincibleDuration)
    {
        CacheComponents();

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
        }

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
        }

        Vector2 knockbackDirection = ((Vector2)transform.position - damageSourcePosition).normalized;
        if (knockbackDirection.sqrMagnitude <= 0.0001f)
        {
            knockbackDirection = new Vector2(-movement.FacingDirection, 0f);
        }

        knockbackDirection.y = Mathf.Max(knockbackDirection.y, 0.35f);
        knockbackDirection.Normalize();

        knockbackRoutine = StartCoroutine(KnockbackRoutine(knockbackDirection));
        blinkRoutine = StartCoroutine(BlinkRoutine(invincibleDuration));
    }

    public void ReceiveKnockback(Vector2 displacement, float duration)
    {
        CacheComponents();
        if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
        knockbackRoutine = StartCoroutine(ForcedMoveRoutine(displacement, duration));
    }

    /// 보스 페이즈 전환 컷신(#10) 등 연출 중 조작 잠금 — 이동·점프·대시(ControlLock)와 공격(CanAttack)을 함께 막는다.
    public void SetCutsceneLock(bool locked)
    {
        CacheComponents();
        cutsceneLocked = locked;

        if (locked)
        {
            StopHitReaction();
            IsKnockbacking = false;
            SetSpriteVisible(true);
            movement.StopVerticalMovement();
        }

        CanAttack = !locked;
        movement.SetControlLocked(locked);
    }

    public void OnDeath()
    {
        CacheComponents();
        CanAttack = false;

        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        IsKnockbacking = false;
        movement.ResetMovementState(true);
        SetSpriteVisible(true);
    }

    public void ResetState()
    {
        CacheComponents();
        StopHitReaction();
        CanAttack = true;
        IsKnockbacking = false;
        movement.ResetMovementState(false);
        SetSpriteVisible(true);
    }

    private IEnumerator KnockbackRoutine(Vector2 direction)
    {
        IsKnockbacking = true;
        CanAttack = false;
        movement.SetControlLocked(true);
        movement.StopVerticalMovement();

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            float deltaTime = Time.deltaTime;
            Vector2 velocity = new Vector2(
                direction.x * knockbackSpeed,
                direction.y * knockbackUpwardSpeed
            );

            movement.MoveX(velocity.x * deltaTime, null);
            movement.MoveY(velocity.y * deltaTime, null);

            elapsed += deltaTime;
            yield return null;
        }

        IsKnockbacking = false;
        CanAttack = !cutsceneLocked;                 // 넉백 종료가 컷신 잠금을 풀지 않도록
        movement.SetControlLocked(cutsceneLocked);
        movement.StopVerticalMovement();
        knockbackRoutine = null;
    }

    private IEnumerator ForcedMoveRoutine(Vector2 displacement, float duration)
    {
        IsKnockbacking = true;
        CanAttack = false;
        movement.SetControlLocked(true);
        movement.StopVerticalMovement();

        Vector2 start = transform.position;
        float elapsed = 0f;
        bool blocked = false;
        while (elapsed < duration && !blocked)
        {
            elapsed += Time.deltaTime;
            Vector2 target = start + displacement * Mathf.Clamp01(elapsed / duration);
            movement.MoveX(target.x - transform.position.x, () => blocked = true);
            if (!blocked) movement.MoveY(target.y - transform.position.y, () => blocked = true);
            yield return null;
        }

        IsKnockbacking = false;
        CanAttack = !cutsceneLocked;
        movement.SetControlLocked(cutsceneLocked);
        movement.StopVerticalMovement();
        knockbackRoutine = null;
    }

    private void StopHitReaction()
    {
        if (knockbackRoutine != null)
        {
            StopCoroutine(knockbackRoutine);
            knockbackRoutine = null;
        }

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
    }

    private IEnumerator BlinkRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            SetSpriteVisible(false);
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;

            SetSpriteVisible(true);
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        SetSpriteVisible(true);
        blinkRoutine = null;
    }

    private void SetSpriteVisible(bool isVisible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = isVisible;
        }
    }

    private void CacheComponents()
    {
        if (movement == null)
        {
            movement = GetComponent<PlayerMovement>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = PlayerVisualResolver.FindSpriteRenderer(this);
        }
    }

    private void OnValidate()
    {
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        knockbackSpeed = Mathf.Max(0f, knockbackSpeed);
        knockbackUpwardSpeed = Mathf.Max(0f, knockbackUpwardSpeed);
        blinkInterval = Mathf.Max(0.01f, blinkInterval);
    }
}
