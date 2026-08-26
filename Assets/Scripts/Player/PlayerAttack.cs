using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerController playerController;
    [Tooltip("Sword 콜라이더는 공격 중에만 활성화됩니다. 평소에는 이동/피격 판정과 겹치지 않도록 비활성 상태로 둡니다.")]
    [SerializeField] private BoxCollider2D swordCollider;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform attackOrigin;

    [Header("Attack Effect")]
    [SerializeField] private Animator attackEffectAnimator;
    [SerializeField] private SpriteRenderer attackEffectRenderer;
    [SerializeField] private string attackEffectTriggerName = "Attack";
    [SerializeField] private string attackEffectStateName = "Effect";
    [SerializeField] private float attackEffectVisibleDuration = 0.25f;

    [Header("Attack")]
    [SerializeField] private int damage = 20;
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private float activeDuration = 0.08f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Hitbox")]
    [SerializeField] private float attackOffsetDistance = 1.6f;
    [SerializeField] private Vector2 hitboxSize = new Vector2(2.2f, 3f);
    [SerializeField] private float hitboxAngleOffset;

    private readonly Collider2D[] hitBuffer = new Collider2D[16];
    private readonly HashSet<object> damagedTargets = new HashSet<object>();
    private float cooldownTimer;
    private Coroutine attackRoutine;
    private Coroutine effectRoutine;
    private bool isAttacking;

    public bool IsAttacking => isAttacking;
    public event Action AttackStarted;

    private void Awake()
    {
        CacheComponents();

        if (targetLayer.value == 0)
        {
            targetLayer = LayerMask.GetMask("Boss");
        }

        ConfigureCollider(false);
        SetAttackEffectVisible(false);
    }

    private void Update()
    {
        CacheComponents();

        float deltaTime = Time.deltaTime;
        cooldownTimer -= deltaTime;

        if (!isAttacking)
        {
            UpdateSwordPose();

            if (CanStartAttack())
            {
                attackRoutine = StartCoroutine(AttackRoutine());
            }
        }
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
            effectRoutine = null;
        }

        isAttacking = false;
        ConfigureCollider(false);
        SetAttackEffectVisible(false);
    }

    private bool CanStartAttack()
    {
        Mouse mouse = Mouse.current;
        return mouse != null
            && mouse.leftButton.wasPressedThisFrame
            && cooldownTimer <= 0f
            && playerMovement != null
            && !playerMovement.IsControlLocked
            && playerController != null
            && playerController.CanAttack;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        cooldownTimer = attackCooldown;
        damagedTargets.Clear();
        UpdateSwordPose();
        NotifyAttackStarted();
        Debug.Log($"[PlayerAttack] 공격 시작 무기 위치={transform.position}, 타겟레이어={targetLayer.value}", this);
        ConfigureCollider(true);
        DetectHits();

        float elapsed = 0f;
        while (elapsed < activeDuration && CanKeepAttackActive())
        {
            elapsed += Time.deltaTime;
            UpdateSwordPose();
            DetectHits();
            yield return null;
        }

        isAttacking = false;
        ConfigureCollider(false);

        if (playerMovement != null)
        {
            playerMovement.ResetAttackDirectionToFacing();
        }

        attackRoutine = null;
    }

    private void NotifyAttackStarted()
    {
        AttackStarted?.Invoke();

        if (attackEffectAnimator == null || string.IsNullOrWhiteSpace(attackEffectTriggerName))
        {
            return;
        }

        SetAttackEffectVisible(true);
        attackEffectAnimator.ResetTrigger(attackEffectTriggerName);
        attackEffectAnimator.SetTrigger(attackEffectTriggerName);
        attackEffectAnimator.Update(0f);

        if (!string.IsNullOrWhiteSpace(attackEffectStateName)
            && !attackEffectAnimator.GetCurrentAnimatorStateInfo(0).IsName(attackEffectStateName))
        {
            attackEffectAnimator.Play(attackEffectStateName, 0, 0f);
            attackEffectAnimator.Update(0f);
        }

        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
        }

        effectRoutine = StartCoroutine(HideAttackEffectAfterDelay());
    }

    private IEnumerator HideAttackEffectAfterDelay()
    {
        yield return new WaitForSeconds(attackEffectVisibleDuration);
        SetAttackEffectVisible(false);
        effectRoutine = null;
    }

    private void SetAttackEffectVisible(bool visible)
    {
        if (attackEffectRenderer != null)
        {
            attackEffectRenderer.enabled = visible;
        }
    }

    private bool CanKeepAttackActive()
    {
        return playerMovement != null
            && !playerMovement.IsControlLocked
            && playerController != null
            && playerController.CanAttack;
    }

    private void UpdateSwordPose()
    {
        if (playerMovement == null || swordCollider == null)
        {
            return;
        }

        Vector2 origin = GetAttackOriginPosition();
        Vector2 direction = GetMouseAttackDirection(origin);
        Vector2 offset = direction * attackOffsetDistance;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + hitboxAngleOffset;

        swordCollider.offset = Vector2.zero;
        swordCollider.size = hitboxSize;
        transform.position = origin + offset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private Vector2 GetMouseAttackDirection(Vector2 origin)
    {
        Mouse mouse = Mouse.current;
        Camera camera = GetWorldCamera();
        if (mouse != null && camera != null)
        {
            Vector3 mouseScreenPosition = mouse.position.ReadValue();
            mouseScreenPosition.z = transform.position.z - camera.transform.position.z;
            Vector2 mouseWorldPosition = camera.ScreenToWorldPoint(mouseScreenPosition);
            Vector2 direction = mouseWorldPosition - origin;

            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }
        }

        Vector2 fallbackDirection = playerMovement.AttackDirection;
        if (fallbackDirection.sqrMagnitude <= 0.0001f)
        {
            fallbackDirection = new Vector2(playerMovement.FacingDirection, 0f);
        }

        return fallbackDirection.normalized;
    }

    private Vector2 GetAttackOriginPosition()
    {
        if (attackOrigin != null)
        {
            return attackOrigin.position;
        }

        return playerMovement != null ? playerMovement.transform.position : transform.position;
    }

    private Camera GetWorldCamera()
    {
        if (worldCamera != null)
        {
            return worldCamera;
        }

        worldCamera = Camera.main;
        return worldCamera;
    }

    private void DetectHits()
    {
        if (swordCollider == null || !swordCollider.enabled)
        {
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(targetLayer);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int hitCount = swordCollider.Overlap(filter, hitBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = hitBuffer[i];
            BossHealth bossHealth = hit.GetComponentInParent<BossHealth>();
            if (bossHealth != null)
            {
                if (!damagedTargets.Add(bossHealth)) continue;
                bossHealth.TakeDamage(damage);
                Debug.Log($"[PlayerAttack] 보스={bossHealth.name}, 대미지={damage}, 보스Hp={bossHealth.CurrentHp}/{bossHealth.MaxHp}", bossHealth);
                continue;
            }

            foreach (MonoBehaviour behaviour in hit.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is not IPlayerDashDamageable damageable || !damagedTargets.Add(damageable)) continue;
                damageable.TakeDamage(damage);
                break;
            }
        }
    }

    private void ConfigureCollider(bool isActive)
    {
        if (swordCollider == null)
        {
            swordCollider = GetComponent<BoxCollider2D>();
        }

        if (swordCollider != null)
        {
            // Sword 콜라이더는 기본 비활성입니다. 공격 판정이 열리는 짧은 시간에만 켜서 상시 충돌과 중복 타격을 막습니다.
            swordCollider.isTrigger = true;
            swordCollider.enabled = isActive;
        }
    }

    private void CacheComponents()
    {
        if (swordCollider == null)
        {
            swordCollider = GetComponent<BoxCollider2D>();
        }

        if (playerMovement == null)
        {
#if UNITY_2023_1_OR_NEWER
            playerMovement = FindFirstObjectByType<PlayerMovement>();
#else
            playerMovement = FindObjectOfType<PlayerMovement>();
#endif
        }

        if (playerController == null && playerMovement != null)
        {
            playerController = playerMovement.GetComponent<PlayerController>();
        }

        if (attackEffectAnimator == null)
        {
            attackEffectAnimator = GetComponent<Animator>();
        }

        if (attackEffectRenderer == null)
        {
            attackEffectRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        activeDuration = Mathf.Max(0.01f, activeDuration);
        attackEffectVisibleDuration = Mathf.Max(0.01f, attackEffectVisibleDuration);
        attackOffsetDistance = Mathf.Max(0f, attackOffsetDistance);
        hitboxSize.x = Mathf.Max(0.01f, hitboxSize.x);
        hitboxSize.y = Mathf.Max(0.01f, hitboxSize.y);

        if (!Application.isPlaying)
        {
            ConfigureCollider(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D boxCollider = swordCollider != null ? swordCollider : GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Matrix4x4 originalMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(boxCollider.offset, boxCollider.size);
        Gizmos.matrix = originalMatrix;
    }
}
