using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class BossClearCameraReturnTrigger : MonoBehaviour
{
    [Header("Boss Clear Condition")]
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private bool allowSceneBossFallback = true;
    [SerializeField] private BossProgressState progressState;

    [Header("Camera")]
    [SerializeField] private CameraFocusTrigger returnCameraFocus;

    [Header("Clear Actions")]
    [SerializeField] private GameObject[] deactivateOnClear;

    [Header("Trigger")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;
    private bool warnedMissingBoss;
    private BossHealth subscribedBoss;
    private Transform currentPlayer;

    private void Reset()
    {
        ConfigureTriggerCollider();
    }

    private void Awake()
    {
        ConfigureTriggerCollider();
        SubscribeBossDeathIfNeeded();
    }

    private void OnEnable()
    {
        SubscribeBossDeathIfNeeded();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        currentPlayer = player.transform;
        TryReturnCamera(currentPlayer);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player != null && currentPlayer == player.transform)
        {
            currentPlayer = null;
        }
    }

    private void HandleBossDeath()
    {
        if (currentPlayer != null)
        {
            TryReturnCamera(currentPlayer);
        }
    }

    private void TryReturnCamera(Transform playerTransform)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        BossHealth targetBoss = GetBossHealth();
        if (targetBoss == null || !targetBoss.IsDead)
        {
            return;
        }

        if (returnCameraFocus == null)
        {
            Debug.LogWarning("[BossClearCameraReturnTrigger] 플레이어 복귀 카메라 포커스가 연결되지 않았습니다.", this);
            return;
        }

        returnCameraFocus.Focus(playerTransform);
        MarkBoss1Cleared();
        SetObjectsActive(deactivateOnClear, false);
        hasTriggered = true;
    }

    private BossHealth GetBossHealth()
    {
        if (bossHealth != null)
        {
            return bossHealth;
        }

        if (!allowSceneBossFallback)
        {
            return null;
        }

#if UNITY_2023_1_OR_NEWER
        bossHealth = FindFirstObjectByType<BossHealth>();
#else
        bossHealth = FindObjectOfType<BossHealth>();
#endif

        if (bossHealth == null && !warnedMissingBoss)
        {
            Debug.LogWarning("[BossClearCameraReturnTrigger] BossHealth를 찾지 못했습니다.", this);
            warnedMissingBoss = true;
        }

        SubscribeBossDeathIfNeeded();
        return bossHealth;
    }

    private void SubscribeBossDeathIfNeeded()
    {
        if (bossHealth == null && allowSceneBossFallback)
        {
#if UNITY_2023_1_OR_NEWER
            bossHealth = FindFirstObjectByType<BossHealth>();
#else
            bossHealth = FindObjectOfType<BossHealth>();
#endif
        }

        if (bossHealth == null || subscribedBoss == bossHealth)
        {
            return;
        }

        UnsubscribeBossDeath();
        subscribedBoss = bossHealth;
        subscribedBoss.OnDeath += HandleBossDeath;
    }

    private void UnsubscribeBossDeath()
    {
        if (subscribedBoss != null)
        {
            subscribedBoss.OnDeath -= HandleBossDeath;
            subscribedBoss = null;
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

    private void MarkBoss1Cleared()
    {
        if (progressState == null)
        {
#if UNITY_2023_1_OR_NEWER
            progressState = FindFirstObjectByType<BossProgressState>();
#else
            progressState = FindObjectOfType<BossProgressState>();
#endif
        }

        if (progressState != null)
        {
            progressState.MarkBoss1Cleared();
        }
    }

    private void OnValidate()
    {
        ConfigureTriggerCollider();
    }

    private void OnDisable()
    {
        UnsubscribeBossDeath();
        currentPlayer = null;
    }
}
