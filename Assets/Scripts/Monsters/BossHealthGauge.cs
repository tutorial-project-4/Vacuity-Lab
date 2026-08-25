using UnityEngine;
using UnityEngine.UI;

/// 보스 체력을 숫자 없이 비율 게이지로만 표시(기획: 수치화 금지).
/// Fill 방식 Image의 fillAmount를 HpRatio에 맞춘다.
///
/// 사용법: Image(Image Type = Filled)와 BossHealth를 인스펙터에 연결.
/// #10 게이지 2단(500+500): 기존 Fill을 복제해 위에 겹치고(다른 색) fillTop에 할당 —
/// 위층(1000→500)이 먼저 닳아 아래층이 드러나고, 500 이하부터 아래층(fill)이 닳는다.
/// fillTop 미할당이면 기존 단일 게이지 그대로 동작.

public class BossHealthGauge : MonoBehaviour
{
    [SerializeField] BossHealth boss;
    [SerializeField] Image fill;   // Image Type = Filled — 2단 사용 시 아래층(HP 0~500)
    [SerializeField] Image fillTop; // 위층(HP 500~1000). 미할당 시 단일 게이지

    void Reset() => fill = GetComponent<Image>();

    void OnEnable()
    {
        if (boss != null)
        {
            boss.OnDamaged += HandleDamaged;
            Refresh();
        }
    }

    void OnDisable()
    {
        if (boss != null) boss.OnDamaged -= HandleDamaged;
    }

    void HandleDamaged(int _) => Refresh();

    public void Bind(BossHealth newBoss)
    {
        if (boss == newBoss) return;
        if (isActiveAndEnabled && boss != null) boss.OnDamaged -= HandleDamaged;
        boss = newBoss;
        if (isActiveAndEnabled && boss != null) boss.OnDamaged += HandleDamaged;
        Refresh();
    }

    public static void ShowFor(BossHealth health)
    {
        BossHealthGauge gauge = FindGauge();
        if (gauge == null) return;
        gauge.Bind(health);
        gauge.SetVisible(true);
    }

    public static void HideFor(BossHealth health)
    {
        BossHealthGauge gauge = FindGauge();
        if (gauge != null && gauge.boss == health) gauge.SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (fill != null) fill.transform.parent.gameObject.SetActive(visible);
    }

    void Refresh()
    {
        if (boss == null || fill == null) return;

        if (fillTop == null)
        {
            fill.fillAmount = boss.HpRatio;
            return;
        }

        fillTop.fillAmount = Mathf.Clamp01(boss.HpRatio * 2f - 1f);
        fill.fillAmount = Mathf.Clamp01(boss.HpRatio * 2f);
    }

    static BossHealthGauge FindGauge()
    {
        foreach (BossHealthGauge gauge in FindObjectsByType<BossHealthGauge>(FindObjectsInactive.Include))
            if (gauge.fill != null) return gauge;
        return null;
    }
}
