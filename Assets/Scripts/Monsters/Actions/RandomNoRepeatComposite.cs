using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

/// #9 개체 공격 무작위 선택(세부기획 D-3): 자식 중 직전 실행 인덱스를 제외하고 무작위로 하나 실행.
/// 직전 인덱스는 Blackboard 변수(LastAttackIndex)에 기록한다 —
/// 최초 학습 돌진(#9)이 0(=Dash)을 시드해 학습 직후 돌진 연속을 막고, 리셋(#15)은 -1로 초기화.
/// 자식 2개(페이즈 1)면 사실상 교대이고, 3개(#13에서 빔 추가)부터 진짜 무작위가 된다.
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

    int _current;

    protected override Status OnStart()
    {
        if (Children.Count == 0) return Status.Success;

        int count = Children.Count;
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
        Debug.Log($"[BossAttack] 개체 공격 선택: 자식 {_current} (직전 {last}) @ {Time.time:F2}s");

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
