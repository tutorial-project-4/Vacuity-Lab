using UnityEngine;

public sealed class UnlockPlayerAbilityAction : DialogueInteractionAction
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerAbility[] abilitiesToUnlock =
    {
        PlayerAbility.DoubleJump,
        PlayerAbility.WallPhaseDash
    };
    [SerializeField] private bool runOnce = true;

    private bool hasRun;

    public override void Run()
    {
        if (runOnce && hasRun)
        {
            return;
        }

        GameManager target = gameManager != null ? gameManager : GameManager.Instance;
        if (target == null)
        {
            Debug.LogWarning("[UnlockPlayerAbilityAction] GameManager is not assigned.", this);
            return;
        }

        target.UnlockAbilities(abilitiesToUnlock);
        hasRun = true;
    }
}
