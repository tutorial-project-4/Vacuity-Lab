using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// 기본 추적(#4): 타깃의 x 방향으로 느리게 이동한다.
/// DurationSeconds가 0이면 항상 Running(중단은 상위 노드가 결정), 0보다 크면 그 시간 후 Success —
/// #9에서 시작 유예(3s)·학습 타이밍(2s)·공격 간 대기(1s)를 "추적하며 기다리기"로 쓴다.
/// 여기 Speed(기본 이동속도)가 돌진(#6) 5배속의 기준값이다.
/// y는 건드리지 않는다 — v0.2: 보스는 바닥 전용(수직 이동 없음).
/// StopDistance 이내면 추적 정지. 타일→유닛 비율은 아레나 미확정이라 1타일=1유닛 잠정.

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Chase Player",
    story: "[Agent] chases [Target]",
    category: "Action/Boss",
    id: "5b1f4c8a3e2d4b7f9a6c0d1e2f3a4b5c")]
public partial class ChasePlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> Speed = new(2f);
    [SerializeReference] public BlackboardVariable<float> StopDistance = new(1.5f);
    [SerializeReference] public BlackboardVariable<float> DurationSeconds = new(0f); // 0 = 무한

    float _elapsed;

    protected override Status OnStart()
    {
        _elapsed = 0f;
        Agent?.Value?.GetComponent<Boss>()?.PlayWalk();
        return Status.Running;
    }

    protected override void OnEnd() => Agent?.Value?.GetComponent<Boss>()?.PlayIdle();

    protected override Status OnUpdate()
    {
        _elapsed += Time.deltaTime;
        if (DurationSeconds.Value > 0f && _elapsed >= DurationSeconds.Value) return Status.Success;

        var self = Agent?.Value;
        var target = Target?.Value;
        if (self == null || target == null) return Status.Failure;

        var pos = self.transform.position;
        float targetX = target.transform.position.x;
        if (Mathf.Abs(targetX - pos.x) <= StopDistance.Value) return Status.Running;

        pos.x = Mathf.MoveTowards(pos.x, targetX, Speed.Value * Time.deltaTime);
        self.transform.position = pos;
        return Status.Running;
    }
}
