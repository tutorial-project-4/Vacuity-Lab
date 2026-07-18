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

    public bool CanAttack { get; private set; } = true;
    public bool IsKnockbacking { get; private set; }

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void ReceiveHit(Vector2 damageSourcePosition, float invincibleDuration)
    {
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

    public void OnDeath()
    {
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
        movement.SetControlLocked(true);
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
        CanAttack = true;
        movement.SetControlLocked(false);
        movement.StopVerticalMovement();
        knockbackRoutine = null;
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

    private void OnValidate()
    {
        knockbackDuration = Mathf.Max(0f, knockbackDuration);
        knockbackSpeed = Mathf.Max(0f, knockbackSpeed);
        knockbackUpwardSpeed = Mathf.Max(0f, knockbackUpwardSpeed);
        blinkInterval = Mathf.Max(0.01f, blinkInterval);
    }
}
