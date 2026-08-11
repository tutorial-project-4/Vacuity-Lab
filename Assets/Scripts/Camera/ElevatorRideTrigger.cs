using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ElevatorRideTrigger : MonoBehaviour
{
    [Header("Elevator")]
    [SerializeField] private Transform elevatorTransform;
    [SerializeField] private float startY = -1.5f;
    [SerializeField] private float arriveY = 13f;
    [SerializeField] private float rideDuration = 3f;
    [SerializeField] private AnimationCurve rideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Fade")]
    [SerializeField] private ScreenFadeController fadeController;
    [SerializeField] private bool useFade = true;
    [SerializeField, Range(0f, 1f)] private float fadeOutStartProgress = 0.85f;
    [SerializeField] private float fadeOutDuration = 0.8f;
    [SerializeField] private float fadeInDelay = 0.2f;
    [SerializeField] private float fadeInDuration = 0.8f;

    [Header("Camera")]
    [SerializeField] private CinemachinePlatformerCamera cameraFollow;
    [SerializeField] private SimpleCameraFollow simpleCameraFollow;
    [SerializeField] private bool returnCameraToPlayerBeforeFadeIn = true;
    [SerializeField] private bool snapCameraToPlayerOnReturn = true;

    [Header("Player")]
    [SerializeField] private bool waitUntilPlayerGrounded = true;
    [SerializeField] private bool alignPlayerToElevatorCenterOnStart = true;
    [SerializeField] private Transform playerAlignPoint;
    [SerializeField] private float playerAlignXOffset;
    [SerializeField] private bool lockPlayerDuringRide = true;
    [SerializeField] private bool movePlayerWithElevator = true;
    [SerializeField] private bool releasePlayerOnComplete = true;

    [Header("Trigger")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;
    private Coroutine rideRoutine;
    private PlayerController waitingPlayerController;

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

        PlayerController playerController = other.GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            return;
        }

        if (elevatorTransform == null)
        {
            Debug.LogWarning("[ElevatorRideTrigger] Elevator Transform is not assigned.", this);
            return;
        }

        if (rideRoutine != null)
        {
            return;
        }

        waitingPlayerController = playerController;

        if (!waitUntilPlayerGrounded || IsPlayerGrounded(playerController))
        {
            StartRide(playerController);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (rideRoutine != null || waitingPlayerController == null)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerController>() != waitingPlayerController)
        {
            return;
        }

        if (IsPlayerGrounded(waitingPlayerController))
        {
            StartRide(waitingPlayerController);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController playerController = other.GetComponentInParent<PlayerController>();
        if (playerController != null && playerController == waitingPlayerController && rideRoutine == null)
        {
            waitingPlayerController = null;
        }
    }

    private void StartRide(PlayerController playerController)
    {
        if (triggerOnce)
        {
            hasTriggered = true;
        }

        waitingPlayerController = null;
        rideRoutine = StartCoroutine(RideRoutine(playerController));
    }

    private bool IsPlayerGrounded(PlayerController playerController)
    {
        PlayerMovement movement = playerController.GetComponent<PlayerMovement>();
        return movement != null && movement.IsGrounded;
    }

    private void AlignPlayerToElevator(Transform playerTransform)
    {
        Transform alignBase = playerAlignPoint != null ? playerAlignPoint : elevatorTransform;
        Vector3 position = playerTransform.position;
        position.x = alignBase.position.x + playerAlignXOffset;
        playerTransform.position = position;
    }

    private IEnumerator RideRoutine(PlayerController playerController)
    {
        Transform playerTransform = playerController.transform;

        if (lockPlayerDuringRide)
        {
            playerController.SetCutsceneLock(true);
        }

        Vector3 elevatorPosition = elevatorTransform.position;
        elevatorPosition.y = startY;
        elevatorTransform.position = elevatorPosition;

        if (alignPlayerToElevatorCenterOnStart && playerTransform != null)
        {
            AlignPlayerToElevator(playerTransform);
        }

        float previousY = startY;
        float elapsed = 0f;
        bool fadeOutStarted = false;
        Coroutine fadeRoutine = null;

        while (elapsed < rideDuration)
        {
            elapsed += Time.deltaTime;
            float t = rideDuration > 0f ? Mathf.Clamp01(elapsed / rideDuration) : 1f;
            if (useFade && !fadeOutStarted && t >= fadeOutStartProgress)
            {
                fadeOutStarted = true;
                ScreenFadeController fade = GetFadeController();
                if (fade != null)
                {
                    fadeRoutine = StartCoroutine(fade.FadeOut(fadeOutDuration));
                }
            }

            float easedT = rideCurve != null ? rideCurve.Evaluate(t) : t;
            float nextY = Mathf.Lerp(startY, arriveY, easedT);
            float deltaY = nextY - previousY;

            elevatorPosition = elevatorTransform.position;
            elevatorPosition.y = nextY;
            elevatorTransform.position = elevatorPosition;

            if (movePlayerWithElevator && playerTransform != null)
            {
                playerTransform.position += new Vector3(0f, deltaY, 0f);
            }

            previousY = nextY;
            yield return null;
        }

        elevatorPosition = elevatorTransform.position;
        elevatorPosition.y = arriveY;
        elevatorTransform.position = elevatorPosition;

        if (fadeRoutine != null)
        {
            yield return fadeRoutine;
        }

        if (returnCameraToPlayerBeforeFadeIn && playerTransform != null)
        {
            ReturnCameraToPlayer(playerTransform);
        }

        if (useFade)
        {
            ScreenFadeController fade = GetFadeController();
            if (fade != null)
            {
                if (fadeInDelay > 0f)
                {
                    yield return new WaitForSeconds(fadeInDelay);
                }

                yield return fade.FadeIn(fadeInDuration);
            }
        }

        if (releasePlayerOnComplete && playerController != null)
        {
            playerController.SetCutsceneLock(false);
        }

        rideRoutine = null;
    }

    private void OnValidate()
    {
        rideDuration = Mathf.Max(0f, rideDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        fadeInDelay = Mathf.Max(0f, fadeInDelay);
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
    }

    private ScreenFadeController GetFadeController()
    {
        if (fadeController != null)
        {
            return fadeController;
        }

#if UNITY_2023_1_OR_NEWER
        fadeController = FindFirstObjectByType<ScreenFadeController>();
#else
        fadeController = FindObjectOfType<ScreenFadeController>();
#endif
        return fadeController;
    }

    private void ReturnCameraToPlayer(Transform playerTransform)
    {
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
            cameraFollow.SetTarget(playerTransform);
            if (snapCameraToPlayerOnReturn)
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
            simpleCameraFollow.SetTarget(playerTransform);
            return;
        }

        Debug.LogWarning("[ElevatorRideTrigger] Camera follow component was not found while returning to player.", this);
    }
}
