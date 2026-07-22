using System;
using UnityEngine;

/// 보스의 정수 HP 체력. 플레이어의 하트 체력과는 별개 시스템
///
/// 데미지 계약(플레이어 담당자 코드 기준):
///   - 플레이어 → 보스: 플레이어 공격이 GetComponent&lt;BossHealth&gt;().TakeDamage(int) 호출
///     (PlayerHealth가 PlayerDamageSource를 읽는 방식과 대칭. 아직 플레이어 공격 미구현 → 계약만 확정)
///   - 보스 → 플레이어: 보스 공격 콜라이더에 PlayerDamageSource (int Damage)를 붙이면
///     PlayerHealth가 스스로 감지해 받는다. (보스 쪽 전용 스크립트 불필요)
///
/// - 피격 시 BossHealthGauge 동안 무적(기획: 보스 0.1초)
/// - 전투 중 회복 없음(기획: 처치 시에만 풀 회복 → 회복 로직 없음)
/// - HP는 게이지로만 노출, 숫자는 표시하지 않는다.

[DisallowMultipleComponent]
public class BossHealth : MonoBehaviour
{
    [SerializeField] int maxHp = 1000;
    [SerializeField] float invulnDuration = 0.1f;

    public int MaxHp => maxHp;
    public int CurrentHp { get; private set; }
    public float HpRatio => maxHp > 0 ? (float)CurrentHp / maxHp : 0f;
    public bool IsDead => CurrentHp <= 0;

    /// 피격 후 남은 HP를 전달. (게이지, 페이즈 전환이 구독)
    public event Action<int> OnDamaged;
    public event Action OnDeath;

    float _invulnUntil;

    void Awake() => CurrentHp = maxHp;

    public void TakeDamage(int damage)
    {
        if (IsDead) return;                    // 죽은 뒤 재호출 무시(사망 1회 보장)
        if (Time.time < _invulnUntil) return;  // 무적 중
        if (damage <= 0) return;

        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        _invulnUntil = Time.time + invulnDuration;

        OnDamaged?.Invoke(CurrentHp);
        if (CurrentHp == 0) OnDeath?.Invoke();
    }

    /// 리스폰(#15) 시 초기 상태로 되돌린다.
    public void ResetHealth()
    {
        CurrentHp = maxHp;
        _invulnUntil = 0f;
        OnDamaged?.Invoke(CurrentHp);
    }

#if UNITY_EDITOR
    // 러너블 체크: 컴포넌트 우클릭 → Self Test. (asmdef 없이 EditMode 대체)
    [ContextMenu("Self Test")]
    void SelfTest()
    {
        var go = new GameObject("BossHealth_SelfTest");
        var h = go.AddComponent<BossHealth>();
        h.maxHp = 10;
        h.invulnDuration = 0f;   // 타이밍 배제하고 로직만 검증
        h.ResetHealth();         // 에디트 모드에선 Awake가 안 돌므로 수동 초기화

        int deaths = 0;
        h.OnDeath += () => deaths++;

        h.TakeDamage(3);
        Debug.Assert(h.CurrentHp == 7, "감산 실패");
        h.TakeDamage(100);
        Debug.Assert(h.CurrentHp == 0, "0 클램프 실패");
        Debug.Assert(deaths == 1, "사망 이벤트 1회 아님");
        h.TakeDamage(5);
        Debug.Assert(h.CurrentHp == 0 && deaths == 1, "죽은 뒤 재피격 무시 실패");
        Debug.Assert(Mathf.Approximately(h.HpRatio, 0f), "비율 계산 실패");

        DestroyImmediate(go);
        Debug.Log("BossHealth SelfTest PASS");
    }
#endif
}
