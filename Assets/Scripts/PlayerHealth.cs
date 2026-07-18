using System;
using System.Collections;
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

    private PlayerController playerController;
    private Coroutine invincibleRoutine;

    public int MaxHearts => maxHearts;
    public int CurrentHearts => currentHearts;
    public bool IsInvincible { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        maxHearts = Mathf.Clamp(maxHearts, 1, UpgradedMaxHearts);
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTakeDamageFrom(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTakeDamageFrom(collision.collider);
    }

    public bool TakeDamage(int damage, Vector2 damageSourcePosition)
    {
        if (damage <= 0 || IsDead || IsInvincible)
        {
            return false;
        }

        currentHearts = Mathf.Max(currentHearts - damage, 0);
        HealthChanged?.Invoke(currentHearts, maxHearts);

        RequestHitStop();
        playerController.ReceiveHit(damageSourcePosition, invincibleDuration);

        if (currentHearts <= 0)
        {
            Die();
        }
        else
        {
            StartInvincible();
        }

        return true;
    }

    private void TryTakeDamageFrom(Collider2D other)
    {
        PlayerDamageSource damageSource = other.GetComponent<PlayerDamageSource>();
        if (damageSource == null)
        {
            damageSource = other.GetComponentInParent<PlayerDamageSource>();
        }

        if (damageSource == null)
        {
            return;
        }

        TakeDamage(damageSource.Damage, damageSource.transform.position);
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

        currentHearts = nextHearts;
        HealthChanged?.Invoke(currentHearts, maxHearts);
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
        IsInvincible = true;
        yield return new WaitForSeconds(invincibleDuration);
        IsInvincible = false;
        invincibleRoutine = null;
    }

    private void Die()
    {
        IsDead = true;
        IsInvincible = false;

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

    private void OnValidate()
    {
        maxHearts = Mathf.Clamp(maxHearts, 1, UpgradedMaxHearts);
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);
        invincibleDuration = Mathf.Max(0f, invincibleDuration);
    }
}
