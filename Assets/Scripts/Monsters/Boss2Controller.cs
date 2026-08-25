using System.Collections.Generic;
using System.Collections;
using Unity.Behavior;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BossHealth), typeof(BehaviorGraphAgent))]
public sealed class Boss2Controller : MonoBehaviour, IBossEncounter
{
    [Header("전투 시작")]
    [Tooltip("공격 대상으로 사용할 플레이어입니다. 미할당 시 전투 시작 때 활성 플레이어를 찾습니다.")]
    [SerializeField] Transform target;
    [Tooltip("활성화하면 BeginBattle() 호출 전까지 행동·피격·체력 UI를 중지합니다.")]
    [SerializeField] bool waitForBattleTrigger = true;

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

    [Header("벽 러시")]
    [Tooltip("한 번의 벽 러시에서 무작위로 생성할 벽 프리팩 목록입니다.")]
    [SerializeField] GameObject[] wallPrefabs;
    [Tooltip("보스전 시작 후 벽 러시가 시작되는 주기입니다. 단위: 초.")]
    [SerializeField] float wallRushInterval = 15f;
    [Tooltip("첫 벽 이후 다음 벽을 생성하는 간격입니다. 단위: 초.")]
    [SerializeField] float wallSpawnInterval = 1.5f;
    [Tooltip("한 번의 벽 러시에서 생성할 벽 개수입니다.")]
    [SerializeField] int wallsPerRush = 3;
    [Tooltip("이동 벽이 1초 동안 왼쪽으로 이동하는 거리입니다. 단위: position 값 1/초.")]
    [SerializeField] float wallSpeed = 3f;
    [Tooltip("이동 벽을 생성할 월드 위치입니다. 임시 아레나가 바뀌면 조정하세요.")]
    [SerializeField] Vector2 wallSpawnPosition = new(105f, 28.96f);
    [Tooltip("벽이 아레나에 진입했다고 판정할 오른쪽 경계 X입니다.")]
    [SerializeField] float arenaRightX = 104f;
    [Tooltip("벽을 제거할 왼쪽 경계 X입니다.")]
    [SerializeField] float wallDespawnX = 76f;
    [Tooltip("벽 러시 중에만 활성화할 왼쪽 가시 벽의 씬 오브젝트 이름입니다.")]
    [SerializeField] string spikeWallName = "spikeWall L";

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
    BehaviorGraphAgent agent;
    Transform player;
    PlayerHealth playerHealth;
    float attackStartTime;
    Coroutine wallRushRoutine;
    GameObject spikeWall;
    int remainingRushWalls;
    bool battleStarted;
    Vector3 startPosition;

    public bool IsBattleStarted => battleStarted;
    public BossHealth Health => health;

    void Awake()
    {
        health = GetComponent<BossHealth>();
        agent = GetComponent<BehaviorGraphAgent>();
        startPosition = transform.position;
        health.OnDeath += HandleDeath;
    }

    void OnEnable()
    {
        spikeWall = FindInactiveObject(spikeWallName);
        SetSpikeWall(false);
    }

    void Start()
    {
        if (!battleStarted && waitForBattleTrigger)
        {
            agent.End();
            health.Invulnerable = true;
            BossHealthGauge.HideFor(health);
            return;
        }

        if (!battleStarted) BeginBattle();
    }

    void OnDisable()
    {
        StopBattle();
        UnsubscribePlayerDeath();
    }

    void OnDestroy()
    {
        if (health != null) health.OnDeath -= HandleDeath;
        UnsubscribePlayerDeath();
    }

    public void BeginBattle()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (battleStarted || health.IsDead) return;

        battleStarted = true;
        health.Invulnerable = false;
        ResolvePlayer();
        attackStartTime = Time.time + .5f;
        agent.Restart();
        wallRushRoutine = StartCoroutine(WallRushLoop());
        BossHealthGauge.ShowFor(health);
        Debug.Log("[Boss2] 보스전 시작", this);
    }

    public void ResetForRetry()
    {
        Time.timeScale = 1f;
        StopBattle();
        transform.position = startPosition;
        health.ResetHealth();
        BeginBattle();
        Debug.Log("[Boss2] 재도전 초기화", this);
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
        if (!battleStarted || health.IsDead || Time.time < attackStartTime) return false;
        if (player == null) ResolvePlayer();
        return player != null;
    }

    void ResolvePlayer()
    {
        if (target == null)
        {
            PlayerMovement found = FindAnyObjectByType<PlayerMovement>();
            if (found != null) target = found.transform;
        }

        player = target;
        PlayerHealth next = target != null ? target.GetComponentInParent<PlayerHealth>() : null;
        if (playerHealth == next) return;
        UnsubscribePlayerDeath();
        playerHealth = next;
        if (playerHealth != null) playerHealth.Died += HandlePlayerDeath;
    }

    void UnsubscribePlayerDeath()
    {
        if (playerHealth != null) playerHealth.Died -= HandlePlayerDeath;
        playerHealth = null;
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

    IEnumerator WallRushLoop()
    {
        float nextRush = Time.time + wallRushInterval;
        while (!health.IsDead)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, nextRush - Time.time));
            nextRush += wallRushInterval;
            yield return SpawnRush();
        }
    }

    IEnumerator SpawnRush()
    {
        if (wallPrefabs == null || wallPrefabs.Length == 0)
        {
            Debug.LogWarning("[Boss2] 벽 러시 프리팩이 연결되지 않았습니다.", this);
            yield break;
        }

        Debug.Log($"[Boss2] 벽 러시 시작: {wallsPerRush}개, {wallSpawnInterval:0.##}초 간격", this);
        remainingRushWalls += wallsPerRush;
        for (int i = 0; i < wallsPerRush; i++)
        {
            SpawnWall();
            if (i < wallsPerRush - 1) yield return new WaitForSeconds(wallSpawnInterval);
        }
    }

    void SpawnWall()
    {
        GameObject prefab = wallPrefabs[Random.Range(0, wallPrefabs.Length)];
        if (prefab == null)
        {
            OnWallExited(null);
            return;
        }
        GameObject wall = Instantiate(prefab, wallSpawnPosition, Quaternion.identity);
        wall.name = "Boss2 Moving Wall";
        wall.AddComponent<Boss2MovingWall>().Initialize(wallSpeed, arenaRightX, wallDespawnX, OnWallEntered, OnWallExited);
        spawned.Add(wall);
    }

    void OnWallEntered() => SetSpikeWall(true);

    void OnWallExited(GameObject wall)
    {
        spawned.Remove(wall);
        remainingRushWalls--;
        if (remainingRushWalls <= 0) SetSpikeWall(false);
    }

    void SetSpikeWall(bool active)
    {
        if (spikeWall != null) spikeWall.SetActive(active);
    }

    static GameObject FindInactiveObject(string objectName)
    {
        foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (item.name == objectName) return item.gameObject;
        return null;
    }

#if UNITY_EDITOR
    [ContextMenu("벽 러시 즉시 테스트")]
    void TestWallRushNow()
    {
        if (Application.isPlaying && isActiveAndEnabled) StartCoroutine(SpawnRush());
        else Debug.LogWarning("플레이 모드에서 활성화된 Boss-2로 실행하세요.", this);
    }
#endif

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
        StopBattle();
        Debug.Log("[Boss2] 사망 — 행동 및 생성물 정리", this);
    }

    void HandlePlayerDeath()
    {
        if (!battleStarted || health.IsDead) return;
        health.Invulnerable = true;
        Time.timeScale = 0f;
    }

    void StopBattle()
    {
        battleStarted = false;
        agent.End();
        if (wallRushRoutine != null) StopCoroutine(wallRushRoutine);
        wallRushRoutine = null;
        remainingRushWalls = 0;
        SetSpikeWall(false);
        ClearSpawned();
        BossHealthGauge.HideFor(health);
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
    [ContextMenu("Test: Reset")]
    void TestReset() => ResetForRetry();

    [ContextMenu("Self Test")]
    void SelfTest()
    {
        Debug.Assert(spreadSpeed > 0f && spreadRange > 0f && spreadWindup >= 0f && spreadRecovery >= 0f);
        Debug.Assert(aimedWarning >= 0f && aimedSpeed > spreadSpeed && aimedRange > 0f && aimedRecovery >= 0f);
        Debug.Assert(spreadProjectileSize > 0f && aimedProjectileSize > 0f && warningLineWidth > 0f);
        Debug.Assert(wallsPerRush > 0 && wallRushInterval >= wallSpawnInterval * (wallsPerRush - 1) && wallSpawnInterval >= 0f && wallSpeed > 0f);
        Debug.Assert(arenaRightX < wallSpawnPosition.x && wallDespawnX < arenaRightX);
        Debug.Assert(wallPrefabs != null && wallPrefabs.Length > 0);
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
