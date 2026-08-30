using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossHealthGaugeBinder : MonoBehaviour
{
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private GameObject bossObject;

    public void Bind()
    {
        BossHealth target = ResolveBossHealth();
        if (target == null)
        {
            Debug.LogWarning("[BossHealthGaugeBinder] BossHealth 참조가 없습니다.", this);
            return;
        }

        BossHealthGauge.ShowFor(target);
    }

    public void Bind(BossHealth fallback)
    {
        if (bossHealth == null) bossHealth = fallback;
        Bind();
    }

    private BossHealth ResolveBossHealth()
    {
        if (bossHealth != null) return bossHealth;
        return bossObject != null ? bossObject.GetComponentInChildren<BossHealth>(true) : null;
    }
}
