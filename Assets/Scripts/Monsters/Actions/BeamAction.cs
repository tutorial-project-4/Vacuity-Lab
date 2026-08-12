using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// #11 관통 빔(v0.2 G-1): 예고 시작 시 발사 높이(낮음/중간/높음) 무작위 고정(이후 보정 없음)
/// → 예고 0.8s → 본체 0.25s 수평 관통 → 후딜 0.6s → Success.
/// 잔상은 발사 시점부터 총 2s까지 빔 오브젝트의 TimedDeactivate가 스스로 유지·소멸시킨다 —
/// 액션이 잔상 종료를 기다리면 "잔상만 다음 돌진·낙하와 공존"(D-4) 규칙이 성립하지 않기 때문.
/// 본체·잔상은 피해가 동일(1하트+공통 무적)해 콜라이더 하나를 계속 켜두면 충족, 구분은 색 연출뿐.
/// 판정은 씬 오브젝트 [Beam](아레나 전폭 트리거 + PlayerDamageSource, 기본 비활성)이 담당.
/// 동시 존재 규칙(본체 중 개체 공격 금지)과 그래프 편입은 #13에서.
/// 높이 3단 y는 아레나 미확정이라 잠정값 — 인스펙터에서 조정.

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Beam",
    story: "Boss fires piercing beam via [Beam]",
    category: "Action/Boss",
    id: "b3a1c5d7e9f24a688c0d2e4f6a8b1c3d")]
public partial class BeamAction : Action
{
    static readonly Color TelegraphColor = new(1f, 0.9f, 0.2f, 0.35f);
    static readonly Color BodyColor = new(0.4f, 0.9f, 1f, 1f);
    static readonly Color AfterimageColor = new(0.4f, 0.9f, 1f, 0.4f);
    static readonly string[] HeightNames = { "낮음", "중간", "높음" };

    [SerializeReference] public BlackboardVariable<GameObject> Beam;
    [SerializeReference] public BlackboardVariable<float> TelegraphSeconds = new(0.8f);
    [SerializeReference] public BlackboardVariable<float> BodySeconds = new(0.25f);
    [SerializeReference] public BlackboardVariable<float> AfterimageTotalSeconds = new(2f); // 발사 시점 기준
    [SerializeReference] public BlackboardVariable<float> RecoverySeconds = new(0.6f);
    [SerializeReference] public BlackboardVariable<float> LowY = new(-3.5f);  // 잠정
    [SerializeReference] public BlackboardVariable<float> MidY = new(-1f);    // 잠정
    [SerializeReference] public BlackboardVariable<float> HighY = new(1.5f);  // 잠정

    enum Phase { Telegraph, Body, Recovery }
    Phase _phase;
    float _timer;
    bool _completed;
    GameObject _beam;
    Collider2D _col;
    SpriteRenderer _sr;

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

        _phase = Phase.Telegraph;
        _timer = 0f;
        _completed = false;

        // 예고 시작 시 높이 고정(D-2) — 이후 보정 없음
        int pick = UnityEngine.Random.Range(0, 3);
        float y = pick == 0 ? LowY.Value : pick == 1 ? MidY.Value : HighY.Value;
        var pos = _beam.transform.position;
        pos.y = y;
        _beam.transform.position = pos;

        if (_col != null) _col.enabled = false; // 예고 중엔 판정 없음
        if (_sr != null) _sr.color = TelegraphColor;
        _beam.SetActive(true);
        Debug.Log($"[BossBeam] 예고 시작(높이 {HeightNames[pick]}, y={y:F2}) @ {Time.time:F2}s");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        _timer += Time.deltaTime;

        switch (_phase)
        {
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
        // 정상 종료면 잔상을 남긴다(TimedDeactivate가 소멸 담당). 중단(사망·컷신)일 때만 즉시 제거.
        if (!_completed && _beam != null && _beam.activeSelf) _beam.SetActive(false);
    }
}
