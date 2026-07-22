using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// 기본 추적(#4): 타깃의 x 방향으로 느리게 이동한다. 항상 Running(중단은 상위 노드가 결정).
/// 여기 Speed(기본 이동속도)가 돌진(#6) 5배속의 기준값이다.
/// y는 건드리지 않는다 — 층 이동은 돌진 행동(#6)의 수직 정렬이 담당.

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

    protected override Status OnUpdate()
    {
        var self = Agent?.Value;
        var target = Target?.Value;
        if (self == null || target == null) return Status.Failure;

        var pos = self.transform.position;
        pos.x = Mathf.MoveTowards(pos.x, target.transform.position.x, Speed.Value * Time.deltaTime);
        self.transform.position = pos;
        return Status.Running;
    }
}
