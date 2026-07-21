using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CinemachinePlatformerCamera : MonoBehaviour
{
    private const string VirtualCameraName = "Player Follow Camera";
    private const float CameraDistance = 10f;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private Vector2 targetOffset = new Vector2(0f, 0.75f);

    [Header("Lens")]
    [SerializeField] private float orthographicSize = 5f;

    [Header("Follow")]
    [SerializeField] private Vector2 followDamping = new Vector2(0.22f, 0.18f);
    [SerializeField] private Vector2 deadZoneScreenSize = new Vector2(0.18f, 0.12f);

    [Header("Look")]
    [SerializeField] private float horizontalLookAhead = 1.35f;
    [SerializeField] private float verticalLookOffset = 1.25f;
    [SerializeField] private float lookSmoothTime = 0.2f;

    [Header("Bounds")]
    [SerializeField] private Collider2D initialBounds;
    [SerializeField] private float confinerDamping = 0.2f;

    private Camera unityCamera;
    private CinemachineBrain brain;
    private CinemachineCamera virtualCamera;
    private CinemachinePositionComposer positionComposer;
    private CinemachineScreenShake2D screenShake;
    private PlayerMovement playerMovement;
    private Vector2 lookOffset;
    private Vector2 lookVelocity;

    private CinemachineConfiner2D confiner;

    private void Awake()
    {
        unityCamera = GetComponent<Camera>();
        EnsureCinemachineRig();
        CacheTargetComponents();
        ApplyStaticSettings();
    }

    private void Start()
    {
        FindTargetIfNeeded();
        AssignTargetToVirtualCamera();
        SnapToTarget();
    }

    private void LateUpdate()
    {
        FindTargetIfNeeded();
        AssignTargetToVirtualCamera();
        CacheTargetComponents();
        UpdateLookOffset();
        ApplyDynamicTargetOffset();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        CacheTargetComponents();
        AssignTargetToVirtualCamera();
    }

    public void SetBounds(Collider2D bounds)
    {
        initialBounds = bounds;
        ApplyBounds();
    }

    public void ClearBounds()
    {
        SetBounds(null);
    }

    public void Shake(float duration, float strength)
    {
        EnsureCinemachineRig();

        if (screenShake != null)
        {
            screenShake.Shake(duration, strength);
        }
    }

    public void SnapToTarget()
    {
        if (target == null || virtualCamera == null)
        {
            return;
        }

        UpdateLookOffset(true);
        ApplyDynamicTargetOffset();

        Vector3 cameraPosition = new Vector3(
            target.position.x + targetOffset.x + lookOffset.x,
            target.position.y + targetOffset.y + lookOffset.y,
            transform.position.z
        );

        virtualCamera.ForceCameraPosition(cameraPosition, Quaternion.identity);
        transform.position = cameraPosition;
        lookVelocity = Vector2.zero;
    }

    private void EnsureCinemachineRig()
    {
        if (unityCamera == null)
        {
            unityCamera = GetComponent<Camera>();
        }

        unityCamera.orthographic = true;
        unityCamera.orthographicSize = orthographicSize;

        brain = GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            brain = gameObject.AddComponent<CinemachineBrain>();
        }

        GameObject virtualCameraObject = GameObject.Find(VirtualCameraName);
        if (virtualCameraObject == null)
        {
            virtualCameraObject = new GameObject(VirtualCameraName);
            virtualCameraObject.transform.position = transform.position;
            virtualCameraObject.transform.rotation = Quaternion.identity;
        }

        virtualCamera = virtualCameraObject.GetComponent<CinemachineCamera>();
        if (virtualCamera == null)
        {
            virtualCamera = virtualCameraObject.AddComponent<CinemachineCamera>();
        }

        positionComposer = virtualCameraObject.GetComponent<CinemachinePositionComposer>();
        if (positionComposer == null)
        {
            positionComposer = virtualCameraObject.AddComponent<CinemachinePositionComposer>();
        }

        screenShake = virtualCameraObject.GetComponent<CinemachineScreenShake2D>();
        if (screenShake == null)
        {
            screenShake = virtualCameraObject.AddComponent<CinemachineScreenShake2D>();
        }

        confiner = virtualCameraObject.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
        {
            confiner = virtualCameraObject.AddComponent<CinemachineConfiner2D>();
        }
    }

    private void ApplyStaticSettings()
    {
        if (virtualCamera == null || positionComposer == null)
        {
            return;
        }

        LensSettings lens = virtualCamera.Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        lens.OrthographicSize = orthographicSize;
        lens.NearClipPlane = unityCamera.nearClipPlane;
        lens.FarClipPlane = unityCamera.farClipPlane;
        virtualCamera.Lens = lens;

        positionComposer.CameraDistance = CameraDistance;
        positionComposer.DeadZoneDepth = 0f;
        positionComposer.Damping = new Vector3(followDamping.x, followDamping.y, 0f);

        ScreenComposerSettings composition = positionComposer.Composition;
        composition.ScreenPosition = Vector2.zero;
        composition.DeadZone.Enabled = true;
        composition.DeadZone.Size = deadZoneScreenSize;
        composition.HardLimits.Enabled = false;
        positionComposer.Composition = composition;

        ApplyBounds();
    }

    private void ApplyBounds()
    {
        if (confiner == null)
        {
            return;
        }

        confiner.BoundingShape2D = initialBounds;
        confiner.Damping = confinerDamping;
        confiner.InvalidateBoundingShapeCache();
    }

    private void FindTargetIfNeeded()
    {
        if (!autoFindPlayer || target != null)
        {
            return;
        }

        PlayerMovement foundPlayer = FindPlayerMovement();
        if (foundPlayer != null)
        {
            SetTarget(foundPlayer.transform);
        }
    }

    private void AssignTargetToVirtualCamera()
    {
        if (virtualCamera != null)
        {
            virtualCamera.Follow = target;
            virtualCamera.LookAt = target;
        }
    }

    private void CacheTargetComponents()
    {
        playerMovement = target != null ? target.GetComponent<PlayerMovement>() : null;
    }

    private void UpdateLookOffset(bool snap = false)
    {
        Vector2 desiredLookOffset = Vector2.zero;

        if (playerMovement != null)
        {
            desiredLookOffset.x = playerMovement.FacingDirection * horizontalLookAhead;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed)
            {
                desiredLookOffset.y = verticalLookOffset;
            }
            else if (keyboard.sKey.isPressed)
            {
                desiredLookOffset.y = -verticalLookOffset;
            }
        }

        if (snap)
        {
            lookOffset = desiredLookOffset;
            lookVelocity = Vector2.zero;
        }
        else
        {
            lookOffset = Vector2.SmoothDamp(lookOffset, desiredLookOffset, ref lookVelocity, lookSmoothTime);
        }
    }

    private void ApplyDynamicTargetOffset()
    {
        if (positionComposer == null)
        {
            return;
        }

        positionComposer.TargetOffset = new Vector3(
            targetOffset.x + lookOffset.x,
            targetOffset.y + lookOffset.y,
            0f
        );
    }

    private PlayerMovement FindPlayerMovement()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<PlayerMovement>();
#else
        return FindObjectOfType<PlayerMovement>();
#endif
    }

    private void OnValidate()
    {
        orthographicSize = Mathf.Max(0.1f, orthographicSize);
        followDamping.x = Mathf.Max(0f, followDamping.x);
        followDamping.y = Mathf.Max(0f, followDamping.y);
        deadZoneScreenSize.x = Mathf.Clamp(deadZoneScreenSize.x, 0f, 2f);
        deadZoneScreenSize.y = Mathf.Clamp(deadZoneScreenSize.y, 0f, 2f);
        horizontalLookAhead = Mathf.Max(0f, horizontalLookAhead);
        verticalLookOffset = Mathf.Max(0f, verticalLookOffset);
        lookSmoothTime = Mathf.Max(0.001f, lookSmoothTime);
        confinerDamping = Mathf.Max(0f, confinerDamping);

        if (Application.isPlaying)
        {
            EnsureCinemachineRig();
            ApplyStaticSettings();
        }
    }
}
