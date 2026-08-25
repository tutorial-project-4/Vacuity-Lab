using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class Boss2IntroTrigger : MonoBehaviour
{
    [SerializeField] GameObject boss2;
    bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered || boss2 == null || other.GetComponentInParent<PlayerMovement>() == null) return;
        IBossEncounter encounter = boss2.GetComponent<IBossEncounter>();
        if (encounter == null)
        {
            Debug.LogWarning("[Boss2IntroTrigger] IBossEncounter 보스 참조가 없습니다", this);
            return;
        }
        triggered = true;
        encounter.BeginBattle();
    }
}
