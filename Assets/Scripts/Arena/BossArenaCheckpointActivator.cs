using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class BossArenaCheckpointActivator : MonoBehaviour
{
    [SerializeField] private BossArenaRespawnController respawnController;
    [SerializeField] private BossRetryCheckpoint checkpoint;
    [SerializeField] private float activationDelay = 1f;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;
    private Coroutine activationRoutine;

    private void Reset()
    {
        ConfigureTriggerCollider();
    }

    private void Awake()
    {
        ConfigureTriggerCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerMovement>() == null)
        {
            return;
        }

        if (activationRoutine != null)
        {
            StopCoroutine(activationRoutine);
        }

        activationRoutine = StartCoroutine(ActivateAfterDelay());
        hasTriggered = true;
    }

    private IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, activationDelay));

        if (respawnController != null)
        {
            respawnController.ActivateCheckpoint(checkpoint);
        }
        else
        {
            Debug.LogWarning("[BossArenaCheckpointActivator] Respawn Controller가 연결되지 않았습니다.", this);
        }

        activationRoutine = null;
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
        activationDelay = Mathf.Max(0f, activationDelay);
        ConfigureTriggerCollider();
    }
}
