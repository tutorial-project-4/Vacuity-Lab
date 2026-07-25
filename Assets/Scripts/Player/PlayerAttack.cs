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
    [SerializeField] private BoxCollider2D swordCollider;

    [Header("Attack")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackCooldown = 0.35f;
    [SerializeField] private float activeDuration = 0.08f;
    [SerializeField] private LayerMask targetLayer;

    [Header("Hitbox")]
    [SerializeField] private Vector2 horizontalOffset = new Vector2(1.6f, 0f);
    [SerializeField] private Vector2 horizontalSize = new Vector2(2.2f, 3f);
    [SerializeField] private Vector2 verticalOffset = new Vector2(0f, 1.6f);
    [SerializeField] private Vector2 verticalSize = new Vector2(2.2f, 3f);

    private readonly Collider2D[] hitBuffer = new Collider2D[16];
    private readonly HashSet<BossHealth> damagedTargets = new HashSet<BossHealth>();
    private float cooldownTimer;
    private Coroutine attackRoutine;
    private bool isAttacking;

    private void Awake()
    {
        CacheComponents();

        if (targetLayer.value == 0)
        {
            targetLayer = LayerMask.GetMask("Boss");
        }

        ConfigureCollider(false);
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

        isAttacking = false;
        ConfigureCollider(false);
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

        Vector2 direction = playerMovement.AttackDirection;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = new Vector2(playerMovement.FacingDirection, 0f);
        }

        direction.Normalize();

        Vector2 offset;
        float angle;
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            offset = direction.y > 0f ? verticalOffset : -verticalOffset;
            swordCollider.size = verticalSize;
            angle = direction.y > 0f ? 90f : -90f;
        }
        else
        {
            float sign = direction.x >= 0f ? 1f : -1f;
            offset = new Vector2(horizontalOffset.x * sign, horizontalOffset.y);
            swordCollider.size = horizontalSize;
            angle = sign > 0f ? 0f : 180f;
        }

        swordCollider.offset = Vector2.zero;
        transform.position = playerMovement.transform.position + (Vector3)offset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
            BossHealth bossHealth = hitBuffer[i].GetComponentInParent<BossHealth>();
            if (bossHealth == null || damagedTargets.Contains(bossHealth))
            {
                continue;
            }

            damagedTargets.Add(bossHealth);
            bossHealth.TakeDamage(damage);
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
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
        attackCooldown = Mathf.Max(0f, attackCooldown);
        activeDuration = Mathf.Max(0.01f, activeDuration);
        horizontalSize.x = Mathf.Max(0.01f, horizontalSize.x);
        horizontalSize.y = Mathf.Max(0.01f, horizontalSize.y);
        verticalSize.x = Mathf.Max(0.01f, verticalSize.x);
        verticalSize.y = Mathf.Max(0.01f, verticalSize.y);

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
