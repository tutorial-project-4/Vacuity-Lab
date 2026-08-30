using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossHealthGaugeBinder : MonoBehaviour
{
    [SerializeField] private BossHealthGauge gauge;
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private GameObject bossObject;

    public void Bind()
    {
        BindResolved(ResolveBossHealth());
    }

    public void Bind(BossHealth fallback)
    {
        BossHealth target = fallback != null ? fallback : ResolveBossHealth();
        BindResolved(target);
    }

    private BossHealth ResolveBossHealth()
    {
        if (bossHealth != null) return bossHealth;
        return bossObject != null ? bossObject.GetComponentInChildren<BossHealth>(true) : null;
    }

    private void BindResolved(BossHealth target)
    {
        if (target == null)
        {
            Debug.LogWarning("[BossHealthGaugeBinder] BossHealth 참조가 없습니다.", this);
            return;
        }

        if (gauge != null)
        {
            gauge.Bind(target);
            gauge.SetVisible(true);
            return;
        }

        BossHealthGauge.ShowFor(target);
    }
}
