using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// #7 점프 후 낙하(v0.2): 예고 시작 순간 플레이어 X와 발밑 플랫폼 상면을 착지점으로 고정(이후 보정 없음)
/// → 0.8s 예고 → 포물선 점프로 착지점에 낙하 → 착지 순간 자식 "SlamHitbox"
/// (좌우 1타일 트리거 + PlayerDamageSource 1하트, 평소 비활성)를 짧게 켜서 판정 → 후딜 0.7s.
/// 점프 높이·시간은 기획 미지정이라 잠정값. 타일→유닛 비율은 1:1 잠정.

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Jump Slam",
    story: "[Agent] jump-slams at [Target]",
    category: "Action/Boss",
    id: "7d3a9c51e8b24f06a1c4d27e5b90f318")]
public partial class JumpSlamAction : Action
{
    const float ImpactActiveSeconds = 0.1f; // ponytail: '착지 순간' 판정 창 잠정 0.1s

    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> TelegraphSeconds = new(0.8f);
    [SerializeReference] public BlackboardVariable<float> JumpSeconds = new(0.6f); // 잠정
    [SerializeReference] public BlackboardVariable<float> JumpHeight = new(4f);    // 잠정
    [SerializeReference] public BlackboardVariable<float> RecoverySeconds = new(0.7f);

    enum Phase { Telegraph, Jump, Recovery }
    Phase _phase;
    float _timer;
    float _landX, _landY;   // 예고 시작 순간 고정
    float _startX, _startY;
    Rigidbody2D _rb;
    float _prevGravity;
    GameObject _hitbox;

    protected override Status OnStart()
    {
        var self = Agent?.Value;
        var target = Target?.Value;
        if (self == null || target == null) return Status.Failure;

        _phase = Phase.Telegraph;
        _timer = 0f;
        _landX = target.transform.position.x;
        _landY = FindLandingY(self, target);
        _hitbox = self.transform.Find("SlamHitbox")?.gameObject;
        if (_hitbox == null)
            Debug.LogWarning("[BossSlam] 자식 SlamHitbox 없음 — 착지 피해 판정 생략", self);
        Debug.Log($"[BossSlam] 예고 시작(착지점={_landX:F2}, {_landY:F2}) @ {Time.time:F2}s"); // 예고 연출은 추후
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
                BeginJump(self);
                return Status.Running;

            case Phase.Jump:
                float t = Mathf.Min(_timer / JumpSeconds.Value, 1f);
                var pos = self.transform.position;
                pos.x = Mathf.Lerp(_startX, _landX, t);
                pos.y = Mathf.Lerp(_startY, _landY, t) + JumpHeight.Value * 4f * t * (1f - t);
                self.transform.position = pos;
                if (t < 1f) return Status.Running;
                Land();
                return Status.Running;

            default: // Recovery
                if (_hitbox != null && _hitbox.activeSelf && _timer >= ImpactActiveSeconds)
                    _hitbox.SetActive(false);
                return _timer < RecoverySeconds.Value ? Status.Running : Status.Success;
        }
    }

    protected override void OnEnd()
    {
        // 사망 등으로 중단돼도 중력·히트박스 원복
        if (_rb != null) { _rb.gravityScale = _prevGravity; _rb = null; }
        if (_hitbox != null && _hitbox.activeSelf) _hitbox.SetActive(false);
    }

    void BeginJump(GameObject self)
    {
        _phase = Phase.Jump;
        _timer = 0f;
        _startX = self.transform.position.x;
        _startY = self.transform.position.y;
        _rb = self.GetComponent<Rigidbody2D>();
        if (_rb != null) { _prevGravity = _rb.gravityScale; _rb.gravityScale = 0f; _rb.linearVelocity = Vector2.zero; } // 공중 제어는 transform이 담당
        Debug.Log($"[BossSlam] 점프 @ {Time.time:F2}s");
    }

    static float FindLandingY(GameObject self, GameObject target)
    {
        Collider2D selfCollider = self.GetComponent<Collider2D>();
        Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();
        int groundMask = LayerMask.GetMask("Solid", "Ground", "OneWayPlatform");
        Vector2 origin = targetCollider != null ? targetCollider.bounds.center : target.transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, Mathf.Infinity, groundMask);
        return hit.collider != null && selfCollider != null
            ? hit.point.y + self.transform.position.y - selfCollider.bounds.min.y
            : self.transform.position.y;
    }

    void Land()
    {
        _phase = Phase.Recovery;
        _timer = 0f;
        if (_rb != null) { _rb.gravityScale = _prevGravity; _rb.linearVelocity = Vector2.zero; _rb = null; }
        if (_hitbox != null) _hitbox.SetActive(true); // 착지 순간 판정
        Debug.Log($"[BossSlam] 착지({_landX:F2}, {_landY:F2}) → 후딜 @ {Time.time:F2}s");
    }
}
