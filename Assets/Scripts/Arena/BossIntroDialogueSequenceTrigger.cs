using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class BossIntroDialogueSequenceTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueLine[] introLines =
    {
        new DialogueLine
        {
            speaker = "폴",
            text = "분명히 돌연변이 연구원이 있다고 했는데, 아무것도 없잖아?"
        }
    };

    [Header("Trigger")]
    [SerializeField] private bool triggerOnce = true;

    public bool HasCompleted { get; private set; }

    private bool hasTriggered;
    private Coroutine sequenceRoutine;

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

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        sequenceRoutine = StartCoroutine(SequenceRoutine(player));
        hasTriggered = true;
    }

    private IEnumerator SequenceRoutine(PlayerController player)
    {
        DialogueRunner runner = GetDialogueRunner();
        if (runner == null)
        {
            Debug.LogWarning("[BossIntroDialogueSequenceTrigger] DialogueRunner가 없어 보스 인트로 대사를 시작하지 않습니다.", this);
            ResetSequenceState();
            yield break;
        }

        if (introLines == null || introLines.Length == 0)
        {
            Debug.LogWarning("[BossIntroDialogueSequenceTrigger] 보스 인트로 대사가 비어 있습니다.", this);
            ResetSequenceState();
            yield break;
        }

        if (!runner.StartDialogue(introLines, player))
        {
            Debug.LogWarning("[BossIntroDialogueSequenceTrigger] 보스 인트로 대사를 시작하지 못했습니다.", this);
            ResetSequenceState();
            yield break;
        }

        yield return null;
        while (runner.IsRunning)
        {
            yield return null;
        }

        if (!runner.LastDialogueCompleted)
        {
            Debug.LogWarning("[BossIntroDialogueSequenceTrigger] 보스 인트로 대사가 정상 종료되지 않았습니다.", this);
            ResetSequenceState();
            yield break;
        }

        HasCompleted = true;
        sequenceRoutine = null;
    }

    private void ResetSequenceState()
    {
        sequenceRoutine = null;
        hasTriggered = false;
        HasCompleted = false;
    }

    private DialogueRunner GetDialogueRunner()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = DialogueRunner.Instance;
        }

        return dialogueRunner;
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
        ConfigureTriggerCollider();
    }

    private void OnDisable()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
    }
}
