using System.Collections;
using Unity.Behavior;
using UnityEngine;

/// 보스 골격. 그래프 밖의 연결 역할만 한다:
/// BossHealth 이벤트 ↔ BehaviorGraphAgent(Blackboard) 연결.
/// 실제 행동(추적·돌진 등)은 전부 Behavior Graph의 커스텀 노드로 구현한다.
///
/// 에디터 셋업:
///   1. 보스 GameObject에 BossHealth + BehaviorGraphAgent + Boss 부착
///   2. Behavior Graph 에셋 생성, Blackboard에 GameObject 변수 "Target" 추가
///   3. 그래프에 ChasePlayerAction 노드 배치(Agent=Self, Target=Target)
///   4. Boss의 target 필드에 임시 타깃(빈 GameObject) 할당 — 플레이어 병합 전까지

[DisallowMultipleComponent]
[RequireComponent(typeof(BossHealth), typeof(BehaviorGraphAgent))]
public class Boss : MonoBehaviour
{
    [Tooltip("추적 대상. feat/player 병합 전에는 빈 GameObject를 임시 타깃으로 사용")]
    [SerializeField] Transform target;

    [Header("전투 시작")]
    [Tooltip("활성화하면 BeginBattle() 호출 전까지 행동·피격·공격 판정을 중지한다")]
    [SerializeField] bool waitForBattleTrigger;

    [Header("빔")]
    [Tooltip("보스 Transform 기준 입의 월드 Y 오프셋. 스프라이트 교체 시 조정")]
    [SerializeField] float beamMouthYOffset = 0.5f;
    [Tooltip("빔 예고 전 플레이어 높이로 수직 정렬하는 속도")]
    [SerializeField] float beamAlignSpeed = 10f;

    public Transform Target => target;
    public float BeamMouthYOffset => beamMouthYOffset;
    public float BeamAlignSpeed => beamAlignSpeed;

    [Header("#10 페이즈 전환 (HP 500, 컷신)")]
    [Tooltip("이 HP 이하 최초 1회에 페이즈 2 전환 (확정 500)")]
    [SerializeField] int phase2HpThreshold = 500;
    [Tooltip("전환 컷신 길이 (잠정 2s, 범위 1.5~3s)")]
    [SerializeField] float cutsceneDuration = 2f;
    [Tooltip("컷신 종료(조작권 반환) 후 2페이즈 스케줄러 시작까지 준비시간 (확정 1s)")]
    [SerializeField] float prepDuration = 1f;
    [Tooltip("전환 컷신에서 활성화되는 가시벽 루트 (기본 비활성, Tools/Boss/Setup Spike Walls)")]
    [SerializeField] GameObject spikeWalls;
    [Tooltip("#12 페이즈 2 강화 전기(세로 라인) 스케줄러 (Tools/Boss/Setup Enhanced Electric). 미할당 시 1페이즈 전기를 재사용")]
    [SerializeField] ElectricFloorScheduler enhancedElectric;

    [Header("#15 사망 정지 / 리스폰")]
    [Tooltip("플레이어 사망 시 화면을 자동 정지(기획 J-4). 게임흐름 담당이 정지를 맡게 되면 끄고 FreezeForPlayerDeath()를 직접 호출")]
    [SerializeField] bool freezeOnPlayerDeath = true;
    [Tooltip("재도전 준비시간 — 이 시간 뒤 1페이즈 루프 시작 (잠정 3s, 범위 2~4s)")]
    [SerializeField] float retryPrepDuration = 3f;

    [Header("#14 보스 사망")]
    [Tooltip("사망 연출 placeholder 길이(잠정). 실제 연출('선배가 기어 나옴')을 붙일 때 교체")]
    [SerializeField] float deathSequenceDuration = 1.5f;
    [Tooltip("사망 연출 종료 후 1회 호출 — 출구 개방·보상·진행 저장을 여기 연결(기획 J-6). 플레이어 풀 회복은 Boss가 처리")]
    [SerializeField] UnityEngine.Events.UnityEvent onDeathSequenceFinished;

    BossHealth _health;
    BehaviorGraphAgent _agent;
    PlayerHealth _playerHealth;
    bool _phase2Triggered;
    bool _battleStarted;

    Vector3 _startPos;
    GameObject[] _hitboxes;   // 몸체·슬램 등 자식 판정
    bool[] _hitboxRest;       // 전투 시작 시점의 활성 상태(슬램은 비활성) — 리셋 시 이대로 복구

    void Awake()
    {
        _health = GetComponent<BossHealth>();
        _agent = GetComponent<BehaviorGraphAgent>();
        _playerHealth = target ? target.GetComponentInParent<PlayerHealth>() : null;

        _startPos = transform.position;
        var sources = GetComponentsInChildren<PlayerDamageSource>(true);
        var list = new System.Collections.Generic.List<GameObject>();
        foreach (var src in sources)
            if (src.gameObject != gameObject) list.Add(src.gameObject); // 루트를 끄면 보스가 통째로 사라진다
        _hitboxes = list.ToArray();
        _hitboxRest = System.Array.ConvertAll(_hitboxes, go => go.activeSelf);
    }

    void OnEnable()
    {
        _health.OnDeath += HandleDeath;
        _health.OnDamaged += HandleDamaged;
        if (_playerHealth) _playerHealth.Died += HandlePlayerDeath;
    }

    void OnDisable()
    {
        _health.OnDeath -= HandleDeath;
        _health.OnDamaged -= HandleDamaged;
        if (_playerHealth) _playerHealth.Died -= HandlePlayerDeath;
    }

    void Start()
    {
        if (target == null)
            Debug.LogWarning("[Boss] target 미할당 — 추적이 동작하지 않음", this);
        _agent.SetVariableValue("Target", target ? target.gameObject : null);

        _battleStarted = !waitForBattleTrigger;
        if (!_battleStarted)
        {
            _agent.End();
            _health.Invulnerable = true;
            Electric()?.Stop();
            if (enhancedElectric) enhancedElectric.Stop();
            foreach (var hitbox in _hitboxes) hitbox.SetActive(false);
            GetComponent<BossHealthGauge>()?.SetVisible(false);
            Debug.Log("[Boss] 전투 시작 트리거 대기");
        }
    }

    public void BeginBattle()
    {
        if (_battleStarted || _health.IsDead) return;

        _battleStarted = true;
        _health.Invulnerable = false;
        GetComponent<BossHealthGauge>()?.SetVisible(true);
        for (int i = 0; i < _hitboxes.Length; i++) _hitboxes[i].SetActive(_hitboxRest[i]);
        _agent.SetVariableValue("Target", target ? target.gameObject : null);
        _agent.Restart();
        _agent.SetVariableValue("Target", target ? target.gameObject : null);
        Debug.Log("[Boss] 진입 장벽 접촉 — 보스전 시작");
    }

    /// #14 사망 처리(기획 J-6): 행동 중단 → 모든 판정 제거 → 연출 placeholder → 외부 훅.
    /// 피격 비활성은 BossHealth.IsDead가 담당(사망 후 TakeDamage 무시).
    void HandleDeath()
    {
        // 전환 준비(prep) 중 사망하면 PhaseTransition 잔여(Restart+스케줄러 재개)를 끊는다.
        // 컷신 중엔 Invulnerable이라 사망 불가 → 플레이어 잠금이 걸린 채 끊길 일은 없다.
        StopAllCoroutines();
        _agent.End(); // 실행 중 공격 취소 — 각 액션 OnEnd가 중력·히트박스 원복

        Electric()?.Stop();
        if (enhancedElectric) enhancedElectric.Stop();
        GameObject beam = Beam();
        if (beam != null && beam.activeSelf) beam.SetActive(false); // 빔 잔상 제거
        if (spikeWalls) spikeWalls.SetActive(false);                // 필드 기믹 판정 제거
        foreach (var hitbox in _hitboxes) hitbox.SetActive(false);  // 몸체·슬램 히트박스 제거

        StartCoroutine(DeathSequence());
    }

    /// #15 플레이어 사망 정지(기획 J-4): 현재 프레임에 전부 멈추고 상태를 그대로 보존한다.
    /// 블랙아웃 완료 후 게임흐름 쪽이 ResetForRetry()를 호출하면 정지가 풀린다.
    /// ponytail: 전역 timeScale=0 — 보스·기믹만 따로 멈추려면 각 코루틴에 일시정지 플래그가 필요하고,
    /// 기획 J-4가 요구하는 건 "화면 전체" 정지라 한 줄로 충분하다. 사망 연출·블랙아웃 UI는 unscaled 시간으로 돌릴 것.
    public void FreezeForPlayerDeath()
    {
        Debug.Log("[Boss] 플레이어 사망 — 화면 정지, 상태 보존");
        _health.Invulnerable = true; // 정지 중 보스 피격 불가(기획 J-2)
        Time.timeScale = 0f;
    }

    void HandlePlayerDeath()
    {
        if (!freezeOnPlayerDeath) return;
        if (_health.IsDead) return; // 같은 프레임에 둘 다 죽으면 보스 사망 우선(기획 A-4)
        FreezeForPlayerDeath();
    }

    /// #15 재도전 초기화(기획 J-5). 블랙아웃 완료 후 1회 호출한다.
    /// 플레이어 체력·위치 복구는 팀원 담당(PlayerHealth.Respawn) — 여기선 보스·기믹만 되돌린다.
    public void ResetForRetry()
    {
        StopAllCoroutines();       // 사망 연출·전환 준비 잔여 차단
        Time.timeScale = 1f;       // 사망 정지 해제
        _agent.End();              // 진행 중 액션 취소 — OnEnd가 중력·히트박스 원복

        _phase2Triggered = false;
        _health.Invulnerable = false;
        _health.ResetHealth();     // HP 1000 (게이지는 OnDamaged로 갱신)
        transform.position = _startPos;

        // 컷신 도중 초기화되더라도 플레이어 잠금이 남지 않게 (없으면 no-op)
        var pc = target ? target.GetComponentInParent<PlayerController>() : null;
        if (pc) pc.SetCutsceneLock(false);
        if (_playerHealth) _playerHealth.RemoveInvincibleOverride(this);

        GameObject beam = Beam();
        if (beam) beam.SetActive(false);                            // 빔 잔상 제거
        if (spikeWalls) spikeWalls.SetActive(false);                // 가시벽은 2페이즈 전용
        if (enhancedElectric) enhancedElectric.Stop();
        ElectricFloorScheduler floor = Electric();
        if (floor)
        {
            floor.gameObject.SetActive(true); // #12 전환 때 숨긴 바닥 전기 루트 복구
            floor.Stop();                     // 남은 예고·판정 제거 (준비시간 뒤 Begin)
        }
        for (int i = 0; i < _hitboxes.Length; i++) _hitboxes[i].SetActive(_hitboxRest[i]);

        StartCoroutine(RetryPrep(floor));
    }

    /// 준비시간 동안 그래프를 멈춰 둔다(기획 J-5 "시작 위치·대기 상태") → 이후 1페이즈 루프 시작.
    IEnumerator RetryPrep(ElectricFloorScheduler floor)
    {
        Debug.Log($"[Boss] 재도전 초기화 — {retryPrepDuration}s 준비 후 1페이즈 루프");
        yield return new WaitForSeconds(retryPrepDuration);

        // 학습 패턴은 재도전에서 생략(LearningDone 유지). Restart()의 Blackboard 초기화 여부가
        // 미확인이라 앞뒤 양쪽에 세팅한다(#10과 동일 패턴).
        SetPhase1Blackboard();
        _agent.Restart();
        SetPhase1Blackboard();
        if (floor) floor.Begin();
        Debug.Log("[Boss] 1페이즈 루프 시작 — Phase=1, 학습 패턴 생략");
    }

    void SetPhase1Blackboard()
    {
        _agent.SetVariableValue("Phase", 1);
        _agent.SetVariableValue("LearningDone", true); // 재도전에선 최초 학습 패턴을 반복하지 않는다
        _agent.SetVariableValue("LastAttackIndex", -1);
    }

    IEnumerator DeathSequence()
    {
        Debug.Log($"[Boss] 사망 — 판정 제거, 연출 placeholder {deathSequenceDuration}s");
        yield return new WaitForSeconds(deathSequenceDuration);
        if (_playerHealth) _playerHealth.Heal(_playerHealth.MaxHearts);
        Debug.Log("[Boss] 사망 연출 종료 — 출구·보상·회복 훅 호출");
        onDeathSequenceFinished?.Invoke();
    }

    void HandleDamaged(int hp)
    {
        // hp <= 0 제외: 한 방에 500 이하와 0에 동시 도달하면 사망 우선(기획 F-2)
        if (_phase2Triggered || hp > phase2HpThreshold || hp <= 0) return;
        _phase2Triggered = true;
        StartCoroutine(PhaseTransition());
    }

    /// #10 전환 컷신: 공격 취소·전기 정지·입력 잠금·보스 무적·가시벽 활성 →
    /// 컷신 종료 후 조작권 반환 → 1초 준비 → Phase=2로 그래프 재시작 + 필드 스케줄러 재개.
    IEnumerator PhaseTransition()
    {
        Debug.Log($"[BossPhase] HP {_health.CurrentHp} — 전환 컷신 시작({cutsceneDuration}s)");
        _agent.End();          // 실행 중 개체 공격 취소 — 각 액션의 OnEnd가 중력·히트박스를 원복한다
        // #13: 액션 Success 후 남은 빔 잔상은 agent.End()로 안 꺼진다 — 명시 제거(F-2 "빔 잔상 제거")
        GameObject beam = Beam();
        if (beam != null && beam.activeSelf) beam.SetActive(false);
        ElectricFloorScheduler floor = Electric();
        floor?.Stop();         // 필드 전기 예고·판정 제거
        // 페이즈 2엔 강화 전기만 — 바닥 전기는 구역 표시까지 숨김(F-2 "필드 전기 제거").
        // 재도전 시 재활성은 #15에서. 강화 전기 미셋업 씬은 폴백으로 계속 쓰므로 숨기지 않는다.
        if (floor != null && enhancedElectric != null)
            floor.gameObject.SetActive(false);
        _health.Invulnerable = true;

        var pc = target ? target.GetComponentInParent<PlayerController>() : null;
        var ph = target ? target.GetComponentInParent<PlayerHealth>() : null;
        if (pc) pc.SetCutsceneLock(true);
        if (ph) ph.AddInvincibleOverride(this); // 가시벽 활성 순간 벽에 붙어 있어도 컷신 중 피해 없음
        if (spikeWalls) spikeWalls.SetActive(true);

        yield return new WaitForSeconds(cutsceneDuration);

        if (pc) pc.SetCutsceneLock(false);
        if (ph) ph.RemoveInvincibleOverride(this);
        _health.Invulnerable = false;
        Debug.Log($"[BossPhase] 컷신 종료 — 조작권 반환, {prepDuration}s 준비");

        yield return new WaitForSeconds(prepDuration);

        // Restart()의 Blackboard 초기화 여부 미확인(#15) — 앞뒤 양쪽에 세팅해 학습 재생을 막는다
        SetPhase2Blackboard();
        _agent.Restart();
        SetPhase2Blackboard();
        // 페이즈 2 필드 공격 = 강화 전기(#12). 일반 전기는 컷신에서 정지된 채 유지(기획 G절)
        if (enhancedElectric != null) enhancedElectric.Begin();
        else Electric()?.Begin(); // 강화 전기 미셋업 씬 폴백 — 1페이즈 전기 재사용
        Debug.Log("[BossPhase] 페이즈 2 시작 — Phase=2, 개체·필드 스케줄러 재개");
    }

    void SetPhase2Blackboard()
    {
        _agent.SetVariableValue("Phase", 2);
        _agent.SetVariableValue("LearningDone", true); // 학습 패턴은 전투 최초 1회만
        _agent.SetVariableValue("LastAttackIndex", -1);
    }

    /// 그래프 Blackboard의 Beam(씬 오브젝트) 재사용 — 씬 참조 중복 방지.
    GameObject Beam()
    {
        return _agent.GetVariable("Beam", out BlackboardVariable<GameObject> v) ? v.Value : null;
    }

    public Vector2 GetBeamMouthPosition(float direction)
    {
        Collider2D body = GetComponent<Collider2D>();
        float x = body != null
            ? (direction < 0f ? body.bounds.min.x : body.bounds.max.x)
            : transform.position.x;
        return new Vector2(x, transform.position.y + beamMouthYOffset);
    }

    /// 그래프 Blackboard의 ElectricFloor(씬 오브젝트)를 재사용 — 씬 참조 중복 방지.
    ElectricFloorScheduler Electric()
    {
        return _agent.GetVariable("ElectricFloor", out BlackboardVariable<GameObject> v) && v.Value != null
            ? v.Value.GetComponent<ElectricFloorScheduler>()
            : null;
    }

#if UNITY_EDITOR
    // 플레이 모드에서 컴포넌트 우클릭으로 검증.
    [ContextMenu("Test: Kill")]
    void TestKill() => _health.TakeDamage(int.MaxValue);

    // #10 검증용: 1회 → 1000→500 전환 트리거, 이후 1회 더 → 사망(전환 없이 사망 우선 확인은 Kill로)
    [ContextMenu("Test: Damage 500")]
    void TestDamage500() => _health.TakeDamage(500);

    // #15 검증용: 플레이어 사망 정지(화면 멈춤·상태 보존) → Test: Reset으로 해제
    [ContextMenu("Test: Freeze (Player Death)")]
    void TestFreeze() => FreezeForPlayerDeath();

    // #15 검증용: 블랙아웃 완료 후 재도전 신호에 해당
    [ContextMenu("Test: Reset")]
    void TestReset() => ResetForRetry();
#endif
}
