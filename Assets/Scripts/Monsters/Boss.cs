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

    BossHealth _health;
    BehaviorGraphAgent _agent;
    bool _phase2Triggered;

    void Awake()
    {
        _health = GetComponent<BossHealth>();
        _agent = GetComponent<BehaviorGraphAgent>();
    }

    void OnEnable()
    {
        _health.OnDeath += HandleDeath;
        _health.OnDamaged += HandleDamaged;
    }

    void OnDisable()
    {
        _health.OnDeath -= HandleDeath;
        _health.OnDamaged -= HandleDamaged;
    }

    void Start()
    {
        if (target == null)
            Debug.LogWarning("[Boss] target 미할당 — 추적이 동작하지 않음", this);
        _agent.SetVariableValue("Target", target ? target.gameObject : null);
    }

    void HandleDeath()
    {
        _agent.End(); // 그래프 정지. 사망 연출, 보상 훅은 #14
        Debug.Log("[Boss] 사망 — 그래프 정지");
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

    /// 그래프 Blackboard의 ElectricFloor(씬 오브젝트)를 재사용 — 씬 참조 중복 방지.
    ElectricFloorScheduler Electric()
    {
        return _agent.GetVariable("ElectricFloor", out BlackboardVariable<GameObject> v) && v.Value != null
            ? v.Value.GetComponent<ElectricFloorScheduler>()
            : null;
    }

#if UNITY_EDITOR
    // #5 검증용: 플레이 모드에서 컴포넌트 우클릭 → 사망 정지 / 리셋 재시작 확인.
    // 정식 리스폰 진입점(ResetForRetry)은 #15에서 승격.
    [ContextMenu("Test: Kill")]
    void TestKill() => _health.TakeDamage(int.MaxValue);

    // #10 검증용: 1회 → 1000→500 전환 트리거, 이후 1회 더 → 사망(전환 없이 사망 우선 확인은 Kill로)
    [ContextMenu("Test: Damage 500")]
    void TestDamage500() => _health.TakeDamage(500);

    [ContextMenu("Test: Reset")]
    void TestReset()
    {
        Debug.Log("[Boss] 리셋 — 그래프 재시작");
        _health.ResetHealth();
        _agent.Restart(); // Restart()는 첫 노드까지 동기 실행하므로 로그를 먼저 찍는다

    }
#endif
}
