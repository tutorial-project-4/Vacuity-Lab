using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// #9 그래프 → 전기 바닥 필드 스케줄러(#8) 연결 노드 2종.
/// [ElectricFloor]에는 ElectricFloorScheduler가 붙은 씬 오브젝트를 Blackboard로 링크한다
/// (BehaviorGraphAgent 인스펙터의 Blackboard 오버라이드에서 할당).

/// 최초 학습 패턴 고정 전기: B1~B3 1사이클을 즉시 발동하고 Success (세부기획 C-4).
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Electric Learning Batch",
    story: "Runs fixed learning batch on [ElectricFloor]",
    category: "Action/Boss",
    id: "9d4c7e2a51b34f68a0c3d5e7f1b2c4d6")]
public partial class ElectricLearningBatchAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ElectricFloor;

    protected override Status OnStart()
    {
        ElectricFloorScheduler scheduler = Find(ElectricFloor);
        if (scheduler == null)
        {
            Debug.LogWarning("[ElectricFloor] Blackboard ElectricFloor 참조 없음 — 학습 전기 생략");
            return Status.Failure;
        }
        scheduler.RunLearningBatch();
        return Status.Success;
    }

    internal static ElectricFloorScheduler Find(BlackboardVariable<GameObject> variable)
    {
        GameObject go = variable != null ? variable.Value : null;
        return go != null ? go.GetComponent<ElectricFloorScheduler>() : null;
    }
}

/// 필드 스케줄러 무한 루프 시작(학습 패턴 종료 후 1회 호출) — 즉시 Success.
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Electric Begin Loop",
    story: "Begins field scheduler loop on [ElectricFloor]",
    category: "Action/Boss",
    id: "4b8e1f6a2d954c07b3e6a9d0c2f15e84")]
public partial class ElectricBeginLoopAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> ElectricFloor;

    protected override Status OnStart()
    {
        ElectricFloorScheduler scheduler = ElectricLearningBatchAction.Find(ElectricFloor);
        if (scheduler == null)
        {
            Debug.LogWarning("[ElectricFloor] Blackboard ElectricFloor 참조 없음 — 필드 스케줄러 시작 생략");
            return Status.Failure;
        }
        scheduler.Begin();
        return Status.Success;
    }
}
