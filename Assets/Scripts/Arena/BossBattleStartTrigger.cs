using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class BossBattleStartTrigger : MonoBehaviour
{
    [SerializeField] Boss boss;
    bool triggered;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || other.GetComponentInParent<PlayerController>() == null) return;
        if (boss == null)
        {
            Debug.LogWarning("[BossBattleStartTrigger] Boss 참조가 없습니다", this);
            return;
        }

        triggered = true;
        boss.BeginBattle();
    }
}
