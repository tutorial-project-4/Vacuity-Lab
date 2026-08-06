using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ElevatorCameraLockTrigger : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private CinemachinePlatformerCamera cameraFollow;
    [SerializeField] private SimpleCameraFollow simpleCameraFollow;
    [SerializeField] private Transform cameraLockPoint;
    [SerializeField] private Vector2 lockOffset = new Vector2(2f, 1.5f);
    [SerializeField] private bool snapOnEnter = true;

    [Header("Trigger")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;
    private Transform runtimeLockTarget;

    private void Reset()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (!other.GetComponentInParent<PlayerMovement>())
        {
            return;
        }

        LockCamera();
        hasTriggered = true;
    }

    private void LockCamera()
    {
        Transform target = GetLockTarget();

        if (cameraFollow == null)
        {
#if UNITY_2023_1_OR_NEWER
            cameraFollow = FindFirstObjectByType<CinemachinePlatformerCamera>();
#else
            cameraFollow = FindObjectOfType<CinemachinePlatformerCamera>();
#endif
        }

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(target);
            if (snapOnEnter)
            {
                cameraFollow.SnapToTarget();
            }

            return;
        }

        if (simpleCameraFollow == null)
        {
#if UNITY_2023_1_OR_NEWER
            simpleCameraFollow = FindFirstObjectByType<SimpleCameraFollow>();
#else
            simpleCameraFollow = FindObjectOfType<SimpleCameraFollow>();
#endif
        }

        if (simpleCameraFollow != null)
        {
            simpleCameraFollow.SetTarget(target);
            return;
        }

        Debug.LogWarning("[ElevatorCameraLockTrigger] Camera follow component was not found.", this);
    }

    private Transform GetLockTarget()
    {
        Transform basePoint = cameraLockPoint != null ? cameraLockPoint : transform;
        Vector3 lockPosition = basePoint.position + new Vector3(lockOffset.x, lockOffset.y, 0f);

        if (runtimeLockTarget == null)
        {
            GameObject targetObject = new GameObject($"{name}_CameraLockTarget");
            runtimeLockTarget = targetObject.transform;
        }

        runtimeLockTarget.position = lockPosition;
        return runtimeLockTarget;
    }
}
