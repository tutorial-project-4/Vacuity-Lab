using UnityEngine;

public class BarrierDeactivateAction : DialogueInteractionAction
{
    [SerializeField] private Collider2D targetCollider;
    [SerializeField] private TerrainDescriptor targetTerrainDescriptor;
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private RuntimeAnimatorController deactivatedController;
    [SerializeField] private string animationStateName = "barrier_deactivate";
    [SerializeField] private bool runOnce = true;

    private bool hasRun;

    public override void Run()
    {
        if (runOnce && hasRun)
        {
            return;
        }

        hasRun = true;

        if (targetCollider != null)
        {
            targetCollider.enabled = false;
        }
        else if (targetTerrainDescriptor != null)
        {
            Collider2D fallbackCollider = targetTerrainDescriptor.GetComponent<Collider2D>();
            if (fallbackCollider != null)
            {
                fallbackCollider.enabled = false;
            }
        }

        if (targetTerrainDescriptor != null)
        {
            targetTerrainDescriptor.enabled = false;
        }

        if (targetAnimator == null && targetTerrainDescriptor != null)
        {
            targetAnimator = targetTerrainDescriptor.GetComponent<Animator>();
        }

        if (targetAnimator != null && !string.IsNullOrWhiteSpace(animationStateName))
        {
            if (deactivatedController != null)
            {
                targetAnimator.runtimeAnimatorController = deactivatedController;
            }

            targetAnimator.Play(animationStateName, 0, 0f);
        }
    }
}
