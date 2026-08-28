using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class EndingDoorTrigger : MonoBehaviour
{
    [SerializeField] private EndingChoiceState choiceState;
    [SerializeField] private EndingSequenceController sequenceController;
    [SerializeField] private bool requireAcceptedInjection = true;
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered;

    private void Awake()
    {
        ConfigureCollider();
    }

    private void OnValidate()
    {
        ConfigureCollider();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStartEnding(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryStartEnding(other);
    }

    private void TryStartEnding(Collider2D other)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        if (requireAcceptedInjection && !HasAcceptedInjection())
        {
            return;
        }

        DialogueRunner runner = DialogueRunner.Instance;
        if (runner != null && runner.IsRunning)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        EndingSequenceController controller = GetSequenceController();
        if (controller == null || !controller.Play(player))
        {
            return;
        }

        hasTriggered = true;
    }

    private bool HasAcceptedInjection()
    {
        EndingChoiceState state = choiceState != null ? choiceState : EndingChoiceState.Instance;
        if (state != null)
        {
            return state.SceneChoice == EndingChoice.AcceptedInjection;
        }

        return EndingChoiceState.AcceptedInjection;
    }

    private EndingSequenceController GetSequenceController()
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

    private void ConfigureCollider()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
        {
            trigger.isTrigger = true;
        }
    }
}
