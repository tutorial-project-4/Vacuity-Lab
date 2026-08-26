using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BossDeathRewardSequence : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private GameObject bossObjectToDeactivate;

    [Header("Hazards")]
    [SerializeField] private GameObject[] deactivateOnSequenceStart;
    [SerializeField] private DialogueInteractionAction[] actionsAfterBossDeactivate;

    [Header("Platforms")]
    [SerializeField] private Transform[] fallingPlatforms;
    [SerializeField] private float platformTargetLocalY = 0f;
    [SerializeField] private float platformFallDuration = 1.2f;
    [SerializeField] private bool disablePlatformCollidersOnFall = true;

    [Header("Rewards")]
    [SerializeField] private Transform rewardSpawnOrigin;
    [SerializeField] private GameObject[] rewardsToReveal;
    [SerializeField] private Transform walletReward;
    [SerializeField] private WalletGravityDrop walletGravityDrop;
    [SerializeField] private bool placeWalletNearOrigin = true;
    [SerializeField] private float walletMinDistance = 3f;
    [SerializeField] private float walletMaxDistance = 5f;
    [SerializeField] private float walletSpawnHeight = 3f;
    [SerializeField] private Vector2 walletDropOffset = Vector2.zero;
    [SerializeField] private ParticleSystem[] rewardParticles;

    [Header("Timing")]
    [SerializeField] private float delayBeforePlatformFall = 0f;
    [SerializeField] private float delayBeforeRewardReveal = 0.2f;
    [SerializeField] private bool runOnce = true;

    private Coroutine sequenceRoutine;
    private bool hasRun;

    private void OnDisable()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
    }

    public void Play()
    {
        CacheBossHealth();

        if (runOnce && hasRun)
        {
            return;
        }

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        hasRun = true;
        SetObjectsActive(deactivateOnSequenceStart, false);

        if (delayBeforePlatformFall > 0f)
        {
            yield return new WaitForSeconds(delayBeforePlatformFall);
        }

        yield return MovePlatformsDownRoutine();
        DeactivateBoss();
        RunActions(actionsAfterBossDeactivate);

        if (delayBeforeRewardReveal > 0f)
        {
            yield return new WaitForSeconds(delayBeforeRewardReveal);
        }

        PlaceWalletReward();
        SetObjectsActive(rewardsToReveal, true);
        if (walletGravityDrop != null)
        {
            walletGravityDrop.BeginDrop();
        }

        PlayRewardParticles();
        sequenceRoutine = null;
    }

    private IEnumerator MovePlatformsDownRoutine()
    {
        if (fallingPlatforms == null || fallingPlatforms.Length == 0)
        {
            yield break;
        }

        if (disablePlatformCollidersOnFall)
        {
            SetPlatformCollidersEnabled(false);
        }

        float duration = Mathf.Max(0.01f, platformFallDuration);
        float elapsed = 0f;
        Vector3[] startPositions = new Vector3[fallingPlatforms.Length];

        for (int i = 0; i < fallingPlatforms.Length; i++)
        {
            if (fallingPlatforms[i] != null)
            {
                startPositions[i] = fallingPlatforms[i].localPosition;
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            ApplyPlatformPositions(startPositions, t);
            yield return null;
        }

        ApplyPlatformPositions(startPositions, 1f);
    }

    private void ApplyPlatformPositions(Vector3[] startPositions, float t)
    {
        for (int i = 0; i < fallingPlatforms.Length; i++)
        {
            Transform platform = fallingPlatforms[i];
            if (platform == null)
            {
                continue;
            }

            Vector3 position = startPositions[i];
            position.y = Mathf.Lerp(startPositions[i].y, platformTargetLocalY, t);
            platform.localPosition = position;
        }
    }

    private void PlaceWalletReward()
    {
        if (walletReward == null || !placeWalletNearOrigin)
        {
            return;
        }

        Transform origin = rewardSpawnOrigin != null
            ? rewardSpawnOrigin
            : bossHealth != null ? bossHealth.transform : transform;
        float minDistance = Mathf.Max(0f, walletMinDistance);
        float maxDistance = Mathf.Max(minDistance, walletMaxDistance);
        float distance = Random.Range(minDistance, maxDistance);
        float direction = Random.value < 0.5f ? -1f : 1f;
        Vector3 position = origin.position + new Vector3(
            direction * distance + walletDropOffset.x,
            Mathf.Max(0f, walletSpawnHeight) + walletDropOffset.y,
            0f
        );

        position.z = walletReward.position.z;
        walletReward.position = position;
    }

    private void DeactivateBoss()
    {
        GameObject target = bossObjectToDeactivate;
        if (target == null && bossHealth != null)
        {
            target = bossHealth.gameObject;
        }

        if (target != null)
        {
            target.SetActive(false);
        }
    }

    private void SetObjectsActive(GameObject[] targets, bool active)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].SetActive(active);
            }
        }
    }

    private void RunActions(DialogueInteractionAction[] actions)
    {
        if (actions == null)
        {
            return;
        }

        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i] != null)
            {
                actions[i].Run();
            }
        }
    }

    private void SetPlatformCollidersEnabled(bool enabled)
    {
        for (int i = 0; i < fallingPlatforms.Length; i++)
        {
            Transform platform = fallingPlatforms[i];
            if (platform == null)
            {
                continue;
            }

            Collider2D[] colliders = platform.GetComponentsInChildren<Collider2D>(true);
            for (int j = 0; j < colliders.Length; j++)
            {
                colliders[j].enabled = enabled;
            }
        }
    }

    private void PlayRewardParticles()
    {
        if (rewardParticles == null)
        {
            return;
        }

        for (int i = 0; i < rewardParticles.Length; i++)
        {
            if (rewardParticles[i] != null)
            {
                rewardParticles[i].Play();
            }
        }
    }

    private void OnValidate()
    {
        platformFallDuration = Mathf.Max(0.01f, platformFallDuration);
        delayBeforePlatformFall = Mathf.Max(0f, delayBeforePlatformFall);
        delayBeforeRewardReveal = Mathf.Max(0f, delayBeforeRewardReveal);
        walletMinDistance = Mathf.Max(0f, walletMinDistance);
        walletMaxDistance = Mathf.Max(walletMinDistance, walletMaxDistance);
        walletSpawnHeight = Mathf.Max(0f, walletSpawnHeight);
    }

    private void CacheBossHealth()
    {
        if (bossHealth != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        bossHealth = FindFirstObjectByType<BossHealth>();
#else
        bossHealth = FindObjectOfType<BossHealth>();
#endif

        if (bossHealth == null)
        {
            Debug.LogWarning("[BossDeathRewardSequence] BossHealth reference was not found.", this);
        }
    }
}
