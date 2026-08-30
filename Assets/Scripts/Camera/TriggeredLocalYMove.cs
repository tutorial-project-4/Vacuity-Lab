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

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip moveLoopClip;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 1f;

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
            StopLoop();
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

        StartLoop(moveLoopClip);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            ApplyTargetPositions(startPositions, t);
            yield return null;
        }

        ApplyTargetPositions(startPositions, 1f);
        StopLoop();
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

    private void StartLoop(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = GetAudioSource();
        if (source == null)
        {
            return;
        }

        source.clip = clip;
        source.loop = true;
        source.volume = audioVolume;
        source.Play();
    }

    private void StopLoop()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.Stop();
        audioSource.clip = null;
    }

    private AudioSource GetAudioSource()
    {
        if (audioSource != null)
        {
            return audioSource;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        return audioSource;
    }

    private void OnDisable()
    {
        StopLoop();
    }
}
