using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IPlayerDashDamageable
{
    void TakeDamage(int damage);
}

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerWallPhaseDash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerHealth health;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 4f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 1.5f;

    [Header("Damage")]
    [SerializeField] private int damage = 20;
    [SerializeField] private int bossContactDamage = 1;
    [SerializeField] private LayerMask targetLayer;

    [Header("Wall Resolve")]
    [SerializeField] private LayerMask dashPassableLayer;
    [SerializeField] private float resolveStep = 0.05f;
    [SerializeField] private float maxExitResolveDistance = 2f;

    private readonly Collider2D[] overlapBuffer = new Collider2D[32];
    private readonly HashSet<int> damagedTargets = new HashSet<int>();
    private Coroutine dashRoutine;
    private float cooldownTimer;
    private bool canAirWallPhaseDash = true;
    private bool hasInvincibleOverride;
    private bool warnedOverlapBufferFull;
    private bool warnedResolveDistanceExceeded;

    public event System.Action DashStarted;

    public bool IsWallPhaseDashing { get; private set; }
    public float CooldownRatio => dashCooldown > 0f ? Mathf.Clamp01(cooldownTimer / dashCooldown) : 0f;
    public bool IsAvailable => CanStartDash();

    private void Awake()
    {
        CacheComponents();
        CacheDefaultLayers();
    }

    private void Update()
    {
        CacheComponents();

        cooldownTimer -= Time.deltaTime;

        if (movement != null && movement.IsGrounded && !IsWallPhaseDashing)
        {
            canAirWallPhaseDash = true;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.qKey.wasPressedThisFrame || !CanStartDash())
        {
            return;
        }

        float horizontal = 0f;
        if (keyboard.aKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            horizontal += 1f;
        }

        Vector2 direction = horizontal != 0f
            ? new Vector2(Mathf.Sign(horizontal), 0f)
            : new Vector2(movement.FacingDirection, 0f);

        dashRoutine = StartCoroutine(DashRoutine(direction));
    }

    private void OnDisable()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        CleanupDashState();
    }

    public void ResetState()
    {
        if (dashRoutine != null)
        {
            StopCoroutine(dashRoutine);
            dashRoutine = null;
        }

        CleanupDashState();
        cooldownTimer = 0f;
        canAirWallPhaseDash = true;
        damagedTargets.Clear();
    }

    private bool CanStartDash()
    {
        return movement != null
            && health != null
            && !health.IsDead
            && !movement.IsControlLocked
            && !movement.IsInputLocked
            && !movement.IsDashing
            && !IsWallPhaseDashing
            && cooldownTimer <= 0f
            && (movement.IsGrounded || canAirWallPhaseDash);
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        bool shouldApplyBossContactDamage = false;
        try
        {
            BeginDashState();

            Vector2 startPosition = transform.position;
            DamageTargetsAtPosition(startPosition);
            DamageTargetsAlongPath(startPosition, direction);

            float elapsed = 0f;
            bool blocked = false;
            while (elapsed < dashDuration && !blocked)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / dashDuration);
                Vector2 targetPosition = startPosition + direction * dashDistance * progress;
                Vector2 delta = targetPosition - (Vector2)transform.position;

                if (Mathf.Abs(delta.x) > 0f)
                {
                    movement.MoveX(delta.x, () => blocked = true, CanPassDuringWallPhaseDash);
                }

                DamageTargetsAtPosition(transform.position);
                yield return null;
            }

            ResolveDashPassableWallExit(direction);
            shouldApplyBossContactDamage = true;
        }
        finally
        {
            CleanupDashState();
            dashRoutine = null;
        }

        if (shouldApplyBossContactDamage)
        {
            ApplyBossContactDamageIfNeeded();
        }
    }

    private void BeginDashState()
    {
        IsWallPhaseDashing = true;
        cooldownTimer = dashCooldown;
        damagedTargets.Clear();
        DashStarted?.Invoke();

        if (!movement.IsGrounded)
        {
            canAirWallPhaseDash = false;
        }

        movement.SetControlLocked(true);
        movement.StopVerticalMovement();
        health.AddInvincibleOverride(this);
        hasInvincibleOverride = true;
    }

    private bool CanPassDuringWallPhaseDash(Collider2D collider)
    {
        TerrainDescriptor descriptor = TerrainDescriptor.From(collider);
        return descriptor != null && descriptor.AllowsWallDashPass && !descriptor.IsAbsoluteBoundary;
    }

    private void DamageTargetsAlongPath(Vector2 startPosition, Vector2 direction)
    {
        Vector2 center = movement.GetColliderCenterAt(startPosition);
        Vector2 size = movement.GetColliderSize();
        RaycastHit2D[] hits = Physics2D.BoxCastAll(center, size, 0f, direction, dashDistance, targetLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            TryDamageTarget(hitCollider);
        }
    }

    private void DamageTargetsAtPosition(Vector2 position)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayer);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int overlapCount = movement.OverlapBodyAt(position, filter, overlapBuffer);
        WarnIfOverlapBufferFull(overlapCount);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D overlap = overlapBuffer[i];
            if (overlap != null)
            {
                TryDamageTarget(overlap);
            }
        }
    }

    private void TryDamageTarget(Collider2D hitCollider)
    {
        BossHealth bossHealth = hitCollider.GetComponentInParent<BossHealth>();
        if (bossHealth != null)
        {
            int bossTargetId = bossHealth.GetInstanceID();
            if (!damagedTargets.Add(bossTargetId))
            {
                return;
            }

            bossHealth.TakeDamage(damage);
            return;
        }

        IPlayerDashDamageable damageable = FindDashDamageable(hitCollider);
        if (damageable == null)
        {
            return;
        }

        int targetId = damageable is Component component
            ? component.GetInstanceID()
            : damageable.GetHashCode();

        if (!damagedTargets.Add(targetId))
        {
            return;
        }

        damageable.TakeDamage(damage);
    }

    private IPlayerDashDamageable FindDashDamageable(Collider2D hitCollider)
    {
        MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IPlayerDashDamageable damageable)
            {
                return damageable;
            }
        }

        return null;
    }

    private void ResolveDashPassableWallExit(Vector2 direction)
    {
        float resolvedDistance = 0f;

        while (IsOverlappingDashPassableWall((Vector2)transform.position) && resolvedDistance < maxExitResolveDistance)
        {
            Vector2 nextPosition = (Vector2)transform.position + direction * resolveStep;
            if (movement.CollideAtPosition(nextPosition, CanPassDuringWallPhaseDash))
            {
                break;
            }

            transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
            resolvedDistance += resolveStep;
        }

        if (!warnedResolveDistanceExceeded && IsOverlappingDashPassableWall((Vector2)transform.position))
        {
            Debug.LogWarning("[PlayerWallPhaseDash] Could not fully resolve the player out of a dash-passable wall.", this);
            warnedResolveDistanceExceeded = true;
        }
    }

    private bool IsOverlappingDashPassableWall(Vector2 position)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(dashPassableLayer);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int overlapCount = movement.OverlapBodyAt(position, filter, overlapBuffer);
        WarnIfOverlapBufferFull(overlapCount);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D overlap = overlapBuffer[i];
            if (overlap != null && CanPassDuringWallPhaseDash(overlap))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyBossContactDamageIfNeeded()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayer);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int overlapCount = movement.OverlapBodyAt(transform.position, filter, overlapBuffer);
        WarnIfOverlapBufferFull(overlapCount);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D overlap = overlapBuffer[i];
            BossHealth bossHealth = overlap != null ? overlap.GetComponentInParent<BossHealth>() : null;
            if (bossHealth == null)
            {
                continue;
            }

            Vector2 damageSourcePosition = overlap.bounds.center;
            health.TakeDamage(bossContactDamage, damageSourcePosition);
            return;
        }
    }

    private void CleanupDashState()
    {
        RemoveInvincibleOverride();

        if (movement != null && IsWallPhaseDashing)
        {
            movement.SetControlLocked(false);
        }

        IsWallPhaseDashing = false;
    }

    private void WarnIfOverlapBufferFull(int overlapCount)
    {
        if (warnedOverlapBufferFull || overlapCount < overlapBuffer.Length)
        {
            return;
        }

        Debug.LogWarning("[PlayerWallPhaseDash] Overlap buffer is full. Some dash targets may be skipped.", this);
        warnedOverlapBufferFull = true;
    }

    private void RemoveInvincibleOverride()
    {
        if (!hasInvincibleOverride || health == null)
        {
            return;
        }

        health.RemoveInvincibleOverride(this);
        hasInvincibleOverride = false;
    }

    private void CacheComponents()
    {
        if (movement == null)
        {
            movement = GetComponent<PlayerMovement>();
        }

        if (health == null)
        {
            health = GetComponent<PlayerHealth>();
        }
    }

    private void CacheDefaultLayers()
    {
        if (targetLayer.value == 0)
        {
            targetLayer = LayerMask.GetMask("Boss");
        }

        if (dashPassableLayer.value == 0)
        {
            dashPassableLayer = LayerMask.GetMask("DashPassableWall");
        }
    }

    private void OnValidate()
    {
        dashDistance = Mathf.Max(0f, dashDistance);
        dashDuration = Mathf.Max(0.01f, dashDuration);
        dashCooldown = Mathf.Max(0f, dashCooldown);
        damage = Mathf.Max(0, damage);
        bossContactDamage = Mathf.Max(0, bossContactDamage);
        resolveStep = Mathf.Max(0.001f, resolveStep);
        maxExitResolveDistance = Mathf.Max(0f, maxExitResolveDistance);
    }
}
