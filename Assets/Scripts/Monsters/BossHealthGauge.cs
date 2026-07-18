using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보스 체력을 <b>숫자 없이 비율 게이지로만</b> 표시(기획: 수치화 금지).
/// Fill 방식 Image의 fillAmount를 HpRatio에 맞춘다.
///
/// 사용법: Image(Image Type = Filled)와 BossHealth를 인스펙터에 연결.
/// </summary>
public class BossHealthGauge : MonoBehaviour
{
    [SerializeField] BossHealth boss;
    [SerializeField] Image fill;   // Image Type = Filled

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

    void Refresh()
    {
        if (boss != null && fill != null) fill.fillAmount = boss.HpRatio;
    }
}
