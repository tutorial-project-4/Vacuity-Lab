using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BossHealth))]
public sealed class Boss2Controller : MonoBehaviour
{
    [Header("확산탄")]
    [Tooltip("확산탄이 1초 동안 이동하는 거리입니다. 단위: position 값 1/초.")]
    [SerializeField] float spreadSpeed = 4.25f;
    [Tooltip("확산탄이 생성 위치부터 이동할 수 있는 최대 거리입니다. 이 거리를 이동하면 자동으로 제거됩니다. 단위: position 값 1.")]
    [SerializeField] float spreadRange = 12f;
    [Tooltip("확산탄 발사 전에 기다리는 시간입니다. 별도 경고선은 표시하지 않습니다. 단위: 초.")]
    [SerializeField] float spreadWindup = .15f;
    [Tooltip("확산탄 5발을 발사한 뒤 조준탄 준비를 시작하기까지 기다리는 시간입니다. 단위: 초.")]
    [SerializeField] float spreadRecovery = .65f;

    [Header("조준탄")]
    [Tooltip("플레이어 방향을 확정하고 조준선을 표시하는 시간입니다. 이 시간이 끝나면 확정된 방향으로 발사합니다. 단위: 초.")]
    [SerializeField] float aimedWarning = .65f;
    [Tooltip("조준탄이 1초 동안 이동하는 거리입니다. 단위: position 값 1/초.")]
    [SerializeField] float aimedSpeed = 12f;
    [Tooltip("조준탄이 생성 위치부터 이동할 수 있는 최대 거리입니다. 이 거리를 이동하면 자동으로 제거됩니다. 단위: position 값 1.")]
    [SerializeField] float aimedRange = 40f;
    [Tooltip("조준탄을 발사한 뒤 다음 확산탄 공격까지 기다리는 시간입니다. 단위: 초.")]
    [SerializeField] float aimedRecovery = .8f;

    [Header("발사체 및 경고선 표시")]
    [Tooltip("확산탄의 가로·세로 크기 배율입니다. 1이면 기본 스프라이트 크기이며 충돌 범위도 함께 변합니다.")]
    [SerializeField] float spreadProjectileSize = .5f;
    [Tooltip("조준탄의 가로·세로 크기 배율입니다. 1이면 기본 스프라이트 크기이며 충돌 범위도 함께 변합니다.")]
    [SerializeField] float aimedProjectileSize = .5f;
    [Tooltip("발사체 Sprite Renderer의 Order in Layer 값입니다. 값이 클수록 같은 Sorting Layer의 다른 스프라이트보다 앞에 표시됩니다.")]
    [SerializeField] int projectileOrderInLayer = 4;
    [Tooltip("조준탄 발사 전에 표시되는 조준선의 굵기입니다. 단위: position 값 1.")]
    [SerializeField] float warningLineWidth = .06f;

    static Sprite projectileSprite;
    readonly List<GameObject> spawned = new();
    BossHealth health;
    Transform player;
    float attackStartTime;

    void Awake()
    {
        health = GetComponent<BossHealth>();
        health.OnDeath += HandleDeath;
    }

    void OnEnable() => attackStartTime = Time.time + .5f;

    void OnDisable()
    {
        ClearSpawned();
    }

    void OnDestroy()
    {
        if (health != null) health.OnDeath -= HandleDeath;
    }

    internal bool TryFireSpread()
    {
        if (!CanAttack()) return false;
        FireSpread();
        return true;
    }

    internal bool TryBeginAimed(out Vector2 direction, out GameObject warning)
    {
        direction = default;
        warning = null;
        if (!CanAttack()) return false;
        direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        warning = CreateWarning(direction);
        return true;
    }

    internal void FireAimed(Vector2 direction, GameObject warning)
    {
        RemoveSpawned(warning);
        FireProjectile(direction, aimedSpeed, aimedRange, aimedProjectileSize);
    }

    internal void CancelWarning(GameObject warning) => RemoveSpawned(warning);
    internal float SpreadWindup => spreadWindup;
    internal float SpreadRecovery => spreadRecovery;
    internal float AimedWarning => aimedWarning;
    internal float AimedRecovery => aimedRecovery;

    bool CanAttack()
    {
        if (health.IsDead || Time.time < attackStartTime) return false;
        if (player == null)
        {
            PlayerMovement found = FindAnyObjectByType<PlayerMovement>();
            if (found != null) player = found.transform;
        }
        return player != null;
    }

    void FireSpread()
    {
        float center = player.position.x >= transform.position.x
            ? Random.Range(-60f, 60f)
            : Random.Range(120f, 240f);
        for (int i = -2; i <= 2; i++)
        {
            float angle = (center + i * 15f) * Mathf.Deg2Rad;
            FireProjectile(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), spreadSpeed, spreadRange, spreadProjectileSize);
        }
    }

    void FireProjectile(Vector2 direction, float speed, float range, float size)
    {
        projectileSprite ??= Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1f);

        var projectile = new GameObject("Boss2 Projectile");
        projectile.layer = gameObject.layer;
        projectile.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
        projectile.transform.localScale = Vector3.one * size;
        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite;
        renderer.sortingOrder = projectileOrderInLayer;
        projectile.AddComponent<CircleCollider2D>().isTrigger = true;
        Rigidbody2D body = projectile.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        projectile.AddComponent<PlayerDamageSource>();
        projectile.AddComponent<Boss2Projectile>().Initialize(direction, speed, range);
        spawned.Add(projectile);
    }

    GameObject CreateWarning(Vector2 direction)
    {
        var warning = new GameObject("Boss2 Aim Warning");
        warning.transform.SetParent(transform, false);
        var line = warning.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = line.endWidth = warningLineWidth;
        line.startColor = line.endColor = new Color(1f, .25f, .25f, .8f);
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.SetPosition(0, transform.position);
        line.SetPosition(1, (Vector2)transform.position + direction * aimedRange);
        spawned.Add(warning);
        return warning;
    }

    void HandleDeath()
    {
        ClearSpawned();
        gameObject.SetActive(false);
    }

    void RemoveSpawned(GameObject item)
    {
        if (item == null) return;
        spawned.Remove(item);
        LineRenderer line = item.GetComponent<LineRenderer>();
        if (line != null && line.material != null) Destroy(line.material);
        Destroy(item);
    }

    void ClearSpawned()
    {
        foreach (GameObject item in spawned)
        {
            if (item == null) continue;
            LineRenderer line = item.GetComponent<LineRenderer>();
            if (line != null && line.material != null) Destroy(line.material);
            Destroy(item);
        }
        spawned.Clear();
    }

#if UNITY_EDITOR
    [ContextMenu("Self Test")]
    void SelfTest()
    {
        Debug.Assert(spreadSpeed > 0f && spreadRange > 0f && spreadWindup >= 0f && spreadRecovery >= 0f);
        Debug.Assert(aimedWarning >= 0f && aimedSpeed > spreadSpeed && aimedRange > 0f && aimedRecovery >= 0f);
        Debug.Assert(spreadProjectileSize > 0f && aimedProjectileSize > 0f && warningLineWidth > 0f);
        Debug.Log("Boss2Controller Self Test PASS", this);
    }
#endif
}

sealed class Boss2Projectile : MonoBehaviour
{
    Vector2 direction;
    float speed;
    float remaining;

    public void Initialize(Vector2 newDirection, float newSpeed, float range)
    {
        direction = newDirection.normalized;
        speed = newSpeed;
        remaining = range;
    }

    void Update()
    {
        float distance = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * distance);
        remaining -= distance;
        if (remaining <= 0f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null) Destroy(gameObject);
    }
}
