using System.Collections.Generic;
using System.Collections;
using Unity.Behavior;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BossHealth), typeof(BehaviorGraphAgent))]
public sealed class Boss2Controller : MonoBehaviour, IBossEncounter
{
    internal enum AttackPattern { Basic, Sniper, Frenzy, Drone }

    [Header("애니메이션")]
    [SerializeField] RuntimeAnimatorController idleAnimation;
    [SerializeField] RuntimeAnimatorController deadAnimation;
    [SerializeField] RuntimeAnimatorController aimedAnimation;
    [SerializeField] RuntimeAnimatorController spreadAnimation;
    [SerializeField] RuntimeAnimatorController phaseAnimation;
    [SerializeField] RuntimeAnimatorController droneAnimation;

    [Header("전투 시작")]
    [Tooltip("공격 대상으로 사용할 플레이어입니다. 미할당 시 전투 시작 때 활성 플레이어를 찾습니다.")]
    [SerializeField] Transform target;
    [Tooltip("활성화하면 BeginBattle() 호출 전까지 행동·피격·체력 UI를 중지합니다.")]
    [SerializeField] bool waitForBattleTrigger = true;

    [Header("플랫폼 위치 이동")]
    [Tooltip("보스가 무작위로 이동할 월드 위치 목록입니다. 플랫폼마다 빈 오브젝트를 배치해 연결하세요.")]
    [SerializeField] List<Transform> platformMovePoints = new();
    [Tooltip("다음 위치 이동까지 기다리는 최소 시간입니다. 단위: 초.")]
    [SerializeField] float platformMoveMinInterval = 3f;
    [Tooltip("다음 위치 이동까지 기다리는 최대 시간입니다. 단위: 초.")]
    [SerializeField] float platformMoveMaxInterval = 10f;

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

    [Header("예약 패턴")]
    [Tooltip("플레이어가 이 거리 이하에 연속으로 머무르면 광분 패턴을 예약합니다. 단위: position 값 1.")]
    [SerializeField] float frenzyRange = 5f;
    [Tooltip("광분 패턴 예약에 필요한 연속 근접 시간입니다. 단위: 초.")]
    [SerializeField] float frenzyDuration = 10f;
    [Tooltip("광분 확산탄 3연사의 발사 간격입니다. 단위: 초.")]
    [SerializeField] float frenzyShotInterval = .12f;
    [Tooltip("저격 조준탄 첫 발 발사 후 두 번째 경고를 시작하기까지의 간격입니다. 단위: 초.")]
    [SerializeField] float sniperShotInterval = .2f;

    [Header("2페이즈 및 드론")]
    [Tooltip("2페이즈가 시작되는 보스 체력입니다.")]
    [SerializeField] int phaseTwoHp = 800;
    [Tooltip("2페이즈 전환 중 보스가 행동과 피격을 멈추는 시간입니다. 단위: 초.")]
    [SerializeField] float phaseTransitionDuration = 3f;
    [Tooltip("2페이즈에서 추가 드론 소환을 예약하는 주기입니다. 첫 드론은 진입 즉시 소환합니다. 단위: 초.")]
    [SerializeField] float droneSummonInterval = 30f;
    [Tooltip("소환할 드론 프리팩입니다.")]
    [SerializeField] GameObject dronePrefab;
    [Tooltip("드론 한 체의 최대 체력입니다.")]
    [SerializeField] int droneHp = 60;
    [Tooltip("드론이 1초 동안 이동하는 거리입니다. 단위: position 값 1/초.")]
    [SerializeField] float droneSpeed = 2f;
    [Tooltip("드론이 플레이어 추적을 멈추는 거리입니다. 단위: position 값 1.")]
    [SerializeField] float droneStopDistance = 1f;
    [Tooltip("플레이어 이동속도를 30% 낮추는 반경입니다. 여러 드론이 겹쳐도 30%까지만 감속합니다.")]
    [SerializeField] float droneSlowRadius = 3f;
    [Tooltip("드론이 피격됐을 때 플레이어 반대 방향으로 밀리는 거리입니다. 임시 조정값입니다.")]
    [SerializeField] float droneKnockbackDistance = 1f;
    [Tooltip("드론 피격 넉백이 진행되는 시간입니다. 임시 조정값이며 단위는 초입니다.")]
    [SerializeField] float droneKnockbackDuration = .2f;

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
    [Tooltip("벽을 제거할 왼쪽 경계 X입니다.")]
    [SerializeField] float wallDespawnX = 76f;
    [Tooltip("벽 러시 중에만 활성화할 왼쪽 가시 벽의 씬 오브젝트 이름입니다.")]
    [SerializeField] string spikeWallName = "spikeWall L";
    [Tooltip("벽 러시 동안 아래로 이동할 천장 가시 벽의 씬 오브젝트 이름입니다.")]
    [SerializeField] string ceilingSpikeWallName = "SpikeWall-2";
    [SerializeField] float ceilingSpikeWallRaisedY = 66f;
    [SerializeField] float ceilingSpikeWallLoweredY = 42f;
    [SerializeField] float ceilingSpikeWallMoveDuration = 1f;

    [Header("발사체 및 경고선 표시")]
    [Tooltip("확산탄의 가로·세로 크기 배율입니다. 1이면 기본 스프라이트 크기이며 충돌 범위도 함께 변합니다.")]
    [SerializeField] float spreadProjectileSize = .5f;
    [Tooltip("조준탄의 가로·세로 크기 배율입니다. 1이면 기본 스프라이트 크기이며 충돌 범위도 함께 변합니다.")]
    [SerializeField] float aimedProjectileSize = .5f;
    [Tooltip("발사체 Sprite Renderer의 Order in Layer 값입니다. 값이 클수록 같은 Sorting Layer의 다른 스프라이트보다 앞에 표시됩니다.")]
    [SerializeField] int projectileOrderInLayer = 4;
    [SerializeField] Sprite spreadProjectileSprite;
    [SerializeField] Sprite aimedProjectileSprite;
    [Tooltip("조준탄 발사 전에 표시되는 조준선의 굵기입니다. 단위: position 값 1.")]
    [SerializeField] float warningLineWidth = .06f;

    static Sprite projectileSprite;
    readonly List<GameObject> spawned = new();
    BossHealth health;
    BehaviorGraphAgent agent;
    Animator animator;
    SpriteRenderer spriteRenderer;
    Transform player;
    PlayerHealth playerHealth;
    float attackStartTime;
    Coroutine wallRushRoutine;
    Coroutine platformMoveRoutine;
    Coroutine ceilingSpikeWallRoutine;
    Coroutine phaseTransitionRoutine;
    Coroutine droneSummonRoutine;
    GameObject spikeWall;
    GameObject ceilingSpikeWall;
    int remainingRushWalls;
    int sniperReservations;
    int droneReservations;
    float frenzyProximityTime;
    bool frenzyReserved;
    AttackPattern currentAttackPattern;
    bool phaseTwo;
    bool transitioning;
    bool battleStarted;
    bool facingLocked;
    Vector3 startPosition;

    public bool IsBattleStarted => battleStarted;
    public BossHealth Health => health;
    public int PhaseTwoHp => phaseTwoHp;

    void Awake()
    {
        health = GetComponent<BossHealth>();
        agent = GetComponent<BehaviorGraphAgent>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;
        PlayAnimation(idleAnimation);
    }

    void OnEnable()
    {
        spikeWall = FindInactiveObject(spikeWallName);
        ceilingSpikeWall = FindInactiveObject(ceilingSpikeWallName);
        SetSpikeWall(false, true);
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

    void Update()
    {
        if (!battleStarted || health.IsDead || transitioning) return;
        if (player == null) ResolvePlayer();
        if (player == null) return;
        if (!facingLocked && spriteRenderer != null) spriteRenderer.flipX = player.position.x < transform.position.x;

        if (Vector2.Distance(transform.position, player.position) > frenzyRange)
        {
            frenzyProximityTime = 0f;
            return;
        }

        if (frenzyReserved) return;
        frenzyProximityTime += Time.deltaTime;
        if (frenzyProximityTime >= frenzyDuration)
        {
            frenzyReserved = true;
            Debug.Log("[Boss2] 광분 패턴 예약", this);
        }
    }

    void OnDisable()
    {
        StopBattle();
        UnsubscribePlayerDeath();
    }

    void OnDestroy()
    {
        if (health != null)
        {
            health.OnDamaged -= HandleDamaged;
            health.OnDeath -= HandleDeath;
        }
        UnsubscribePlayerDeath();
    }

    public void BeginBattle()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (battleStarted || health.IsDead) return;

        battleStarted = true;
        health.Invulnerable = false;
        PlayAnimation(idleAnimation);
        ResolvePlayer();
        attackStartTime = Time.time + .5f;
        agent.Restart();
        wallRushRoutine = StartCoroutine(WallRushLoop());
        platformMoveRoutine = StartCoroutine(PlatformMoveLoop());
        BossHealthGauge.ShowFor(health);
        Debug.Log("[Boss2] 보스전 시작", this);
    }

    public void ResetForRetry()
    {
        Time.timeScale = 1f;
        StopBattle();
        transform.position = startPosition;
        health.ResetHealth();
        health.Invulnerable = true;
        PlayIdleAnimation();
        FindAnyObjectByType<Boss2IntroTrigger>()?.ResetForRetry();
        Debug.Log("[Boss2] 재도전 초기화 — 입장 트리거 대기", this);
    }

    internal bool TryFireSpread()
    {
        if (!CanAttack()) return false;
        FireSpread();
        return true;
    }

    internal void PlaySpreadAnimation()
    {
        LockFacing();
        PlayAnimation(spreadAnimation);
    }

    internal void PlayAimedAnimation()
    {
        LockFacing();
        PlayAnimation(aimedAnimation);
    }

    internal void PlayIdleAnimation()
    {
        facingLocked = false;
        if (!health.IsDead && !transitioning) PlayAnimation(idleAnimation);
    }

    void LockFacing()
    {
        if (player != null && spriteRenderer != null)
            spriteRenderer.flipX = player.position.x < transform.position.x;
        facingLocked = true;
    }

    internal AttackPattern BeginAttackCycle()
    {
        if (droneReservations > 0)
        {
            droneReservations--;
            SpawnDrone();
            Debug.Log($"[Boss2] 드론 소환 패턴 시작 (남은 예약: {droneReservations})", this);
            return currentAttackPattern = AttackPattern.Drone;
        }
        if (sniperReservations > 0)
        {
            sniperReservations--;
            Debug.Log($"[Boss2] 저격 패턴 시작 (남은 예약: {sniperReservations})", this);
            return currentAttackPattern = AttackPattern.Sniper;
        }
        if (frenzyReserved)
        {
            frenzyReserved = false;
            frenzyProximityTime = 0f;
            Debug.Log("[Boss2] 광분 패턴 시작", this);
            return currentAttackPattern = AttackPattern.Frenzy;
        }
        return currentAttackPattern = AttackPattern.Basic;
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
        FireProjectile(direction, aimedSpeed, aimedRange, aimedProjectileSize, aimedProjectileSprite);
    }

    internal void CancelWarning(GameObject warning) => RemoveSpawned(warning);
    internal float SpreadWindup => spreadWindup;
    internal float SpreadRecovery => spreadRecovery;
    internal float AimedWarning => aimedWarning;
    internal float AimedRecovery => aimedRecovery;
    internal float FrenzyShotInterval => frenzyShotInterval;
    internal float SniperShotInterval => sniperShotInterval;
    internal AttackPattern CurrentAttackPattern => currentAttackPattern;

    bool CanAttack()
    {
        if (!battleStarted || health.IsDead || transitioning || Time.time < attackStartTime) return false;
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
            FireProjectile(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), spreadSpeed, spreadRange, spreadProjectileSize, spreadProjectileSprite);
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
        SetSpikeWall(true);
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
        wall.AddComponent<Boss2MovingWall>().Initialize(wallSpeed, wallDespawnX, OnWallExited, ReserveSniper);
        spawned.Add(wall);
    }

    void ReserveSniper()
    {
        sniperReservations++;
        Debug.Log($"[Boss2] 벽 넉백으로 저격 패턴 예약 (누적: {sniperReservations})", this);
    }

    void OnWallExited(GameObject wall)
    {
        spawned.Remove(wall);
        remainingRushWalls--;
        if (remainingRushWalls <= 0) SetSpikeWall(false);
    }

    void SetSpikeWall(bool active, bool immediate = false)
    {
        if (spikeWall != null) spikeWall.SetActive(active);
        if (ceilingSpikeWall == null) return;
        ceilingSpikeWall.SetActive(true);
        if (ceilingSpikeWallRoutine != null) StopCoroutine(ceilingSpikeWallRoutine);
        float targetY = active ? ceilingSpikeWallLoweredY : ceilingSpikeWallRaisedY;
        if (immediate || !isActiveAndEnabled)
        {
            SetCeilingSpikeWallY(targetY);
            return;
        }
        ceilingSpikeWallRoutine = StartCoroutine(MoveCeilingSpikeWall(targetY));
    }

    IEnumerator MoveCeilingSpikeWall(float targetY)
    {
        float startY = ceilingSpikeWall.transform.position.y;
        float elapsed = 0f;
        while (elapsed < ceilingSpikeWallMoveDuration)
        {
            elapsed += Time.deltaTime;
            SetCeilingSpikeWallY(Mathf.Lerp(startY, targetY, Mathf.Clamp01(elapsed / ceilingSpikeWallMoveDuration)));
            yield return null;
        }
        SetCeilingSpikeWallY(targetY);
        ceilingSpikeWallRoutine = null;
    }

    void SetCeilingSpikeWallY(float y)
    {
        Vector3 position = ceilingSpikeWall.transform.position;
        position.y = y;
        ceilingSpikeWall.transform.position = position;
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

    void FireProjectile(Vector2 direction, float speed, float range, float size, Sprite sprite)
    {
        if (projectileSprite == null)
            projectileSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1f);
        if (sprite == null) sprite = projectileSprite;

        var projectile = new GameObject("Boss2 Projectile");
        projectile.layer = gameObject.layer;
        projectile.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
        float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        projectile.transform.localScale = Vector3.one * (size / spriteSize);
        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = projectileOrderInLayer;
        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.radius = spriteSize * .5f;
        collider.isTrigger = true;
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

    void HandleDamaged(int currentHp)
    {
        if (!battleStarted || phaseTwo || currentHp > phaseTwoHp) return;
        phaseTransitionRoutine = StartCoroutine(EnterPhaseTwo());
    }

    IEnumerator EnterPhaseTwo()
    {
        phaseTwo = true;
        transitioning = true;
        health.Invulnerable = true;
        agent.End();
        StopPatternRoutines();
        ResetPatternState();
        SetSpikeWall(false);
        ClearSpawned();
        PlayAnimation(phaseAnimation);
        Debug.Log($"[Boss2] 2페이즈 전환 시작 ({phaseTransitionDuration:0.##}초)", this);

        yield return new WaitForSeconds(phaseTransitionDuration);
        phaseTransitionRoutine = null;
        if (!battleStarted || health.IsDead) yield break;

        transitioning = false;
        health.Invulnerable = false;
        attackStartTime = Time.time + .5f;
        agent.Restart();
        wallRushRoutine = StartCoroutine(WallRushLoop());
        platformMoveRoutine = StartCoroutine(PlatformMoveLoop());
        SpawnDrone();
        droneSummonRoutine = StartCoroutine(DroneSummonLoop());
        Debug.Log("[Boss2] 2페이즈 시작 및 첫 드론 소환", this);
    }

    IEnumerator DroneSummonLoop()
    {
        while (battleStarted && phaseTwo && !health.IsDead)
        {
            yield return new WaitForSeconds(droneSummonInterval);
            if (!battleStarted || health.IsDead) yield break;
            droneReservations++;
            Debug.Log($"[Boss2] 드론 소환 예약 (누적: {droneReservations})", this);
        }
    }

    void SpawnDrone()
    {
        if (dronePrefab == null || player == null)
        {
            Debug.LogWarning("[Boss2] 드론 프리팩 또는 플레이어 참조가 없습니다.", this);
            return;
        }

        GameObject drone = Instantiate(dronePrefab, transform.position, Quaternion.identity);
        PlayAnimation(droneAnimation);
        drone.name = "Boss2 Drone";
        SetLayerRecursively(drone.transform, LayerMask.NameToLayer("Boss"));
        drone.AddComponent<Boss2Drone>().Initialize(player, droneHp, droneSpeed, droneStopDistance, droneSlowRadius, droneKnockbackDistance, droneKnockbackDuration);
        spawned.Add(drone);
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root) SetLayerRecursively(child, layer);
    }

    void HandleDeath()
    {
        StopBattle();
        PlayAnimation(deadAnimation);
        Debug.Log("[Boss2] 사망 — 행동 및 생성물 정리", this);
    }

    IEnumerator PlatformMoveLoop()
    {
        if (platformMovePoints.Count == 0)
        {
            Debug.LogWarning("[Boss2] 플랫폼 이동 위치가 연결되지 않았습니다.", this);
            yield break;
        }

        while (battleStarted && !health.IsDead)
        {
            float min = Mathf.Max(.1f, Mathf.Min(platformMoveMinInterval, platformMoveMaxInterval));
            float max = Mathf.Max(min, Mathf.Max(platformMoveMinInterval, platformMoveMaxInterval));
            yield return new WaitForSeconds(Random.Range(min, max));
            if (!battleStarted || health.IsDead || transitioning) yield break;

            int startIndex = Random.Range(0, platformMovePoints.Count);
            for (int i = 0; i < platformMovePoints.Count; i++)
            {
                Transform point = platformMovePoints[(startIndex + i) % platformMovePoints.Count];
                if (point == null) continue;
                Vector3 position = point.position;
                position.z = transform.position.z;
                transform.position = position;
                break;
            }
        }
    }


    void PlayAnimation(RuntimeAnimatorController controller)
    {
        if (animator == null || controller == null) return;
        animator.runtimeAnimatorController = controller;
        animator.Play(0, 0, 0f);
    }

    void HandlePlayerDeath()
    {
        if (!battleStarted || health.IsDead) return;
        ResetForRetry();
    }

    void StopBattle()
    {
        battleStarted = false;
        agent.End();
        StopPatternRoutines();
        if (phaseTransitionRoutine != null) StopCoroutine(phaseTransitionRoutine);
        phaseTransitionRoutine = null;
        phaseTwo = false;
        transitioning = false;
        ResetPatternState();
        SetSpikeWall(false);
        ClearSpawned();
        BossHealthGauge.HideFor(health);
    }

    void StopPatternRoutines()
    {
        if (wallRushRoutine != null) StopCoroutine(wallRushRoutine);
        if (platformMoveRoutine != null) StopCoroutine(platformMoveRoutine);
        if (droneSummonRoutine != null) StopCoroutine(droneSummonRoutine);
        if (ceilingSpikeWallRoutine != null) StopCoroutine(ceilingSpikeWallRoutine);
        wallRushRoutine = null;
        platformMoveRoutine = null;
        droneSummonRoutine = null;
        ceilingSpikeWallRoutine = null;
    }

    void ResetPatternState()
    {
        remainingRushWalls = 0;
        droneReservations = 0;
        sniperReservations = 0;
        frenzyProximityTime = 0f;
        frenzyReserved = false;
        currentAttackPattern = AttackPattern.Basic;
        facingLocked = false;
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
            Boss2MovingWall wall = item.GetComponent<Boss2MovingWall>();
            if (wall != null) wall.SuppressExitCallback();
            LineRenderer line = item.GetComponent<LineRenderer>();
            if (line != null && line.material != null) Destroy(line.material);
            Destroy(item);
        }
        spawned.Clear();
    }

#if UNITY_EDITOR
    [ContextMenu("Test: Reset")]
    void TestReset() => ResetForRetry();

    [ContextMenu("Test: Reserve Frenzy")]
    void TestReserveFrenzy() => frenzyReserved = true;

    [ContextMenu("Test: Reserve Sniper")]
    void TestReserveSniper() => ReserveSniper();

    [ContextMenu("Self Test")]
    void SelfTest()
    {
        Debug.Assert(spreadSpeed > 0f && spreadRange > 0f && spreadWindup >= 0f && spreadRecovery >= 0f);
        Debug.Assert(aimedWarning >= 0f && aimedSpeed > spreadSpeed && aimedRange > 0f && aimedRecovery >= 0f);
        Debug.Assert(spreadProjectileSize > 0f && aimedProjectileSize > 0f && warningLineWidth > 0f);
        Debug.Assert(wallsPerRush > 0 && wallRushInterval >= wallSpawnInterval * (wallsPerRush - 1) && wallSpawnInterval >= 0f && wallSpeed > 0f);
        Debug.Assert(ceilingSpikeWallRaisedY > ceilingSpikeWallLoweredY && ceilingSpikeWallMoveDuration > 0f);
        Debug.Assert(frenzyRange > 0f && frenzyDuration > 0f && frenzyShotInterval >= 0f && sniperShotInterval >= 0f);
        Debug.Assert(phaseTwoHp > 0 && phaseTransitionDuration >= 0f && droneSummonInterval > 0f);
        Debug.Assert(dronePrefab != null && droneHp > 0 && droneSpeed > 0f && droneStopDistance >= 0f && droneSlowRadius > 0f);
        Debug.Assert(wallDespawnX < wallSpawnPosition.x);
        Debug.Assert(wallPrefabs != null && wallPrefabs.Length > 0);

        int savedSniperReservations = sniperReservations;
        bool savedFrenzyReserved = frenzyReserved;
        float savedFrenzyProximityTime = frenzyProximityTime;
        AttackPattern savedAttackPattern = currentAttackPattern;
        sniperReservations = 1;
        frenzyReserved = true;
        Debug.Assert(BeginAttackCycle() == AttackPattern.Sniper);
        Debug.Assert(BeginAttackCycle() == AttackPattern.Frenzy);
        Debug.Assert(BeginAttackCycle() == AttackPattern.Basic);
        sniperReservations = savedSniperReservations;
        frenzyReserved = savedFrenzyReserved;
        frenzyProximityTime = savedFrenzyProximityTime;
        currentAttackPattern = savedAttackPattern;
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
