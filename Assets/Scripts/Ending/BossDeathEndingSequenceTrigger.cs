using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossDeathEndingSequenceTrigger : MonoBehaviour
{
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private EndingChoiceState choiceState;
    [SerializeField] private EndingSequenceController sequenceController;
    [SerializeField] private DialogueLine[] preEndingLines;
    [SerializeField] private bool requireRejectedInjection = true;
    [SerializeField] private float playDelay = 1.5f;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;
    private Coroutine playRoutine;

    private void OnEnable()
    {
        SubscribeBossDeath();
    }

    private void OnDisable()
    {
        UnsubscribeBossDeath();

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

        if (requireRejectedInjection && !HasRejectedInjection())
        {
            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        hasTriggered = true;
        playRoutine = StartCoroutine(PlayAfterDelay());
    }

    private IEnumerator PlayAfterDelay()
    {
        if (playDelay > 0f)
        {
            yield return new WaitForSeconds(playDelay);
        }

        EndingSequenceController controller = ResolveSequenceController();
        PlayerController player = ResolvePlayer();
        yield return PlayPreEndingDialogue(player);

        if (controller != null)
        {
            controller.Play(player);
        }
        else
        {
            Debug.LogWarning("[BossDeathEndingSequenceTrigger] EndingSequenceController 참조가 없습니다.", this);
        }

        playRoutine = null;
    }

    private IEnumerator PlayPreEndingDialogue(PlayerController player)
    {
        if (preEndingLines == null || preEndingLines.Length == 0)
        {
            yield break;
        }

        DialogueRunner runner = DialogueRunner.Instance;
        if (runner == null || !runner.StartDialogue(preEndingLines, player))
        {
            yield break;
        }

        while (runner.IsRunning)
        {
            yield return null;
        }
    }

    private bool HasRejectedInjection()
    {
        EndingChoiceState state = choiceState != null ? choiceState : EndingChoiceState.Instance;
        if (state != null)
        {
            return state.SceneChoice == EndingChoice.RejectedInjection;
        }

        return EndingChoiceState.RejectedInjection;
    }

    private EndingSequenceController ResolveSequenceController()
    {
        if (sequenceController != null)
        {
            return sequenceController;
        }

#if UNITY_2023_1_OR_NEWER
        sequenceController = FindFirstObjectByType<EndingSequenceController>(FindObjectsInactive.Include);
#else
        sequenceController = FindObjectOfType<EndingSequenceController>(true);
#endif
        return sequenceController;
    }

    private static PlayerController ResolvePlayer()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<PlayerController>();
#else
        return FindObjectOfType<PlayerController>();
#endif
    }

    private void SubscribeBossDeath()
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

    private void UnsubscribeBossDeath()
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
