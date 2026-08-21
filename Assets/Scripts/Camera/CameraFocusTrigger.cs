using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CameraFocusTrigger : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private CinemachinePlatformerCamera cameraFollow;
    [SerializeField] private bool allowSceneCameraFallback = true;
    [SerializeField] private Transform focusPoint;
    [SerializeField] private bool usePlayerAsTarget;
    [SerializeField] private bool snapOnEnter = true;
    [SerializeField] private bool focusOnTriggerEnter = true;

    [Header("Lens")]
    [SerializeField] private bool overrideOrthographicSize = true;
    [SerializeField] private float orthographicSize = 8f;

    [Header("Bounds")]
    [SerializeField] private Collider2D cameraBounds;
    [SerializeField] private bool clearBounds;

    [Header("Trigger")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;
    private bool warnedMissingCamera;

    private void Reset()
    {
        ConfigureTriggerCollider();
    }

    private void Awake()
    {
        ConfigureTriggerCollider();
        WarnIfCameraReferenceMissing();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!focusOnTriggerEnter)
        {
            return;
        }

        if (triggerOnce && hasTriggered)
        {
            return;
        }

        PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
        {
            return;
        }

        ApplyCameraFocus(playerMovement.transform);
        hasTriggered = true;
    }

    public bool Focus(Transform playerTransform = null)
    {
        CinemachinePlatformerCamera targetCamera = GetCameraFollow();
        if (targetCamera == null)
        {
            Debug.LogWarning("[CameraFocusTrigger] CinemachinePlatformerCamera reference was not found.", this);
            return false;
        }

        Transform target = usePlayerAsTarget ? playerTransform : focusPoint;
        if (target == null && usePlayerAsTarget)
        {
            target = FindPlayerTransform();
        }

        if (target == null)
        {
            Debug.LogWarning("[CameraFocusTrigger] Focus Point is not assigned.", this);
            return false;
        }

        if (overrideOrthographicSize)
        {
            targetCamera.SetOrthographicSize(orthographicSize);
        }

        if (clearBounds)
        {
            targetCamera.ClearBounds();
        }
        else if (cameraBounds != null)
        {
            targetCamera.SetBounds(cameraBounds);
        }

        targetCamera.SetTarget(target);
        if (snapOnEnter)
        {
            targetCamera.SnapToTarget();
        }

        return true;
    }

    private void ApplyCameraFocus(Transform playerTransform)
    {
        Focus(playerTransform);
    }

    private CinemachinePlatformerCamera GetCameraFollow()
    {
        if (cameraFollow != null)
        {
            return cameraFollow;
        }

        if (!allowSceneCameraFallback)
        {
            return null;
        }

#if UNITY_2023_1_OR_NEWER
        cameraFollow = FindFirstObjectByType<CinemachinePlatformerCamera>();
#else
        cameraFollow = FindObjectOfType<CinemachinePlatformerCamera>();
#endif
        return cameraFollow;
    }

    private Transform FindPlayerTransform()
    {
#if UNITY_2023_1_OR_NEWER
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
#else
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
#endif
        return playerMovement != null ? playerMovement.transform : null;
    }

    private void ConfigureTriggerCollider()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnValidate()
    {
        orthographicSize = Mathf.Max(0.1f, orthographicSize);
        ConfigureTriggerCollider();

        if (focusPoint == null && !usePlayerAsTarget)
        {
            Debug.LogWarning("[CameraFocusTrigger] Focus Point is not assigned.", this);
        }

        if (cameraFollow == null)
        {
            Debug.LogWarning("[CameraFocusTrigger] Camera Follow is not assigned. Assign it in the Inspector, or keep fallback enabled.", this);
        }
    }

    private void WarnIfCameraReferenceMissing()
    {
        if (cameraFollow != null || warnedMissingCamera)
        {
            return;
        }

        string fallbackMessage = allowSceneCameraFallback
            ? " Scene fallback will be used at runtime."
            : " Scene fallback is disabled.";

        Debug.LogWarning($"[CameraFocusTrigger] Camera Follow is not assigned.{fallbackMessage}", this);
        warnedMissingCamera = true;
    }
}
