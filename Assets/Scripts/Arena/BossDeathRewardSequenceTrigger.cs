using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossDeathRewardSequenceTrigger : MonoBehaviour
{
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private BossDeathRewardSequence rewardSequence;
    [SerializeField] private float playDelay = 1.5f;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;
    private Coroutine playRoutine;

    private void OnEnable()
    {
        SubscribeBoss();
    }

    private void OnDisable()
    {
        UnsubscribeBoss();

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }

    private void HandleBossDeath()
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayAfterDelay());
        hasTriggered = true;
    }

    private IEnumerator PlayAfterDelay()
    {
        if (playDelay > 0f)
        {
            yield return new WaitForSeconds(playDelay);
        }

        if (rewardSequence != null)
        {
            rewardSequence.Play();
        }
        else
        {
            Debug.LogWarning("[BossDeathRewardSequenceTrigger] BossDeathRewardSequence가 연결되지 않았습니다.", this);
        }

        playRoutine = null;
    }

    private void SubscribeBoss()
    {
        if (bossHealth == null)
        {
#if UNITY_2023_1_OR_NEWER
            bossHealth = FindFirstObjectByType<BossHealth>();
#else
            bossHealth = FindObjectOfType<BossHealth>();
#endif
        }

        if (bossHealth != null)
        {
            bossHealth.OnDeath += HandleBossDeath;
        }
    }

    private void UnsubscribeBoss()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDeath -= HandleBossDeath;
        }
    }

    private void OnValidate()
    {
        playDelay = Mathf.Max(0f, playDelay);
    }
}
