using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// #11 관통 빔(v0.2 G-1): 예고 시작 시 보스 입 높이와 플레이어 방향을 고정(이후 보정 없음)
/// → 예고 0.8s → 본체 0.25s 수평 관통 → 후딜 0.6s → Success.
/// 잔상은 발사 시점부터 총 2s까지 빔 오브젝트의 TimedDeactivate가 스스로 유지·소멸시킨다 —
/// 액션이 잔상 종료를 기다리면 "잔상만 다음 돌진·낙하와 공존"(D-4) 규칙이 성립하지 않기 때문.
/// 본체·잔상은 피해가 동일(1하트+공통 무적)해 콜라이더 하나를 계속 켜두면 충족, 구분은 색 연출뿐.
/// 판정은 씬 오브젝트 [Beam](수평 트리거 + PlayerDamageSource, 기본 비활성)이 담당.
/// 동시 존재 규칙(본체 중 개체 공격 금지)과 그래프 편입은 #13에서.

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Beam",
    story: "Boss fires piercing beam via [Beam]",
    category: "Action/Boss",
    id: "b3a1c5d7e9f24a688c0d2e4f6a8b1c3d")]
public partial class BeamAction : Action
{
    static readonly Color32 TelegraphColor = new(255, 255, 255, 45);
    static readonly Color32 BodyColor = new(255, 255, 255, 255);
    static readonly Color32 AfterimageColor = new(31, 31, 31, 174);

    [SerializeReference] public BlackboardVariable<GameObject> Beam;
    [SerializeReference] public BlackboardVariable<float> TelegraphSeconds = new(0.8f);
    [SerializeReference] public BlackboardVariable<float> BodySeconds = new(0.25f);
    [SerializeReference] public BlackboardVariable<float> AfterimageTotalSeconds = new(2f); // 발사 시점 기준
    [SerializeReference] public BlackboardVariable<float> RecoverySeconds = new(0.6f);
    // 기존 Behavior Graph 직렬화 호환용. 새 빔은 이 높이 값을 사용하지 않는다.
    [SerializeReference] public BlackboardVariable<float> LowY = new(-3.5f);
    [SerializeReference] public BlackboardVariable<float> MidY = new(-1f);
    [SerializeReference] public BlackboardVariable<float> HighY = new(1.5f);

    enum Phase { Align, Telegraph, Body, Recovery }
    Phase _phase;
    float _timer;
    bool _completed;
    GameObject _beam;
    Collider2D _col;
    SpriteRenderer _sr;
    Boss _boss;
    Rigidbody2D _rb;
    float _prevGravity;
    float _targetY;
    float _direction;
    Collider2D _bodyCollider;
    readonly List<Collider2D> _ignoredPlatforms = new();

    protected override Status OnStart()
    {
        _beam = Beam?.Value;
        if (_beam == null)
        {
            Debug.LogWarning("[BossBeam] Blackboard Beam 참조 없음 — 빔 생략");
            return Status.Failure;
        }
        _col = _beam.GetComponent<Collider2D>();
        _sr = _beam.GetComponent<SpriteRenderer>();

        _phase = Phase.Align;
        _timer = 0f;
        _completed = false;

        _boss = UnityEngine.Object.FindAnyObjectByType<Boss>();
        if (_boss == null || _boss.Target == null)
        {
            Debug.LogWarning("[BossBeam] Boss 또는 Target 참조 없음 — 빔 생략");
            return Status.Failure;
        }
        _boss.PlayBeam();

        Collider2D targetCollider = _boss.Target.GetComponentInChildren<Collider2D>();
        Vector2 targetPosition = targetCollider != null ? targetCollider.bounds.center : _boss.Target.position;
        _targetY = targetPosition.y - _boss.BeamMouthYOffset;
        _rb = _boss.GetComponent<Rigidbody2D>();
        if (_rb != null) { _prevGravity = _rb.gravityScale; _rb.gravityScale = 0f; _rb.linearVelocity = Vector2.zero; }
        IgnorePlatformCollision();

        if (_col != null) _col.enabled = false; // 예고 중엔 판정 없음
        if (_sr != null) _sr.color = TelegraphColor;
        Debug.Log($"[BossBeam] 수직 정렬 시작(y={_targetY:F2}) @ {Time.time:F2}s");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _timer += Time.deltaTime;

        switch (_phase)
        {
            case Phase.Align:
                var bossPos = _boss.transform.position;
                bossPos.y = Mathf.MoveTowards(bossPos.y, _targetY, _boss.BeamAlignSpeed * Time.deltaTime);
                _boss.transform.position = bossPos;
                if (!Mathf.Approximately(bossPos.y, _targetY)) return Status.Running;
                BeginTelegraph();
                return Status.Running;

            case Phase.Telegraph:
                if (_timer < TelegraphSeconds.Value) return Status.Running;
                _phase = Phase.Body;
                _timer = 0f;
                if (_col != null) _col.enabled = true;
                if (_sr != null) _sr.color = BodyColor;
                // 잔상 소멸(발사+2s)은 빔이 스스로 처리 — 액션 종료 후에도 판정 유지
                if (_beam.TryGetComponent(out TimedDeactivate td)) td.Arm(AfterimageTotalSeconds.Value);
                else Debug.LogWarning("[BossBeam] TimedDeactivate 없음 — 잔상이 소멸하지 않음", _beam);
                Debug.Log($"[BossBeam] 발사(본체 {BodySeconds.Value:F2}s, 잔상 총 {AfterimageTotalSeconds.Value:F2}s) @ {Time.time:F2}s");
                return Status.Running;

            case Phase.Body:
                if (_timer < BodySeconds.Value) return Status.Running;
                _phase = Phase.Recovery;
                if (_sr != null) _sr.color = AfterimageColor; // 판정 동일 — 색만 잔상으로
                Debug.Log($"[BossBeam] 본체 종료 → 잔상·후딜 @ {Time.time:F2}s");
                return Status.Running;

            default: // Recovery — 후딜은 본체 종료부터, 잔상과 병행
                if (_timer < BodySeconds.Value + RecoverySeconds.Value) return Status.Running;
                _completed = true;
                Debug.Log($"[BossBeam] 후딜 종료(잔상은 잔여 유지) @ {Time.time:F2}s");
                return Status.Success;
        }
    }

    protected override void OnEnd()
    {
        if (_rb != null) { _rb.gravityScale = _prevGravity; _rb.linearVelocity = Vector2.zero; _rb = null; }
        RestorePlatformCollision();
        // 정상 종료면 잔상을 남긴다(TimedDeactivate가 소멸 담당). 중단(사망·컷신)일 때만 즉시 제거.
        if (!_completed && _beam != null && _beam.activeSelf) _beam.SetActive(false);
        _boss?.PlayIdle();
    }

    void IgnorePlatformCollision()
    {
        _bodyCollider = _boss.GetComponent<Collider2D>();
        if (_bodyCollider == null) return;

        foreach (var effector in UnityEngine.Object.FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None))
            foreach (var platform in effector.GetComponents<Collider2D>())
                if (platform.enabled && platform.usedByEffector)
                {
                    Physics2D.IgnoreCollision(_bodyCollider, platform, true);
                    _ignoredPlatforms.Add(platform);
                }
    }

    void RestorePlatformCollision()
    {
        if (_bodyCollider != null)
            foreach (var platform in _ignoredPlatforms)
                if (platform != null) Physics2D.IgnoreCollision(_bodyCollider, platform, false);
        _ignoredPlatforms.Clear();
        _bodyCollider = null;
    }

    void BeginTelegraph()
    {
        _phase = Phase.Telegraph;
        _timer = 0f;
        _direction = _boss.Target.position.x < _boss.transform.position.x ? -1f : 1f;
        Vector2 mouth = _boss.GetBeamMouthPosition(_direction);
        float width = Mathf.Abs(_beam.transform.lossyScale.x);
        if (_sr != null) _sr.flipX = _direction < 0f;
        _beam.transform.position = new Vector3(mouth.x + _direction * width * 0.5f, mouth.y, _beam.transform.position.z);
        _beam.SetActive(true);
        Debug.Log($"[BossBeam] 예고 시작(입={mouth}, 방향={(_direction < 0f ? "←" : "→")}) @ {Time.time:F2}s");
    }
}
