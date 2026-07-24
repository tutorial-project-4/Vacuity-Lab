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

    BossHealth _health;
    BehaviorGraphAgent _agent;

    void Awake()
    {
        _health = GetComponent<BossHealth>();
        _agent = GetComponent<BehaviorGraphAgent>();
    }

    void OnEnable() => _health.OnDeath += HandleDeath;
    void OnDisable() => _health.OnDeath -= HandleDeath;

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
}
