using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerDamageSource : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private bool useTrigger = true;

    public int Damage => damage;

    private void Reset()
    {
        useTrigger = true;
        ConfigureCollider();
    }

    private void Awake()
    {
        ConfigureCollider();
    }

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
        ConfigureCollider();
    }

    private void ConfigureCollider()
    {
        Collider2D damageCollider = GetComponent<Collider2D>();
        if (damageCollider != null)
        {
            damageCollider.isTrigger = useTrigger;
        }
    }
}
