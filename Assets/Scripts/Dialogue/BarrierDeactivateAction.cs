using UnityEngine;

public class BarrierDeactivateAction : DialogueInteractionAction
{
    [SerializeField] private Collider2D targetCollider;
    [SerializeField] private TerrainDescriptor targetTerrainDescriptor;
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private string animatorBoolName = "Deactive";
    [SerializeField] private string resetAnimatorTriggerName = "Active";
    [SerializeField] private string animationStateName = "barrier_deactivate";
    [SerializeField] private bool runOnce = true;

    private bool hasRun;

    public override void Run()
    {
        if (runOnce && hasRun)
        {
            return;
        }

        bool changedState = false;

        if (targetCollider != null)
        {
            targetCollider.enabled = false;
            changedState = true;
        }

        if (targetTerrainDescriptor != null)
        {
            targetTerrainDescriptor.enabled = false;
            changedState = true;
        }

        if (targetAnimator != null)
        {
            if (!string.IsNullOrWhiteSpace(resetAnimatorTriggerName))
            {
                targetAnimator.ResetTrigger(resetAnimatorTriggerName);
            }

            if (!string.IsNullOrWhiteSpace(animatorBoolName))
            {
                targetAnimator.SetTrigger(animatorBoolName);
            }

            if (!string.IsNullOrWhiteSpace(animationStateName))
            {
                targetAnimator.Play(animationStateName, 0, 0f);
            }

            changedState = true;
        }

        if (changedState)
        {
            hasRun = true;
        }
    }

    private void OnValidate()
    {
        if (targetCollider == null)
        {
            Debug.LogWarning("[BarrierDeactivateAction] Target Collider is not assigned.", this);
        }

        if (targetAnimator == null)
        {
            Debug.LogWarning("[BarrierDeactivateAction] Target Animator is not assigned.", this);
        }
    }
}
