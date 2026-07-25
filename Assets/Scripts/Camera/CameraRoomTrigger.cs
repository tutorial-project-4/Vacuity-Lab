using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoomTrigger : MonoBehaviour
{
    [SerializeField] private CameraBounds2D roomBounds;
    [SerializeField] private CinemachinePlatformerCamera cameraFollow;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool snapOnEnter;

    private bool warnedMissingCamera;
    private bool warnedMissingBounds;

    private void Reset()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayerCollider(other, out PlayerMovement playerMovement))
        {
            return;
        }

        if (cameraFollow == null)
        {
            if (!warnedMissingCamera)
            {
                Debug.LogWarning("[CameraRoomTrigger] CinemachinePlatformerCamera is not assigned.", this);
                warnedMissingCamera = true;
            }

            return;
        }

        cameraFollow.SetTarget(playerMovement.transform);

        if (roomBounds == null || roomBounds.BoundsCollider == null)
        {
            cameraFollow.ClearBounds();

            if (!warnedMissingBounds)
            {
                Debug.LogWarning("[CameraRoomTrigger] roomBounds is not assigned. Camera bounds were cleared.", this);
                warnedMissingBounds = true;
            }
        }
        else
        {
            cameraFollow.SetBounds(roomBounds.BoundsCollider);
        }

        if (snapOnEnter)
        {
            cameraFollow.SnapToTarget();
        }
    }

    private bool IsPlayerCollider(Collider2D other, out PlayerMovement playerMovement)
    {
        playerMovement = other.GetComponentInParent<PlayerMovement>();
        if (playerMovement == null)
        {
            return false;
        }

        if (playerLayer.value == 0)
        {
            return true;
        }

        int colliderLayer = 1 << other.gameObject.layer;
        int playerRootLayer = 1 << playerMovement.gameObject.layer;
        return (playerLayer.value & (colliderLayer | playerRootLayer)) != 0;
    }

    private void OnValidate()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }
}
