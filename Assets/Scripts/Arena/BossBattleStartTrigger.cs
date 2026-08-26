using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class BossBattleStartTrigger : MonoBehaviour
{
    [Tooltip("IBossEncounter를 구현한 보스 컴포넌트입니다.")]
    [SerializeField] MonoBehaviour boss;
    bool triggered;

    void Reset() => GetComponent<Collider2D>().isTrigger = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || other.GetComponentInParent<PlayerController>() == null) return;
        if (boss is not IBossEncounter encounter)
        {
            Debug.LogWarning("[BossBattleStartTrigger] IBossEncounter 보스 참조가 없습니다", this);
            return;
        }

        triggered = true;
        encounter.BeginBattle();
    }
}
