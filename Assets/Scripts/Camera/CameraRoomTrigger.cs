using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class CameraRoomTrigger : MonoBehaviour
{
    [SerializeField] private CameraBounds2D roomBounds;
    [SerializeField] private CinemachinePlatformerCamera cameraFollow;
    [SerializeField] private bool snapOnEnter;

    private void Reset()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
    }

    private void Awake()
    {
        if (cameraFollow == null && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CinemachinePlatformerCamera>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement playerMovement = other.GetComponent<PlayerMovement>();
        if (playerMovement == null)
        {
            playerMovement = other.GetComponentInParent<PlayerMovement>();
        }

        if (playerMovement == null)
        {
            return;
        }

        if (cameraFollow == null && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CinemachinePlatformerCamera>();
        }

        if (cameraFollow == null)
        {
            return;
        }

        cameraFollow.SetTarget(playerMovement.transform);
        cameraFollow.SetBounds(roomBounds != null ? roomBounds.BoundsCollider : null);

        if (snapOnEnter)
        {
            cameraFollow.SnapToTarget();
        }
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
