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
        triggered = true;
        boss2.SetActive(true);
    }
}
