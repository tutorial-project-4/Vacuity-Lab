using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// #5 전용 stub: 실제 행동 자리에 놓고 메시지·시각만 로그로 찍는다. 즉시 Success.
/// Message만 바꿔 "행동A"/"행동B" 등으로 재사용. 실제 행동(#6·#7·#11)이 생기면 교체 후 이 파일 삭제.

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Log Stub",
    story: "Log stub [Message]",
    category: "Action/Boss",
    id: "c9d0e1f2a3b4c5d6e7f8091a2b3c4d5e")]
public partial class LogStubAction : Action
{
    [SerializeReference] public BlackboardVariable<string> Message = new("행동");

    protected override Status OnStart()
    {
        Debug.Log($"[BossStub] {Message.Value} @ {Time.time:F2}s");
        return Status.Success;
    }
}
