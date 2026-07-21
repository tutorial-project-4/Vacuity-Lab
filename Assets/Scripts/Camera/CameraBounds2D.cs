using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class CameraBounds2D : MonoBehaviour
{
    private BoxCollider2D boundsCollider;

    public BoxCollider2D BoundsCollider
    {
        get
        {
            if (boundsCollider == null)
            {
                boundsCollider = GetComponent<BoxCollider2D>();
            }

            return boundsCollider;
        }
    }

    private void Awake()
    {
        boundsCollider = GetComponent<BoxCollider2D>();
    }

    private void Reset()
    {
        boundsCollider = GetComponent<BoxCollider2D>();
        boundsCollider.isTrigger = true;
    }

    private void OnValidate()
    {
        BoxCollider2D boundsCollider = GetComponent<BoxCollider2D>();
        if (boundsCollider != null)
        {
            boundsCollider.isTrigger = true;
        }
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D gizmoCollider = BoundsCollider != null ? BoundsCollider : GetComponent<BoxCollider2D>();
        if (gizmoCollider == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(gizmoCollider.bounds.center, gizmoCollider.bounds.size);
    }
}
