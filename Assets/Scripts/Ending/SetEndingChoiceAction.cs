using UnityEngine;

public sealed class SetEndingChoiceAction : DialogueInteractionAction
{
    [SerializeField] private EndingChoiceState choiceState;
    [SerializeField] private EndingChoice choice = EndingChoice.AcceptedInjection;
    [SerializeField] private bool healPlayerToFull = true;

    public override void Run()
    {
        EndingChoiceState targetState = choiceState != null ? choiceState : EndingChoiceState.Instance;
        if (targetState != null)
        {
            targetState.SetChoice(choice);
        }
        else
        {
            EndingChoiceState.SetCurrentChoice(choice);
        }

        if (healPlayerToFull)
        {
            HealPlayerToFull();
        }
    }

    private static void HealPlayerToFull()
    {
#if UNITY_2023_1_OR_NEWER
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
#else
        PlayerHealth health = FindObjectOfType<PlayerHealth>();
#endif
        if (health != null)
        {
            health.Heal(health.MaxHearts);
        }
    }
}
