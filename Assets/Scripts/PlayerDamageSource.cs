using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerDamageSource : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    public int Damage => damage;

    private void OnValidate()
    {
        damage = Mathf.Max(0, damage);
    }
}
