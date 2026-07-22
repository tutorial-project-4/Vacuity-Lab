using System.Collections;
using UnityEngine;

/// 위험 지형 공통 상태 기계: Inactive → Warning → Active → Cooldown → Inactive (문서 7.8~7.10).
/// 피해량·시간 수치는 전부 TBD - Inspector 임시값이며 기획 확정 후 채운다 (문서 5.2).
///
/// 피해 전달: damageObject(트리거 콜라이더 + PlayerDamageSource)를 Active 상태에서만 켜고,
/// Kinematic 플레이어 RB는 Static 트리거와 물리 이벤트를 생성하지 않으므로
/// Active 중 직접 겹침 검사로 PlayerHealth.TakeDamage를 호출한다.
/// 무적/중복 피격 정책은 PlayerHealth가 책임진다 (문서 9.5).

public abstract class HazardBase : MonoBehaviour
{
    public enum HazardState { Inactive, Warning, Active, Cooldown }

    [Header("Durations (TBD - 기획 확정 전 임시값)")]
    [SerializeField] private float warningDuration = 1f;
    [SerializeField] private float activeDuration = 2f;
    [SerializeField] private float cooldownDuration = 1f;

    [Header("Damage")]
    [Tooltip("트리거 BoxCollider2D + PlayerDamageSource를 가진 자식. Active 상태에서만 활성화된다.")]
    [SerializeField] private GameObject damageObject;
    [Tooltip("피해 대상 Layer. 비워두면 Player Layer를 사용한다. 보스 피해 여부는 TBD(문서 18).")]
    [SerializeField] private LayerMask targetLayers;

    [Header("State Visual")]
    [SerializeField] private SpriteRenderer stateVisual;
    [SerializeField] private Color inactiveColor = new Color(0.35f, 0.3f, 0.3f);
    [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color activeColor = new Color(1f, 0.25f, 0.2f);
    [SerializeField] private Color cooldownColor = new Color(0.4f, 0.45f, 0.6f);

    public HazardState State { get; private set; }
    public bool IsCycleRunning => cycleRoutine != null;

    /// 상태 변경 알림 - 아트 애니메이션/사운드 연출 연결용 훅.
    /// 어댑터 컴포넌트가 구독해서 Animator 등을 구동하면 되고, 단색 틴트(stateVisual)는 비워도 된다.
    public event System.Action<HazardState> StateChanged;

    private Coroutine cycleRoutine;
    private BoxCollider2D damageCollider;
    private PlayerDamageSource damageSource;

    protected virtual void OnEnable()
    {
        if (targetLayers.value == 0)
        {
            targetLayers = LayerMask.GetMask("Player");
        }

        ApplyState(HazardState.Inactive);
    }

    protected virtual void OnDisable()
    {
        // Disable/씬 종료 시 코루틴은 자동 중단되므로 상태와 피해 트리거만 정리한다 (문서 15 안정성).
        cycleRoutine = null;
        ApplyState(HazardState.Inactive);
    }

    /// Warning → Active → Cooldown → Inactive 1회 사이클을 시작한다. 진행 중이면 무시.
    public void StartCycle()
    {
        if (!isActiveAndEnabled || cycleRoutine != null)
        {
            return;
        }

        cycleRoutine = StartCoroutine(CycleRoutine());
    }

    /// 진행 중인 사이클을 즉시 중단하고 Inactive로 되돌린다.
    public void Deactivate()
    {
        if (cycleRoutine != null)
        {
            StopCoroutine(cycleRoutine);
            cycleRoutine = null;
        }

        ApplyState(HazardState.Inactive);
    }

    private void FixedUpdate()
    {
        if (State != HazardState.Active || damageCollider == null)
        {
            return;
        }

        // ponytail: OverlapBox 폴링 — 플레이어 RB가 Kinematic(Full Kinematic Contacts 꺼짐)이라
        // 트리거 이벤트가 오지 않는다. 플레이어 물리 설정이 바뀌면 트리거 방식으로 전환 가능.
        Vector2 center = damageCollider.transform.TransformPoint(damageCollider.offset);
        Vector2 size = Vector2.Scale(damageCollider.size, damageCollider.transform.lossyScale);
        Collider2D hit = Physics2D.OverlapBox(center, size, 0f, targetLayers);
        if (hit == null)
        {
            return;
        }

        PlayerHealth health = hit.GetComponentInParent<PlayerHealth>();
        if (health != null)
        {
            int damage = damageSource != null ? damageSource.Damage : 1;
            health.TakeDamage(damage, transform.position);
        }
    }

    private void ApplyState(HazardState state)
    {
        State = state;

        if (damageObject != null)
        {
            damageObject.SetActive(state == HazardState.Active);

            if (damageCollider == null)
            {
                damageCollider = damageObject.GetComponent<BoxCollider2D>();
                damageSource = damageObject.GetComponent<PlayerDamageSource>();
            }
        }

        if (stateVisual != null)
        {
            stateVisual.color = state switch
            {
                HazardState.Warning => warningColor,
                HazardState.Active => activeColor,
                HazardState.Cooldown => cooldownColor,
                _ => inactiveColor
            };
        }

        StateChanged?.Invoke(state);
    }

    private IEnumerator CycleRoutine()
    {
        ApplyState(HazardState.Warning);
        yield return new WaitForSeconds(warningDuration);
        ApplyState(HazardState.Active);
        yield return new WaitForSeconds(activeDuration);
        ApplyState(HazardState.Cooldown);
        yield return new WaitForSeconds(cooldownDuration);
        ApplyState(HazardState.Inactive);
        cycleRoutine = null;
    }

    /// 씬 빌더/에디터 셋업용 참조 주입.
    public void SetupReferences(GameObject damage, SpriteRenderer visual)
    {
        damageObject = damage;
        stateVisual = visual;
    }
}
