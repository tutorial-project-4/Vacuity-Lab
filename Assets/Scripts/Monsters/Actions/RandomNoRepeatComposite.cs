using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// #9 개체 공격 무작위 선택(세부기획 D-3): 자식 중 직전 실행 인덱스를 제외하고 무작위로 하나 실행.
/// 직전 인덱스는 Blackboard 변수(LastAttackIndex)에 기록한다 —
/// 최초 학습 돌진(#9)이 0(=Dash)을 시드해 학습 직후 돌진 연속을 막고, 리셋(#15)은 -1로 초기화.
/// #13 페이즈 분기(G-3): 자식 순서는 0=돌진, 1=점프 낙하, 2=빔 고정.
/// Phase가 2 미만이면 후보를 앞 2개(돌진·낙하)로 제한한다 — 빔은 페이즈 2 전용.
/// 가중치는 후보 동일(D-3 기본값) — 플레이테스트 후 필요해지면 추가한다.
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Random No Repeat",
    story: "Runs a random child, excluding last index [LastIndex]",
    category: "Flow",
    id: "7a3d9e51c2b84f06a1d5e8f04c7b2a93")]
public partial class RandomNoRepeatComposite : Composite
{
    [SerializeReference] public BlackboardVariable<int> LastIndex = new(-1);
    [SerializeReference] public BlackboardVariable<int> Phase = new(1);

    // ponytail: 페이즈 1 후보 수 2 하드코딩 — 후보 구성이 기획에서 바뀌면 필드로 승격
    const int Phase1Candidates = 2;

    int _current;

    protected override Status OnStart()
    {
        if (Children.Count == 0) return Status.Success;

        int count = Phase.Value < 2 ? Mathf.Min(Children.Count, Phase1Candidates) : Children.Count;
        int last = LastIndex.Value;
        if (count > 1 && last >= 0 && last < count)
        {
            _current = UnityEngine.Random.Range(0, count - 1);
            if (_current >= last) _current++; // 직전 제외 보정
        }
        else
        {
            _current = UnityEngine.Random.Range(0, count);
        }

        LastIndex.Value = _current;
        Debug.Log($"[BossAttack] 개체 공격 선택: 자식 {_current} (직전 {last}, 후보 {count}) @ {Time.time:F2}s");

        var status = StartNode(Children[_current]);
        if (status == Status.Success || status == Status.Failure) return status;
        return Status.Waiting;
    }

    protected override Status OnUpdate()
    {
        var status = Children[_current].CurrentStatus;
        if (status == Status.Success || status == Status.Failure) return status;
        return Status.Waiting;
    }
}
