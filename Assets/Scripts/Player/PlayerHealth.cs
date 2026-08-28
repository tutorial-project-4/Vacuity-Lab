using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    private const int BaseMaxHearts = 4;
    private const int UpgradedMaxHearts = 7;

    [SerializeField] private int maxHearts = BaseMaxHearts;
    [SerializeField] private int currentHearts = BaseMaxHearts;
    [SerializeField] private float invincibleDuration = 0.8f;
    [SerializeField] private LayerMask damageSourceLayer = Physics2D.AllLayers;

    private PlayerController playerController;
    private BoxCollider2D bodyCollider;
    private Coroutine invincibleRoutine;
    private bool damageInvincible;
    private int anonymousInvincibleOverrideCount;
    private readonly HashSet<object> invincibleOverrideSources = new HashSet<object>();
    private readonly Collider2D[] damageOverlapBuffer = new Collider2D[16];

    public int MaxHearts => maxHearts;
    public int CurrentHearts => currentHearts;
    public bool IsInvincible => damageInvincible || anonymousInvincibleOverrideCount > 0 || invincibleOverrideSources.Count > 0;
    public bool IsDead { get; private set; }

    public event Action<int, int> HealthChanged;
    public event Action<int, Vector2> Damaged;
    public event Action<int> Healed;
    public event Action Died;

    private void Awake()
    {
        CacheComponents();
        maxHearts = Mathf.Clamp(maxHearts, 1, UpgradedMaxHearts);
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);
        ResetState(currentHearts);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTakeDamageFrom(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTakeDamageFrom(collision.collider);
    }

    private void FixedUpdate()
    {
        if (IsDead || IsInvincible)
        {
            return;
        }

        PollOverlappingDamageSources();
    }

    public bool TakeDamage(int damage, Vector2 damageSourcePosition)
    {
        CacheComponents();

        if (IsDead)
        {
            Debug.Log($"[PlayerHealth] 플레이어 사망으로 대미지 무시 damage={damage}", this);
            return false;
        }

        if (IsInvincible)
        {
            return false;
        }

        int previousHearts = currentHearts;
        currentHearts = Mathf.Max(currentHearts - damage, 0);
        HealthChanged?.Invoke(currentHearts, maxHearts);
        Debug.Log($"[PlayerHealth] 받은 대미지={damage} 위치={damageSourcePosition}. 하트 {previousHearts}->{currentHearts}/{maxHearts}", this);

        RequestHitStop();
        playerController.ReceiveHit(damageSourcePosition, invincibleDuration);

        if (currentHearts <= 0)
        {
            Die();
        }
        else
        {
            Damaged?.Invoke(damage, damageSourcePosition);
            StartInvincible();
        }

        return true;
    }

    public void ResetState(int hearts = BaseMaxHearts)
    {
        CacheComponents();

        if (invincibleRoutine != null)
        {
            StopCoroutine(invincibleRoutine);
            invincibleRoutine = null;
        }

        IsDead = false;
        damageInvincible = false;
        anonymousInvincibleOverrideCount = 0;
        invincibleOverrideSources.Clear();
        currentHearts = Mathf.Clamp(hearts, 0, maxHearts);
        playerController.ResetState();
        HealthChanged?.Invoke(currentHearts, maxHearts);
    }

    public void Respawn(Vector2 position, int hearts = -1)
    {
        transform.position = new Vector3(position.x, position.y, transform.position.z);
        ResetState(hearts < 0 ? maxHearts : hearts);
    }

    private bool TryTakeDamageFrom(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        PlayerDamageSource damageSource = other.GetComponent<PlayerDamageSource>();
        if (damageSource == null)
        {
            damageSource = other.GetComponentInParent<PlayerDamageSource>();
        }

        if (damageSource == null)
        {
            return false;
        }

        Debug.Log($"[PlayerHealth] 공격 감지 : {damageSource.name}, 대미지={damageSource.Damage}", damageSource);
        return TakeDamage(damageSource.Damage, damageSource.transform.position);
    }

    public void Heal(int hearts)
    {
        if (hearts <= 0 || IsDead)
        {
            return;
        }

        int nextHearts = Mathf.Min(currentHearts + hearts, maxHearts);
        if (nextHearts == currentHearts)
        {
            return;
        }

        int healedHearts = nextHearts - currentHearts;
        currentHearts = nextHearts;
        HealthChanged?.Invoke(currentHearts, maxHearts);
        Healed?.Invoke(healedHearts);
    }

    public void AddInvincibleOverride()
    {
        anonymousInvincibleOverrideCount++;
    }

    public void RemoveInvincibleOverride()
    {
        anonymousInvincibleOverrideCount = Mathf.Max(0, anonymousInvincibleOverrideCount - 1);
    }

    public bool AddInvincibleOverride(object source)
    {
        if (source == null)
        {
            AddInvincibleOverride();
            return true;
        }

        return invincibleOverrideSources.Add(source);
    }

    public bool RemoveInvincibleOverride(object source)
    {
        if (source == null)
        {
            RemoveInvincibleOverride();
            return true;
        }

        return invincibleOverrideSources.Remove(source);
    }

    public void UpgradeMaxHealth(bool healToFull = false)
    {
        SetMaxHealth(UpgradedMaxHearts, healToFull);
    }

    public void SetMaxHealth(int hearts, bool healToFull = false)
    {
        maxHearts = Mathf.Clamp(hearts, 1, UpgradedMaxHearts);
        currentHearts = healToFull ? maxHearts : Mathf.Min(currentHearts, maxHearts);
        HealthChanged?.Invoke(currentHearts, maxHearts);
    }

    private void StartInvincible()
    {
        if (invincibleRoutine != null)
        {
            StopCoroutine(invincibleRoutine);
        }

        invincibleRoutine = StartCoroutine(InvincibleRoutine());
    }

    private IEnumerator InvincibleRoutine()
    {
        damageInvincible = true;
        yield return new WaitForSeconds(invincibleDuration);
        damageInvincible = false;
        invincibleRoutine = null;
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        damageInvincible = false;
        anonymousInvincibleOverrideCount = 0;
        invincibleOverrideSources.Clear();

        if (invincibleRoutine != null)
        {
            StopCoroutine(invincibleRoutine);
            invincibleRoutine = null;
        }

        playerController.OnDeath();
        Died?.Invoke();
    }

    private void RequestHitStop()
    {
        // TODO: Call the hit-stop manager here when it is added.
    }

    private void CacheComponents()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<BoxCollider2D>();
        }
    }

    private void PollOverlappingDamageSources()
    {
        CacheComponents();

        if (bodyCollider == null)
        {
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(damageSourceLayer);
        filter.useLayerMask = true;
        filter.useTriggers = true;

        int hitCount = bodyCollider.Overlap(filter, damageOverlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            if (TryTakeDamageFrom(damageOverlapBuffer[i]) || IsDead || IsInvincible)
            {
                break;
            }
        }
    }

    private void OnValidate()
    {
        maxHearts = Mathf.Clamp(maxHearts, 1, UpgradedMaxHearts);
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);
        invincibleDuration = Mathf.Max(0f, invincibleDuration);
    }
}
