using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// #6 직선 돌진(기획 변경 반영): 예고 0.7s → 예고 종료 시 플레이어 y·x방향 고정(이후 보정 없음)
/// → 고정된 플레이어의 발 y로 수직 이동(중력 일시 0) → 체공 HoverSeconds(x축 예고 암시, 인스펙터 조정용)
/// → 초당 10타일, 벽 또는 최대 6타일 x축 직선 이동
/// (플레이어·플랫폼 통과) → 돌진 종료 시 중력 원복 → 후딜 0.5s.
/// 피해는 보스 몸통 히트박스의 PlayerDamageSource(1하트)가 담당 — 이 노드는 이동만.
/// 타일→유닛 비율은 아레나 미확정이라 1타일=1유닛 잠정.

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Dash",
    story: "[Agent] dashes toward [Target]",
    category: "Action/Boss",
    id: "1c6b2f0d5a4e4c31b8f2a7c9d0e14a62")]
public partial class DashAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> TelegraphSeconds = new(0.7f); // 최초 학습 돌진(#9)은 1초로 오버라이드
    [SerializeReference] public BlackboardVariable<float> HoverSeconds = new(0.3f); // 잠정 — y 정렬 후 체공, 기획자가 인스펙터에서 조정
    [SerializeReference] public BlackboardVariable<float> Speed = new(10f);
    [SerializeReference] public BlackboardVariable<float> MaxDistance = new(6f);
    [SerializeReference] public BlackboardVariable<float> RecoverySeconds = new(0.5f);

    enum Phase { Telegraph, Align, Hover, Dash, Recovery }
    Phase _phase;
    float _timer;
    float _dir;          // 예고 종료 시 고정, 이후 보정 없음
    float _targetY;      // 예고 종료 시 고정 — 돌진 전 이 y로 수직 정렬
    float _traveled;
    float _stopDistance; // 벽 레이캐스트로 잘라낸 실제 이동 한계
    Rigidbody2D _rb;
    float _prevGravity;

    protected override Status OnStart()
    {
        if (Agent?.Value == null || Target?.Value == null) return Status.Failure;
        _phase = Phase.Telegraph;
        _timer = 0f;
        Debug.Log($"[BossDash] 예고 시작 @ {Time.time:F2}s"); // 예고 연출은 추후 — 지금은 로그로 검증
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var self = Agent.Value;
        if (self == null) return Status.Failure;
        _timer += Time.deltaTime;

        switch (_phase)
        {
            case Phase.Telegraph:
                if (_timer < TelegraphSeconds.Value) return Status.Running;
                LockDirection(self);
                return Status.Running;

            case Phase.Align:
                var apos = self.transform.position;
                apos.y = Mathf.MoveTowards(apos.y, _targetY, Speed.Value * Time.deltaTime);
                self.transform.position = apos;
                if (!Mathf.Approximately(apos.y, _targetY)) return Status.Running;
                _phase = Phase.Hover;
                _timer = 0f;
                Debug.Log($"[BossDash] 정렬 완료 → 체공 {HoverSeconds.Value:F2}s @ {Time.time:F2}s");
                return Status.Running;

            case Phase.Hover:
                if (_timer < HoverSeconds.Value) return Status.Running;
                BeginDash(self); // 벽 레이캐스트는 정렬된 y에서 수행
                return Status.Running;

            case Phase.Dash:
                float step = Mathf.Min(Speed.Value * Time.deltaTime, _stopDistance - _traveled);
                var pos = self.transform.position;
                pos.x += _dir * step;
                self.transform.position = pos;
                _traveled += step;
                if (_traveled < _stopDistance) return Status.Running;
                _phase = Phase.Recovery;
                _timer = 0f;
                RestoreGravity();
                Debug.Log($"[BossDash] 이동 종료({_traveled:F2}유닛) → 후딜 @ {Time.time:F2}s");
                return Status.Running;

            default: // Recovery
                return _timer < RecoverySeconds.Value ? Status.Running : Status.Success;
        }
    }

    protected override void OnEnd()
    {
        RestoreGravity(); // 사망 등으로 중단돼도 중력 원복
    }

    void LockDirection(GameObject self)
    {
        var target = Target.Value;
        _dir = (target != null && target.transform.position.x < self.transform.position.x) ? -1f : 1f;
        _targetY = self.transform.position.y;
        if (target != null)
        {
            Collider2D selfCollider = self.GetComponent<Collider2D>();
            Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();
            _targetY += targetCollider != null && selfCollider != null
                ? targetCollider.bounds.min.y - selfCollider.bounds.min.y
                : target.transform.position.y - self.transform.position.y;
        }
        _phase = Phase.Align;

        _rb = self.GetComponent<Rigidbody2D>();
        if (_rb != null) { _prevGravity = _rb.gravityScale; _rb.gravityScale = 0f; _rb.linearVelocity = Vector2.zero; } // 수직 정렬·돌진은 transform이 담당

        Debug.Log($"[BossDash] 방향 고정 {(_dir > 0 ? "→" : "←")}, 정렬 y={_targetY:F2} @ {Time.time:F2}s");
    }

    void BeginDash(GameObject self)
    {
        _phase = Phase.Dash;
        _traveled = 0f;
        _stopDistance = MaxDistance.Value;

        // ponytail: 벽 = Solid 레이어 잠정 — 아레나 확정 시 마스크 조정
        float half = self.TryGetComponent(out Collider2D col) ? col.bounds.extents.x : 0f;
        var hit = Physics2D.Raycast(self.transform.position, new Vector2(_dir, 0f),
                                    MaxDistance.Value + half, LayerMask.GetMask("Solid"));
        if (hit.collider != null)
            _stopDistance = Mathf.Max(0f, hit.distance - half);

        Debug.Log($"[BossDash] 돌진 시작, 이동 한계 {_stopDistance:F2}유닛 @ {Time.time:F2}s");
    }

    void RestoreGravity()
    {
        if (_rb == null) return;
        _rb.gravityScale = _prevGravity;
        _rb.linearVelocity = Vector2.zero;
        _rb = null;
    }
}
