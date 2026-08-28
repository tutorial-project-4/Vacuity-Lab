using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TriggeredLocalYMove : MonoBehaviour
{
    [SerializeField] private Transform[] targets;
    [SerializeField] private float targetLocalY = 0f;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;
    private Coroutine moveRoutine;

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
        if (!triggerOnEnter)
        {
            return;
        }

        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerMovement>() == null)
        {
            return;
        }

        TriggerMove();
        hasTriggered = true;
    }

    public void TriggerMove()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveTargetsRoutine());
    }

    private IEnumerator MoveTargetsRoutine()
    {
        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning("[TriggeredLocalYMove] Move targets are not assigned.", this);
            moveRoutine = null;
            yield break;
        }

        float duration = Mathf.Max(0.01f, moveDuration);
        float elapsed = 0f;
        Vector3[] startPositions = new Vector3[targets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                startPositions[i] = targets[i].localPosition;
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            ApplyTargetPositions(startPositions, t);
            yield return null;
        }

        ApplyTargetPositions(startPositions, 1f);
        moveRoutine = null;
    }

    private void ApplyTargetPositions(Vector3[] startPositions, float t)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            if (target == null)
            {
                continue;
            }

            Vector3 position = startPositions[i];
            position.y = Mathf.Lerp(startPositions[i].y, targetLocalY, t);
            target.localPosition = position;
        }
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
        moveDuration = Mathf.Max(0.01f, moveDuration);
        ConfigureTriggerCollider();

        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning("[TriggeredLocalYMove] Move targets are not assigned.", this);
        }
    }
}
