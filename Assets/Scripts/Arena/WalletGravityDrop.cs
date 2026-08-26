using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class WalletGravityDrop : MonoBehaviour
{
    [SerializeField] private float gravity = 18f;
    [SerializeField] private float maxFallSpeed = 10f;
    [SerializeField] private float moveStep = 0.01f;
    [SerializeField] private LayerMask solidLayer;
    [SerializeField] private bool dropOnEnable = true;

    private readonly Collider2D[] collisionBuffer = new Collider2D[16];
    private BoxCollider2D bodyCollider;
    private FloatingObject[] floatingObjects;
    private float ySpeed;
    private float yRemainder;
    private bool isDropping;

    private void Awake()
    {
        CacheComponents();
        CacheDefaultLayers();
    }

    private void OnEnable()
    {
        CacheComponents();

        if (dropOnEnable)
        {
            BeginDrop();
        }
    }

    private void Update()
    {
        if (!isDropping)
        {
            return;
        }

        ySpeed = Mathf.Max(ySpeed - gravity * Time.deltaTime, -maxFallSpeed);
        MoveY(ySpeed * Time.deltaTime);
    }

    public void BeginDrop()
    {
        CacheComponents();
        ySpeed = 0f;
        yRemainder = 0f;
        isDropping = true;
        SetFloatingEnabled(false);
    }

    private void MoveY(float amount)
    {
        yRemainder += amount;
        int move = Mathf.RoundToInt(yRemainder / moveStep);

        if (move == 0)
        {
            return;
        }

        yRemainder -= move * moveStep;
        int sign = move > 0 ? 1 : -1;

        while (move != 0)
        {
            Vector2 nextPosition = (Vector2)transform.position + new Vector2(0f, sign * moveStep);
            if (!CollideAt(nextPosition))
            {
                transform.position = new Vector3(nextPosition.x, nextPosition.y, transform.position.z);
                move -= sign;
                continue;
            }

            ySpeed = 0f;
            yRemainder = 0f;
            isDropping = false;
            SetFloatingEnabled(true);
            break;
        }
    }

    private bool CollideAt(Vector2 position)
    {
        Vector2 center = position + Vector2.Scale(bodyCollider.offset, transform.lossyScale);
        Vector2 size = Vector2.Scale(bodyCollider.size, transform.lossyScale);
        int overlapCount = Physics2D.OverlapBoxNonAlloc(center, size, 0f, collisionBuffer, solidLayer);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D hit = collisionBuffer[i];
            if (hit != null && hit != bodyCollider && !hit.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    private void SetFloatingEnabled(bool enabled)
    {
        if (floatingObjects == null)
        {
            return;
        }

        for (int i = 0; i < floatingObjects.Length; i++)
        {
            if (floatingObjects[i] != null)
            {
                floatingObjects[i].enabled = enabled;
            }
        }
    }

    private void CacheComponents()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<BoxCollider2D>();
        }

        if (floatingObjects == null || floatingObjects.Length == 0)
        {
            floatingObjects = GetComponents<FloatingObject>();
        }

        moveStep = Mathf.Max(0.0001f, moveStep);
    }

    private void CacheDefaultLayers()
    {
        if (solidLayer.value == 0)
        {
            solidLayer = LayerMask.GetMask("Solid", "OneWayPlatform");
        }
    }

    private void OnValidate()
    {
        gravity = Mathf.Max(0f, gravity);
        maxFallSpeed = Mathf.Max(0f, maxFallSpeed);
        moveStep = Mathf.Max(0.0001f, moveStep);
    }
}
