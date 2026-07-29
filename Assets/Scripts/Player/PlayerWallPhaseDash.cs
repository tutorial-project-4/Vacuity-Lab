using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    public bool IsWallPhaseDashing { get; private set; }

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

    private bool CanStartDash()
    {
        return movement != null
            && health != null
            && !health.IsDead
            && !movement.IsControlLocked
            && !movement.IsDashing
            && !IsWallPhaseDashing
            && cooldownTimer <= 0f
            && (movement.IsGrounded || canAirWallPhaseDash);
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        IsWallPhaseDashing = true;
        cooldownTimer = dashCooldown;
        damagedTargets.Clear();

        if (!movement.IsGrounded)
        {
            canAirWallPhaseDash = false;
        }

        movement.SetControlLocked(true);
        movement.StopVerticalMovement();
        health.AddInvincibleOverride();
        hasInvincibleOverride = true;

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
        RemoveInvincibleOverride();
        FinishDash();
        ApplyBossContactDamageIfNeeded();
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
        if (bossHealth == null)
        {
            TrySendGenericDamage(hitCollider);
            return;
        }

        int targetId = bossHealth.GetInstanceID();
        if (!damagedTargets.Add(targetId))
        {
            return;
        }

        bossHealth.TakeDamage(damage);
    }

    private void TrySendGenericDamage(Collider2D hitCollider)
    {
        Transform targetRoot = hitCollider.attachedRigidbody != null
            ? hitCollider.attachedRigidbody.transform
            : hitCollider.transform.root;

        int targetId = targetRoot.GetInstanceID();
        if (!damagedTargets.Add(targetId))
        {
            return;
        }

        targetRoot.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
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
    }

    private bool IsOverlappingDashPassableWall(Vector2 position)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(dashPassableLayer);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int overlapCount = movement.OverlapBodyAt(position, filter, overlapBuffer);
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

    private void FinishDash()
    {
        movement.SetControlLocked(false);
        IsWallPhaseDashing = false;
        dashRoutine = null;
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

    private void RemoveInvincibleOverride()
    {
        if (!hasInvincibleOverride || health == null)
        {
            return;
        }

        health.RemoveInvincibleOverride();
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
