using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Boss2Drone : MonoBehaviour, IPlayerDashDamageable
{
    Transform target;
    PlayerMovement playerMovement;
    int hp;
    float speed;
    float stopDistance;
    float slowRadius;
    float knockbackDistance;
    float knockbackDuration;
    bool slowing;
    bool dead;
    Coroutine knockbackRoutine;

    public void Initialize(Transform player, int maxHp, float moveSpeed, float stoppingDistance, float radius, float knockback, float knockbackTime)
    {
        target = player;
        playerMovement = player != null ? player.GetComponentInParent<PlayerMovement>() : null;
        hp = maxHp;
        speed = moveSpeed;
        stopDistance = stoppingDistance;
        slowRadius = radius;
        knockbackDistance = knockback;
        knockbackDuration = knockbackTime;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null) collider.isTrigger = true;
        Rigidbody2D body = GetComponent<Rigidbody2D>() ?? gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
    }

    void Update()
    {
        if (dead || target == null) return;
        float distance = Vector2.Distance(transform.position, target.position);
        SetSlowing(distance <= slowRadius);
        if (knockbackRoutine == null && distance > stopDistance)
            transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    }

    public void TakeDamage(int damage)
    {
        if (dead || damage <= 0) return;
        hp = Mathf.Max(0, hp - damage);
        if (hp == 0)
        {
            dead = true;
            Destroy(gameObject);
            return;
        }

        if (knockbackRoutine != null) StopCoroutine(knockbackRoutine);
        Vector2 direction = target != null ? ((Vector2)transform.position - (Vector2)target.position).normalized : Vector2.left;
        knockbackRoutine = StartCoroutine(Knockback(direction));
    }

    IEnumerator Knockback(Vector2 direction)
    {
        Vector2 start = transform.position;
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = start + direction * knockbackDistance * Mathf.Clamp01(elapsed / knockbackDuration);
            yield return null;
        }
        knockbackRoutine = null;
    }

    void SetSlowing(bool active)
    {
        if (slowing == active || playerMovement == null) return;
        slowing = active;
        playerMovement.SetMoveSlowed(this, active);
    }

    void OnDisable() => SetSlowing(false);
}
